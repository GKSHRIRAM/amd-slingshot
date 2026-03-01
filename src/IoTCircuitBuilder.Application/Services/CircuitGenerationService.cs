using IoTCircuitBuilder.Application.DTOs;
using IoTCircuitBuilder.Application.Interfaces;
using IoTCircuitBuilder.Core.Interfaces;
using IoTCircuitBuilder.Core.Validation;
using IoTCircuitBuilder.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace IoTCircuitBuilder.Application.Services;

public class CircuitGenerationService : ICircuitGenerationService
{
    private readonly ILLMService _llmService;
    private readonly IBoardRepository _boardRepository;
    private readonly IComponentRepository _componentRepository;
    private readonly IConstraintSolver _solver;
    private readonly ICodeGenerator _codeGenerator;
    private readonly PinMappingValidator _validator;
    private readonly IComponentDependencyService _dependencyService;
    private readonly ILogger<CircuitGenerationService> _logger;

    public CircuitGenerationService(
        ILLMService llmService,
        IBoardRepository boardRepository,
        IComponentRepository componentRepository,
        IConstraintSolver solver,
        ICodeGenerator codeGenerator,
        PinMappingValidator validator,
        IComponentDependencyService dependencyService,
        ILogger<CircuitGenerationService> logger)
    {
        _llmService = llmService;
        _boardRepository = boardRepository;
        _componentRepository = componentRepository;
        _solver = solver;
        _codeGenerator = codeGenerator;
        _validator = validator;
        _dependencyService = dependencyService;
        _logger = logger;
    }

    public async Task<GenerateCircuitResponse> GenerateCircuitAsync(string prompt)
    {
        try
        {
            // ─── STAGE 1: ORCHESTRATOR ──────────────────────────────
            _logger.LogInformation("Stage 1: Calling Orchestrator for Network Topology Blueprint");
            var intent = await _llmService.ParseIntentAsync(prompt);

            var response = new GenerateCircuitResponse { Success = true };

            if (intent.Boards == null || intent.Boards.Count == 0)
            {
                // Fallback protection if orchestration returns zero boards
                intent.Boards = new List<BoardIntent> { new BoardIntent { Role = "General IoT Device", Board = "arduino_uno" } };
            }

            // ─── STAGE 2: DETERMINISTIC LOOP ────────────────────────
            foreach (var boardIntent in intent.Boards)
            {
                _logger.LogInformation("Processing array entry. Board ID: {BoardId}, Role: {Role}", boardIntent.Board_Id, boardIntent.Role);
                
                // 1. BOM Agent
                var catalog = await _componentRepository.GetAllComponentTypesAsync();
                var bomComponents = await _llmService.ParseBOMAsync(boardIntent.Role, boardIntent.Hardware_Class, intent.Communication_Hardware, catalog);
                
                // 1.5. Deterministic Pruning Matrix
                var prunedComponents = PruneHallucinations(boardIntent.Hardware_Class, boardIntent.Role, bomComponents);

                _logger.LogInformation("=== BOM AGENT FINAL SELECTION ===");
                foreach(var pc in prunedComponents)
                {
                    _logger.LogInformation("Selected: {Type} x{Quantity}", pc.Type, pc.Quantity);
                }
                _logger.LogInformation("================================");

                var componentTypes = prunedComponents.SelectMany(c => Enumerable.Repeat(c.Type, c.Quantity)).ToList();

                // 2. Load DB Hardware definitions
                var board = await _boardRepository.GetBoardByNameAsync(boardIntent.Board ?? "arduino_uno");
                if (board == null)
                    return GenerateCircuitResponse.Fail($"Board '{boardIntent.Board}' not found in database.");

                var initialComponents = new List<Component>();
                foreach (var type in componentTypes)
                {
                    var comp = await _componentRepository.GetComponentByTypeAsync(type);
                    if (comp == null)
                    {
                        var errorMsg = $"🛑 FATAL: LLM requested '{type}', but it does NOT exist in the hardware database!";
                        _logger.LogError(errorMsg);
                        throw new Exception(errorMsg);
                    }
                    initialComponents.Add(comp);
                }

                // 3. Auto-Inject Physics Dependencies per-board (Power Budget, Topology)
                var (injectedTypes, dependencyAdvice, preAssignments, needsBreadboardFromDeps) = _dependencyService.AnalyzeAndInject(board, initialComponents, boardIntent.Hardware_Class, prompt);

                var injectedComponents = new List<Component>();
                if (injectedTypes.Any())
                {
                    injectedComponents = await _componentRepository.GetComponentsByTypesAsync(injectedTypes);
                }

                var allComponents = initialComponents.Concat(injectedComponents).ToList();

                // ─── CRITICAL VOLTAGE CHECK ────────────────────────────
                // Before solver: Verify LLC is in the component list if voltage issues exist
                bool hasVoltageIssue = allComponents.Any(c => c.LogicVoltage == 3.3f) && (float)board.LogicLevelV > 3.3f;
                bool hasLLCInjected = allComponents.Any(c => c.Type == "logic_level_converter_4ch");
                
                if (hasVoltageIssue && !hasLLCInjected)
                {
                    _logger.LogError("🛑 VOLTAGE CONFLICT: 3.3V components detected on {BoardVoltage}V board, but Logic Level Converter not injected!", board.LogicLevelV);
                    return GenerateCircuitResponse.Fail($"Voltage incompatibility on {boardIntent.Board_Id}: 3.3V components require a logic level converter. DependencyService failed to inject.");
                }

                if (hasVoltageIssue && hasLLCInjected)
                {
                    _logger.LogInformation("✅ VOLTAGE SAFETY: Logic Level Converter injected for 3.3V silicon on {BoardVoltage}V board", board.LogicLevelV);
                }

                // 4. I2C Conflicts check (Intra-board only)
                var componentIds = allComponents.Select(c => c.ComponentId).ToList();
                var hasI2cConflict = await _componentRepository.CheckI2cConflictsAsync(componentIds);
                if (hasI2cConflict)
                {
                    response.GlobalWarnings.Add($"Board {boardIntent.Board_Id}: I2C Conflict detected. Ensure components have unique addresses or use a multiplexer.");
                }

                // 5. Physics Engine / Constraint Solver
                _logger.LogInformation("Solving pins for Board ID: {BoardId} with {ComponentCount} components (including injected safety parts)", boardIntent.Board_Id, allComponents.Count);
                var solverResult = await _solver.SolveAsync(board, allComponents, preAssignments);

                if (!solverResult.Success)
                {
                    return GenerateCircuitResponse.Fail($"Solver failed for {boardIntent.Board_Id}: {solverResult.Error}");
                }

                // ─── STAGE 2.5: THE VOLTAGE INTERCEPTOR ────────────────
                // Physically alter the final mapped graph to inject Logic Level Converters 
                // between any 5V Arduino outputs and 3.3V silicon.
                var lowVoltageInstanceKeys = solverResult.PinMapping.Keys
                    .Where(k => allComponents.Any(c => c.LogicVoltage < 4.5f && c.LogicVoltage > 0.0f && k.StartsWith(c.Type + "_")))
                    .ToList();

                if (lowVoltageInstanceKeys.Any() && (float)board.LogicLevelV >= 4.5f)
                {
                    string llcInstance = "logic_level_converter_4ch_0"; // Guarantee an instance name
                    int currentLlcChannel = 1;
                    
                    // Route power strictly
                    solverResult.PinMapping[$"{llcInstance}.HV"] = "5V";
                    solverResult.PinMapping[$"{llcInstance}.GND_HV"] = "GND";
                    solverResult.PinMapping[$"{llcInstance}.LV"] = "3V3";
                    solverResult.PinMapping[$"{llcInstance}.GND_LV"] = "GND";

                    foreach (var componentPinKey in lowVoltageInstanceKeys)
                    {
                        // Check if it's a structural power pin -> ignore, those go to 3V3 naturally
                        if (solverResult.PinMapping[componentPinKey] == "3V3" || 
                            solverResult.PinMapping[componentPinKey] == "GND" || 
                            solverResult.PinMapping[componentPinKey] == "5V" || 
                            solverResult.PinMapping[componentPinKey] == "VIN")
                        {
                            continue;
                        }

                        // It's a Data pin communicating with a 5V Arduino! Rewire the graph.
                        if (currentLlcChannel <= 4)
                        {
                            string arduinoPinOriginal = solverResult.PinMapping[componentPinKey];
                            
                            // Delete the illegal 5V -> 3.3V bridge
                            solverResult.PinMapping.Remove(componentPinKey);

                            // Construct the LLC Physical Bridge
                            solverResult.PinMapping[componentPinKey] = $"{llcInstance}.LV{currentLlcChannel}";
                            solverResult.PinMapping[$"{llcInstance}.HV{currentLlcChannel}"] = arduinoPinOriginal;

                            currentLlcChannel++;
                        }
                    }

                    // Add the injected node so the UI will render it
                    if (currentLlcChannel > 1 && !allComponents.Any(c => c.Type == "logic_level_converter_4ch"))
                    {
                        var llcComponent = await _componentRepository.GetComponentsByTypesAsync(new List<string>{ "logic_level_converter_4ch" });
                        if (llcComponent.Any()) {
                            allComponents.Add(llcComponent.First());
                            dependencyAdvice.Add("⚡ SHIFTER INJECTED: 3.3V Silicon detected. Dropped 4-Channel Bi-Directional Logic Level Converter to prevent 5V blowout.");
                        }
                    }
                }

                // Validation
                var validation = _validator.Validate(solverResult.PinMapping, board, allComponents);
                if (!validation.IsValid)
                {
                    return GenerateCircuitResponse.Fail($"Validation failed for {boardIntent.Board_Id}: \n" + string.Join("\n", validation.Errors));
                }

                // ─── STAGE 3: FIRMWARE SYNCHRONIZER ─────────────────
                _logger.LogInformation("Generating Firmware for Board ID: {BoardId} with shared payload injection", boardIntent.Board_Id);
                var code = await _codeGenerator.GenerateCodeAsync(solverResult.PinMapping, allComponents, boardIntent.Logic_Type, boardIntent.Role, intent.Shared_Payload);

                var allWarnings = new List<string>(solverResult.Warnings);
                allWarnings.AddRange(dependencyAdvice);

                // Append this generated board to the master array
                response.Boards.Add(new CircuitBoardResult
                {
                    BoardId = boardIntent.Board_Id,
                    Role = boardIntent.Role,
                    PinMapping = solverResult.PinMapping,
                    GeneratedCode = code,
                    ComponentsUsed = allComponents.Select(c => c.DisplayName ?? c.Type).ToList(),
                    NeedsBreadboard = solverResult.NeedsBreadboard || needsBreadboardFromDeps,
                    Warnings = allWarnings
                });
            }

            _logger.LogInformation("Pipeline completed successfully natively generating {Count} boards", response.Boards.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Stage 1-3 multi-board generative pipeline");
            return GenerateCircuitResponse.Fail($"System error: {ex.Message}");
        }
    }

    // ─── DETERMINISTIC PRUNING MATRIX ───────────────────────────
    private static readonly Dictionary<string, HashSet<string>> ForbiddenComponents = new(StringComparer.OrdinalIgnoreCase)
    {
        { "STATIONARY_STATIC", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "l298n_motor_driver", "dc_motor", "sg90_servo", "stepper_motor", "wheel" } },
        { "STATIONARY_KINEMATIC", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "l298n_motor_driver", "dc_motor", "wheel" } },
        { "UI_CONTROLLER", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "l298n_motor_driver", "dc_motor", "sg90_servo", "stepper_motor", "wheel", "relay_module" } },
        { "MOBILE_ROBOTICS", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "relay_module", "mains_power_plug" } }
    };

    private List<ComponentIntent> PruneHallucinations(string hardwareClass, string role, List<ComponentIntent> rawBom)
    {
        var cleanBom = new List<ComponentIntent>();
        ForbiddenComponents.TryGetValue(hardwareClass, out var illegalParts);
        illegalParts ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool isTransmitterOnly = role.Contains("transmit", StringComparison.OrdinalIgnoreCase) && !role.Contains("receive", StringComparison.OrdinalIgnoreCase);
        bool isReceiverOnly = role.Contains("receive", StringComparison.OrdinalIgnoreCase) && !role.Contains("transmit", StringComparison.OrdinalIgnoreCase);

        foreach (var comp in rawBom)
        {
            // Matrix Rules
            if (illegalParts.Contains(comp.Type))
            {
                _logger.LogWarning("🛑 PRUNING EXECUTION: Removed illegal {ComponentType} from {HardwareClass}", comp.Type, hardwareClass);
                continue; // Drop the component entirely
            }

            // Simplex Radio Rules
            if (isTransmitterOnly && comp.Type.Equals("rf_receiver", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("📻 SIMPLEX RADIO RULE: Removed rf_receiver from dedicated Transmitter board");
                continue;
            }
            if (isReceiverOnly && comp.Type.Equals("rf_transmitter", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("📻 SIMPLEX RADIO RULE: Removed rf_transmitter from dedicated Receiver board");
                continue;
            }

            cleanBom.Add(comp);
        }
        return cleanBom;
    }
}

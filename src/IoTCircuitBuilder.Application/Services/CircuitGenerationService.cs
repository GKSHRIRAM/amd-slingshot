using IoTCircuitBuilder.Application.DTOs;
using IoTCircuitBuilder.Application.Interfaces;
using IoTCircuitBuilder.Core.Interfaces;
using IoTCircuitBuilder.Core.Validation;
using IoTCircuitBuilder.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IoTCircuitBuilder.Application.Services;

public class CircuitGenerationService : ICircuitGenerationService
{
    private readonly ILLMService _llmService;
    private readonly IBoardRepository _boardRepository;
    private readonly IComponentRepository _componentRepository;
    private readonly IConstraintSolver _solver;
    private readonly ICodeGenerator _codeGenerator;
    private readonly PinMappingValidator _validator;
    private readonly ILogger<CircuitGenerationService> _logger;

    public CircuitGenerationService(
        ILLMService llmService,
        IBoardRepository boardRepository,
        IComponentRepository componentRepository,
        IConstraintSolver solver,
        ICodeGenerator codeGenerator,
        PinMappingValidator validator,
        ILogger<CircuitGenerationService> logger)
    {
        _llmService = llmService;
        _boardRepository = boardRepository;
        _componentRepository = componentRepository;
        _solver = solver;
        _codeGenerator = codeGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<GenerateCircuitResponse> GenerateCircuitAsync(string prompt)
    {
        try
        {
            // ─── PHASE 1: Parse Intent ──────────────────────────
            _logger.LogInformation("Phase 1: Parsing user prompt");
            var intent = await _llmService.ParseIntentAsync(prompt);

            // ─── PHASE 2: Load Data ─────────────────────────────
            _logger.LogInformation("Phase 2: Loading hardware data for board: {Board}", intent.Board);
            var board = await _boardRepository.GetBoardByNameAsync(intent.Board);

            if (board == null)
                return GenerateCircuitResponse.Fail($"Board '{intent.Board}' not found in database. Available boards: arduino_uno");

            _logger.LogInformation("Loaded board {Board} with {PinCount} pins", board.DisplayName, board.Pins.Count);

            // Expand components by quantity
            var componentTypes = intent.Components
                .SelectMany(c => Enumerable.Repeat(c.Type, c.Quantity))
                .ToList();

            var components = await _componentRepository.GetComponentsByTypesAsync(componentTypes);

            if (components.Count == 0)
                return GenerateCircuitResponse.Fail("No recognized components found in your description.");

            var unknownTypes = componentTypes.Except(components.Select(c => c.Type)).Distinct().ToList();
            if (unknownTypes.Any())
            {
                _logger.LogWarning("Unknown component types: {Types}", string.Join(", ", unknownTypes));
            }

            // ─── PHASE 3: Check I2C Conflicts ───────────────────
            var componentIds = components.Select(c => c.ComponentId).Distinct().ToList();
            var hasI2cConflict = await _componentRepository.CheckI2cConflictsAsync(componentIds);
            if (hasI2cConflict)
            {
                return GenerateCircuitResponse.Fail(
                    "⚠️ I2C ADDRESS CONFLICT\n\n" +
                    "Multiple components are attempting to use the same I2C address.\n\n" +
                    "💡 SOLUTIONS:\n" +
                    "1. Check component datasheets for alternate I2C addresses\n" +
                    "2. Use an I2C multiplexer (TCA9548A)\n" +
                    "3. Replace one component with an SPI alternative");
            }

            // ─── PHASE 4: Solve Constraints ─────────────────────
            _logger.LogInformation("Phase 3: Solving constraints for {Count} components", components.Count);
            var solverResult = await _solver.SolveAsync(board, components);

            if (!solverResult.Success)
            {
                _logger.LogWarning("Solver failed: {Error}", solverResult.Error);
                return GenerateCircuitResponse.Fail(solverResult.Error!);
            }

            _logger.LogInformation("Solver succeeded with {Mappings} pin assignments", solverResult.PinMapping.Count);

            // ─── PHASE 4.5: Post-Solve Validation (Safety Net) ───
            _logger.LogInformation("Phase 4.5: Validating pin mapping");
            var validation = _validator.Validate(solverResult.PinMapping, board, components);
            if (!validation.IsValid)
            {
                _logger.LogError("Post-solve validation FAILED");
                return GenerateCircuitResponse.Fail(
                    "⚠️ INTERNAL VALIDATION FAILED\n\n" +
                    string.Join("\n", validation.Errors) +
                    "\n\nThis is a bug in the solver. Please report it.");
            }

            // ─── PHASE 5: Generate Code ─────────────────────────
            _logger.LogInformation("Phase 4: Generating Arduino code");
            var code = await _codeGenerator.GenerateCodeAsync(solverResult.PinMapping, components, intent.Logic_Type);

            _logger.LogInformation("Pipeline completed successfully");

            return GenerateCircuitResponse.Ok(
                solverResult.PinMapping,
                code,
                components.Select(c => c.DisplayName ?? c.Type).ToList(),
                solverResult.NeedsBreadboard,
                solverResult.Warnings
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in circuit generation pipeline");
            return GenerateCircuitResponse.Fail($"System error: {ex.Message}");
        }
    }
}

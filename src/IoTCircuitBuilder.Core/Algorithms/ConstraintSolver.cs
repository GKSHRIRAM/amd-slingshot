using IoTCircuitBuilder.Core.Interfaces;
using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;
using IoTCircuitBuilder.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IoTCircuitBuilder.Core.Algorithms;

public class ConstraintSolver : IConstraintSolver
{
    private readonly ILogger<ConstraintSolver> _logger;
    private int _backtrackCount;
    private const int MAX_BACKTRACKS = 10000;  // Prevent infinite loops on complex circuits

    // ═══════════════════════════════════════════════════════════════
    //  HARDWARE BLACKLISTS — These are physics, not suggestions
    // ═══════════════════════════════════════════════════════════════

    // HARD RULE: D0 (RX) and D1 (TX) are the hardware UART pins.
    // If Serial.begin() is called (which 99% of projects do for debugging),
    // these pins are LOCKED by the USB-to-Serial chip. Assigning a sensor
    // to D0/D1 will cause: garbage serial output, upload failures (avrdude
    // sync error), and non-functional components.
    private static readonly HashSet<string> SERIAL_RESERVED_PINS = new() { "D0", "D1" };

    // SPI pins on Uno — reserved if any SPI component is present
    private static readonly HashSet<string> SPI_PINS = new() { "D10", "D11", "D12", "D13" };

    // I2C pins on Uno — reserved if any I2C component is present
    private static readonly HashSet<string> I2C_PINS = new() { "A4", "A5" };

    public ConstraintSolver(ILogger<ConstraintSolver> logger)
    {
        _logger = logger;
    }

    public Task<SolverResult> SolveAsync(Board board, List<Component> components, Dictionary<string, string>? preAssignedPins = null)
    {
        _backtrackCount = 0;

        _logger.LogInformation("Starting constraint solver with {ComponentCount} components on {Board}", components.Count, board.Name);

        // ─── BUILD DYNAMIC BLACKLIST ─────────────────────────────
        var blacklistedPins = BuildBlacklist(components);
        _logger.LogInformation("Blacklisted pins: {Pins}", string.Join(", ", blacklistedPins));

        // ─── PRE-VALIDATION ──────────────────────────────────────

        // Check 1: Current Budget (only board-powered components)
        var currentCheck = ValidateCurrentBudget(board, components);
        if (currentCheck != null) return Task.FromResult(currentCheck);

        // Check 2: Voltage Compatibility
        var voltageCheck = ValidateVoltageCompatibility(board, components);
        if (voltageCheck != null) return Task.FromResult(voltageCheck);

        // Collect all pin requirements (Skipping those already pre-assigned by the Dependency Service)
        var requirements = CollectRequirements(components, preAssignedPins);
        _logger.LogInformation("Collected {RequirementCount} pin requirements to route to Arduino", requirements.Count);

        // Check 3: Pin Availability by Type (excluding blacklisted)
        var pinCheck = ValidatePinAvailability(board, requirements, blacklistedPins);
        if (pinCheck != null) return Task.FromResult(pinCheck);

        // Check 4: Power Rail Distribution (breadboard detection)
        var powerCheck = CheckPowerRails(components);

        // ─── BACKTRACKING CSP ────────────────────────────────────
        var sortedReqs = requirements
            .OrderBy(r => GetRoutingPriority(r.RequiredCapability))
            .ThenBy(r => CountAvailablePins(board, r.RequiredCapability, blacklistedPins))
            .ToList();

        // Seed assignment with pre-assigned pins (e.g. topological component-to-component rules)
        var assignment = new Dictionary<string, string>();
        if (preAssignedPins != null)
        {
            foreach (var kvp in preAssignedPins)
            {
                assignment[kvp.Key] = kvp.Value;
            }
        }
        
        var usedPins = new HashSet<int>();

        _logger.LogInformation("Starting backtracking with {Count} sorted requirements", sortedReqs.Count);

        bool success = Backtrack(board, sortedReqs, 0, assignment, usedPins, blacklistedPins);

        if (success)
        {
            _logger.LogInformation("Solver completed successfully with {BacktrackCount} backtracks", _backtrackCount);

            // Attach metadata about power rails and warnings
            var warnings = new List<string>();

            if (powerCheck.NeedsBreadboard)
                warnings.Add(powerCheck.Message!);

            // Check for servo brownout risk
            var servoWarning = CheckServoBrownout(components);
            if (servoWarning != null)
                warnings.Add(servoWarning);

            return Task.FromResult(SolverResult.Succeeded(assignment, _backtrackCount, powerCheck.NeedsBreadboard, warnings));
        }

        _logger.LogWarning("Solver failed after {BacktrackCount} backtracks", _backtrackCount);
        return Task.FromResult(SolverResult.Failed(
            $"⚠️ PIN ASSIGNMENT FAILED\n\n" +
            $"Could not find a valid pin assignment for all components on {board.DisplayName ?? board.Name}.\n" +
            $"Attempted {_backtrackCount} combinations.\n\n" +
            $"💡 SOLUTIONS:\n" +
            $"1. Reduce the number of components\n" +
            $"2. Upgrade to Arduino Mega (more pins available)\n" +
            $"3. Use I2C/SPI expanders for additional GPIO"));
    }

    // ═══════════════════════════════════════════════════════════════
    //  BLACKLIST BUILDER
    // ═══════════════════════════════════════════════════════════════

    private HashSet<string> BuildBlacklist(List<Component> components)
    {
        var blacklist = new HashSet<string>();

        // ALWAYS blacklist D0/D1 — Serial.begin(9600) is in every generated sketch
        foreach (var pin in SERIAL_RESERVED_PINS)
            blacklist.Add(pin);

        // If any component uses I2C, A4/A5 are EXCLUSIVELY I2C.
        // Blacklist them from being assigned as regular Analog pins.
        // The solver's shared-pin logic will still correctly route I2C to A4/A5.
        bool hasI2c = components.Any(c =>
            c.InterfaceProtocol?.Equals("i2c", StringComparison.OrdinalIgnoreCase) == true);
        if (hasI2c)
        {
            _logger.LogInformation("I2C components detected — A4/A5 reserved exclusively for I2C bus");
            foreach (var pin in I2C_PINS)
                blacklist.Add(pin);
        }

        // If any component uses SPI protocol, reserve SPI pins
        bool hasSpi = components.Any(c =>
            c.InterfaceProtocol?.Equals("spi", StringComparison.OrdinalIgnoreCase) == true);
        if (hasSpi)
        {
            _logger.LogInformation("SPI components detected — reserving SPI bus pins");
            foreach (var pin in SPI_PINS)
                blacklist.Add(pin);
        }

        return blacklist;
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRE-VALIDATION
    // ═══════════════════════════════════════════════════════════════

    private SolverResult? ValidateCurrentBudget(Board board, List<Component> components)
    {
        // Only count components that draw from the board's own power (USB/regulator).
        // Components with RequiresExternalPower = true (e.g. DC motors via L298N)
        // draw from their own supply, not the Arduino's 500mA USB budget.
        var boardPowered = components.Where(c => !c.RequiresExternalPower).ToList();
        int totalCurrent = boardPowered.Sum(c => c.CurrentDrawMa);
        int externalCurrent = components.Where(c => c.RequiresExternalPower).Sum(c => c.CurrentDrawMa);
        _logger.LogInformation("Current budget: {Used}mA board-powered / {Max}mA (+ {External}mA external)", totalCurrent, board.MaxCurrentMa, externalCurrent);

        if (totalCurrent > board.MaxCurrentMa)
        {
            var topConsumers = boardPowered
                .OrderByDescending(c => c.CurrentDrawMa)
                .Take(3)
                .Select(c => $"• {c.DisplayName ?? c.Type}: {c.CurrentDrawMa}mA");

            return SolverResult.Failed(
                $"⚠️ POWER BUDGET EXCEEDED\n\n" +
                $"Board-powered components require {totalCurrent}mA.\n" +
                $"The {board.DisplayName ?? board.Name} can only provide {board.MaxCurrentMa}mA safely.\n\n" +
                $"💡 SOLUTIONS:\n" +
                $"1. Use an external power supply for motors/high-current components\n" +
                $"2. Reduce the number of components\n" +
                $"3. Upgrade to Arduino Mega (800mA capacity)\n\n" +
                $"COMPONENTS DRAWING MOST CURRENT:\n" +
                string.Join("\n", topConsumers));
        }

        return null;
    }

    private SolverResult? ValidateVoltageCompatibility(Board board, List<Component> components)
    {
        foreach (var comp in components)
        {
            // Skip voltage validations for power supplies and passive components (resistors, diodes)
            if (comp.Category == "power" || comp.Category == "passive")
                continue;

            // 1. Physical Power compatibility
            if (!comp.RequiresExternalPower && (board.Voltage < comp.VoltageMin || board.Voltage > comp.VoltageMax))
            {
                return SolverResult.Failed(
                    $"⚠️ VOLTAGE INCOMPATIBILITY\n\n" +
                    $"{comp.DisplayName ?? comp.Type} requires {comp.VoltageMin}V - {comp.VoltageMax}V.\n" +
                    $"The {board.DisplayName ?? board.Name} operates at {board.Voltage}V.\n\n" +
                    $"💡 SOLUTIONS:\n" +
                    $"1. Use a level shifter\n" +
                    $"2. Use a voltage regulator\n" +
                    $"3. Choose a compatible board");
            }

            // 2. Logic Level Signal compatibility (e.g., 5V Uno vs 3.3V Sensor)
            // Even if powered correctly, a 5V signal on a 3.3V GPIO will fry most sensors.
            if (board.LogicLevelV > comp.VoltageMax)
            {
                return SolverResult.Failed(
                    $"⚠️ LOGIC LEVEL CONFLICT\n\n" +
                    $"{comp.DisplayName ?? comp.Type} has a maximum tolerated voltage of {comp.VoltageMax}V.\n" +
                    $"The {board.DisplayName ?? board.Name} uses {board.LogicLevelV}V logic signals.\n" +
                    $"Connecting {board.LogicLevelV}V signals directly to this component will likely damage it.\n\n" +
                    $"💡 SOLUTIONS:\n" +
                    $"1. Use a Logic Level Shifter (e.g., TXB0104)\n" +
                    $"2. Switch to a 3.3V board (ESP32, Raspberry Pi Pico)\n" +
                    $"3. Use a resistors-based voltage divider for simple inputs (Slow)");
            }
        }

        return null;
    }

    private SolverResult? ValidatePinAvailability(Board board, List<PinRequirementEntry> requirements, HashSet<string> blacklist)
    {
        var reqGroups = requirements
            .Where(r => r.RequiredCapability != PinCapabilityType.Power5V &&
                        r.RequiredCapability != PinCapabilityType.Power3V3 &&
                        r.RequiredCapability != PinCapabilityType.PowerVin &&
                        r.RequiredCapability != PinCapabilityType.Ground)
            .GroupBy(r => r.RequiredCapability);

        foreach (var group in reqGroups)
        {
            int needed = group.Count();
            int available = CountAvailablePins(board, group.Key, blacklist);

            _logger.LogInformation("{Type} pins: need {Needed}, have {Available} (after blacklist)", group.Key, needed, available);

            if (needed > available)
            {
                return SolverResult.Failed(
                    $"⚠️ INSUFFICIENT {group.Key.ToString().ToUpper()} PINS\n\n" +
                    $"Your project needs {needed} {group.Key}-capable pins.\n" +
                    $"The {board.DisplayName ?? board.Name} only has {available} available (after reserving Serial/protocol pins).\n\n" +
                    $"💡 SOLUTIONS:\n" +
                    $"1. Upgrade to Arduino Mega (more pins)\n" +
                    $"2. Use an expansion board (e.g., PCA9685 for PWM)\n" +
                    $"3. Reduce components requiring {group.Key} capability\n\n" +
                    $"COMPONENTS REQUIRING {group.Key.ToString().ToUpper()}:\n" +
                    string.Join("\n", group.Select(r => $"• {r.ComponentLabel}.{r.PinName}")));
            }
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    //  POWER RAIL ANALYSIS (Breadboard Injector)
    // ═══════════════════════════════════════════════════════════════

    private PowerRailResult CheckPowerRails(List<Component> components)
    {
        // Count how many components need 5V and GND connections
        int required5V = components.Sum(c =>
            c.PinRequirements.Count(p => p.RequiredCapability == PinCapabilityType.Power5V));
        int requiredGnd = components.Sum(c =>
            c.PinRequirements.Count(p => p.RequiredCapability == PinCapabilityType.Ground));

        _logger.LogInformation("Power rails: {V5} components need 5V, {Gnd} need GND", required5V, requiredGnd);

        // Arduino Uno has 1x 5V pin, 1x 3.3V pin, 3x GND pins
        bool needsBreadboard = required5V > 1 || requiredGnd > 3;

        if (needsBreadboard)
        {
            _logger.LogWarning("BREADBOARD REQUIRED: {V5} devices need 5V (board has 1 pin), {Gnd} need GND (board has 3)", required5V, requiredGnd);
            return new PowerRailResult
            {
                NeedsBreadboard = true,
                Message = $"⚡ BREADBOARD REQUIRED: {required5V} components need 5V power but Arduino Uno only has 1 physical 5V pin. " +
                          $"Route one wire from Arduino 5V → breadboard + rail, then connect all component VCC lines to the breadboard."
            };
        }

        return new PowerRailResult { NeedsBreadboard = false };
    }

    // ═══════════════════════════════════════════════════════════════
    //  SERVO BROWNOUT DETECTION
    // ═══════════════════════════════════════════════════════════════

    private string? CheckServoBrownout(List<Component> components)
    {
        var servos = components.Where(c =>
            c.Type.Contains("servo", StringComparison.OrdinalIgnoreCase)).ToList();

        if (servos.Count == 0) return null;

        // SG90 rated at 100mA idle but spikes to 650mA+ under load
        int servoSpikeCurrent = servos.Count * 650; // worst-case spike per servo
        int otherCurrent = components
            .Where(c => !c.Type.Contains("servo", StringComparison.OrdinalIgnoreCase) && !c.RequiresExternalPower)
            .Sum(c => c.CurrentDrawMa);

        if (servoSpikeCurrent + otherCurrent > 500)
        {
            return $"⚠️ SERVO BROWNOUT WARNING: SG90 servo can spike to 650mA under load. " +
                   $"Combined with other components ({otherCurrent}mA), this will exceed the Arduino's 500mA USB power budget. " +
                   $"RECOMMENDED: Power servo(s) from an external 5V supply (e.g., 4xAA battery pack) and connect servo GND to Arduino GND.";
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    //  BACKTRACKING CSP
    // ═══════════════════════════════════════════════════════════════

    private bool Backtrack(
        Board board,
        List<PinRequirementEntry> requirements,
        int index,
        Dictionary<string, string> assignment,
        HashSet<int> usedPins,
        HashSet<string> blacklistedPins)
    {
        // Prevent infinite loops on overly complex circuits
        if (_backtrackCount > MAX_BACKTRACKS)
        {
            _logger.LogError("Max backtracks ({Max}) exceeded — circuit too complex for Uno", MAX_BACKTRACKS);
            return false;
        }

        if (index == requirements.Count)
            return true;

        var req = requirements[index];

        // Power, Ground, and I2C bus pins are SHARED — assign directly without marking as used.
        // I2C is a bus protocol: multiple devices share the SAME SDA/SCL wires
        // and are differentiated by address, not by pin.
        if (req.RequiredCapability == PinCapabilityType.Power5V ||
            req.RequiredCapability == PinCapabilityType.Power3V3 ||
            req.RequiredCapability == PinCapabilityType.PowerVin ||
            req.RequiredCapability == PinCapabilityType.Ground ||
            req.RequiredCapability == PinCapabilityType.I2cSda ||
            req.RequiredCapability == PinCapabilityType.I2cScl)
        {
            var sharedPin = board.Pins.FirstOrDefault(p =>
                p.Capabilities.Any(c => c.CapabilityType == req.RequiredCapability));

            if (sharedPin != null)
            {
                assignment[$"{req.ComponentLabel}.{req.PinName}"] = sharedPin.PinIdentifier;
                // Don't mark I2C/power/ground pins as "used" — they're shared buses
                return Backtrack(board, requirements, index + 1, assignment, usedPins, blacklistedPins);
            }

            return false;
        }

        // Get candidate pins: must have capability, not used, not blacklisted
        var candidatePins = board.Pins
            .Where(p => !usedPins.Contains(p.PinId) &&
                        !blacklistedPins.Contains(p.PinIdentifier) &&
                        p.Capabilities.Any(c => c.CapabilityType == req.RequiredCapability))
            .ToList();

        foreach (var pin in candidatePins)
        {
            string key = $"{req.ComponentLabel}.{req.PinName}";
            assignment[key] = pin.PinIdentifier;
            usedPins.Add(pin.PinId);

            if (Backtrack(board, requirements, index + 1, assignment, usedPins, blacklistedPins))
                return true;

            assignment.Remove(key);
            usedPins.Remove(pin.PinId);
            _backtrackCount++;

            _logger.LogDebug("Backtracking at depth {Depth}, attempt {Count}", index, _backtrackCount);
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    private List<PinRequirementEntry> CollectRequirements(List<Component> components, Dictionary<string, string>? preAssignedPins)
    {
        var entries = new List<PinRequirementEntry>();
        var typeCounts = new Dictionary<string, int>();

        foreach (var comp in components)
        {
            if (!typeCounts.ContainsKey(comp.Type)) typeCounts[comp.Type] = 0;
            int idx = typeCounts[comp.Type]++;
            string label = $"{comp.Type}_{idx}";

            foreach (var req in comp.PinRequirements)
            {
                string pinKey = $"{label}.{req.PinName}";
                
                // Rule 4: If this pin was explicitly assigned topologically by the Dependency Service, skip Uno routing.
                if (preAssignedPins != null && preAssignedPins.ContainsKey(pinKey))
                    continue;

                entries.Add(new PinRequirementEntry
                {
                    ComponentLabel = label,
                    PinName = req.PinName,
                    RequiredCapability = req.RequiredCapability,
                    ComponentDisplayName = comp.DisplayName ?? comp.Type
                });
            }
        }

        return entries;
    }

    private int CountAvailablePins(Board board, PinCapabilityType capType, HashSet<string> blacklist)
    {
        return board.Pins.Count(p =>
            !blacklist.Contains(p.PinIdentifier) &&
            p.Capabilities.Any(c => c.CapabilityType == capType));
    }

    private int GetRoutingPriority(PinCapabilityType cap)
    {
        // Tier 1: Hardcoded Protocol Pins (Must go first)
        if (cap == PinCapabilityType.I2cSda || cap == PinCapabilityType.I2cScl || 
            cap == PinCapabilityType.SpiMosi || cap == PinCapabilityType.SpiMiso || cap == PinCapabilityType.SpiSck)
            return 1;
        
        // Tier 2: Scarce Hardware Pins
        if (cap == PinCapabilityType.Pwm)
            return 2;
        
        // Tier 3: Semi-flexible Pins
        if (cap == PinCapabilityType.Analog)
            return 3;
        
        // Tier 4: Trash Pins (Generic Digital - can go anywhere)
        return 4;
    }

    private class PinRequirementEntry
    {
        public string ComponentLabel { get; set; } = string.Empty;
        public string PinName { get; set; } = string.Empty;
        public PinCapabilityType RequiredCapability { get; set; }
        public string ComponentDisplayName { get; set; } = string.Empty;
    }

    private class PowerRailResult
    {
        public bool NeedsBreadboard { get; set; }
        public string? Message { get; set; }
    }
}

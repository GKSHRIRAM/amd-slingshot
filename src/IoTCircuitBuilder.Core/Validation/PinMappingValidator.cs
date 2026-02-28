using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IoTCircuitBuilder.Core.Validation;

/// <summary>
/// Post-solve validator: double-checks every pin assignment before returning to user.
/// This is a safety net — the solver should never produce an invalid mapping,
/// but if it does, this catches it before it reaches the frontend.
/// </summary>
public class PinMappingValidator
{
    private readonly ILogger<PinMappingValidator> _logger;

    public PinMappingValidator(ILogger<PinMappingValidator> logger)
    {
        _logger = logger;
    }

    public ValidationResult Validate(
        Dictionary<string, string> mapping,
        Board board,
        List<Component> components)
    {
        var errors = new List<string>();

        // CHECK 1: No signal pin used twice (power/ground/I2C are exempt — shared buses)
        var signalPins = mapping
            .Where(kvp => !IsSharedPin(kvp.Value))
            .Select(kvp => kvp.Value)
            .ToList();

        var duplicates = signalPins
            .GroupBy(p => p)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            var msg = $"PIN CONFLICT: {string.Join(", ", duplicates)} assigned to multiple components";
            errors.Add(msg);
            _logger.LogError(msg);
        }

        // CHECK 2: Reserved pins D0/D1 NOT used (Serial always active)
        var reservedPinUsed = mapping.Values.Any(v => v == "D0" || v == "D1");
        if (reservedPinUsed)
        {
            var msg = "RESERVED PIN VIOLATION: D0/D1 (UART RX/TX) cannot be used — Serial.begin() occupies them";
            errors.Add(msg);
            _logger.LogError(msg);
        }

        // CHECK 3: PWM assignments go to actual PWM pins
        var pwmPins = new HashSet<string> { "D3", "D5", "D6", "D9", "D10", "D11" };
        foreach (var kvp in mapping)
        {
            // Find the component+pin requirement this belongs to
            var parts = kvp.Key.Split('.');
            if (parts.Length != 2) continue;

            var compLabel = parts[0]; // e.g. "sg90_servo_0"
            var pinName = parts[1];   // e.g. "SIGNAL"

            // Check if this is a PWM requirement
            var compType = compLabel.Contains('_')
                ? string.Join("_", compLabel.Split('_').SkipLast(1))
                : compLabel;

            var comp = components.FirstOrDefault(c =>
                compLabel.StartsWith(c.Type, StringComparison.OrdinalIgnoreCase));

            if (comp != null)
            {
                var req = comp.PinRequirements.FirstOrDefault(r =>
                    r.PinName.Equals(pinName, StringComparison.OrdinalIgnoreCase));

                if (req?.RequiredCapability == PinCapabilityType.Pwm && !pwmPins.Contains(kvp.Value))
                {
                    var msg = $"CAPABILITY MISMATCH: {kvp.Key} requires PWM but assigned to {kvp.Value} (not a PWM pin)";
                    errors.Add(msg);
                    _logger.LogError(msg);
                }
            }
        }

        // CHECK 4: I2C completeness — if any I2C component, both A4 and A5 must be assigned
        var hasI2c = components.Any(c =>
            c.PinRequirements.Any(p =>
                p.RequiredCapability == PinCapabilityType.I2cSda ||
                p.RequiredCapability == PinCapabilityType.I2cScl));

        if (hasI2c)
        {
            var hasSda = mapping.Values.Contains("A4");
            var hasScl = mapping.Values.Contains("A5");

            if (!hasSda || !hasScl)
            {
                var msg = "I2C INCOMPLETE: I2C components require both A4 (SDA) and A5 (SCL)";
                errors.Add(msg);
                _logger.LogError(msg);
            }
        }

        // CHECK 5: Current budget (board-powered only)
        var boardPoweredCurrent = components
            .Where(c => !c.RequiresExternalPower)
            .Sum(c => c.CurrentDrawMa);

        if (boardPoweredCurrent > board.MaxCurrentMa)
        {
            var msg = $"CURRENT OVERLOAD: {boardPoweredCurrent}mA required but {board.MaxCurrentMa}mA available";
            errors.Add(msg);
            _logger.LogError(msg);
        }

        // CHECK 6: Electrical Rules Checking (ERC)
        foreach (var kvp in mapping)
        {
            var parts = kvp.Key.Split('.');
            if (parts.Length != 2) continue;

            var compLabel = parts[0];
            var pinName = parts[1];

            var comp = components.FirstOrDefault(c => compLabel.StartsWith(c.Type, StringComparison.OrdinalIgnoreCase));
            var boardPin = board.Pins.FirstOrDefault(p => p.PinIdentifier == kvp.Value);

            if (comp != null && boardPin != null)
            {
                var req = comp.PinRequirements.FirstOrDefault(r => r.PinName.Equals(pinName, StringComparison.OrdinalIgnoreCase));

                if (req != null)
                {
                    // THE GROUND EXEMPTION: GND can ALWAYS connect to GND
                    if (boardPin.PinIdentifier == "GND" && req.RequiredCapability == PinCapabilityType.Ground)
                    {
                        continue; // Safe to connect!
                    }

                    var ercStatus = ErcCollisionMatrix.CheckConnection(boardPin.BaseErcType, req.ErcType);
                    
                    if (ercStatus == ErcConnectionStatus.Error)
                    {
                        var msg = $"ERC FATAL: Electrical short prevented! Cannot connect {boardPin.BaseErcType} ({kvp.Value}) to {req.ErcType} ({kvp.Key}).";
                        errors.Add(msg);
                        _logger.LogError(msg);
                    }
                    else if (ercStatus == ErcConnectionStatus.Warning)
                    {
                        _logger.LogWarning("ERC WARNING: Sub-optimal connection {BoardErc} ({BoardPin}) to {ReqErc} ({ReqPin}).", 
                            boardPin.BaseErcType, kvp.Value, req.ErcType, kvp.Key);
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogError("Post-solve validation FAILED with {ErrorCount} errors", errors.Count);
            return new ValidationResult { IsValid = false, Errors = errors };
        }

        _logger.LogInformation("Post-solve validation PASSED — all {MappingCount} assignments verified", mapping.Count);
        return new ValidationResult { IsValid = true, Errors = new List<string>() };
    }

    private static bool IsSharedPin(string pinIdentifier)
    {
        return pinIdentifier is "5V" or "3V3" or "GND" or "A4" or "A5";
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

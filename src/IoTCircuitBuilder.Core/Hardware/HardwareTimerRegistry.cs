using System;
using System.Collections.Generic;
using System.Linq;
using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;

namespace IoTCircuitBuilder.Core.Hardware;

public class HardwareTimerRegistry
{
    private readonly HashSet<HardwareTimer> _hijackedTimers = new();

    public void AnalyzeBomForConflicts(IEnumerable<Component> bom)
    {
        _hijackedTimers.Clear();

        // Rule 1: Servo Hijack (Timer1)
        if (bom.Any(c => c.Type.Contains("SERVO", StringComparison.OrdinalIgnoreCase)))
        {
            _hijackedTimers.Add(HardwareTimer.Timer1);
            Console.WriteLine("🛑 ERC WARNING: Servo detected. Timer1 hijacked. Pins 9 and 10 locked out for PWM.");
        }

        // Rule 2: Buzzer/Tone/RF Hijack (Timer2 usually, sometimes Timer1)
        // Note: RF modules using VirtualWire/RadioHead often use Timer1 by default, but can be moved.
        // We'll stick to Timer2 for Buzzer/Tone as per common Arduino behavior.
        if (bom.Any(c => c.Type.Contains("BUZZER", StringComparison.OrdinalIgnoreCase) || 
                         c.Type.Equals("PIEZO", StringComparison.OrdinalIgnoreCase)))
        {
            _hijackedTimers.Add(HardwareTimer.Timer2);
            Console.WriteLine("🛑 ERC WARNING: Piezo buzzer detected. Timer2 hijacked. Pins 3 and 11 locked out for PWM.");
        }
        
        // RF Modules often use Timer1 for precision bit-banging in RadioHead/VirtualWire
        if (bom.Any(c => c.Type.Contains("rf_transmitter", StringComparison.OrdinalIgnoreCase) || 
                         c.Type.Contains("rf_receiver", StringComparison.OrdinalIgnoreCase)))
        {
            _hijackedTimers.Add(HardwareTimer.Timer1);
             Console.WriteLine("🛑 ERC WARNING: RF Module detected. Timer1 hijacked for bit-banging.");
        }
    }

    public bool IsPinSafeForPwm(string pinIdentifier)
    {
        // Extract numeric part if it's like "D3"
        string pinNumber = pinIdentifier.StartsWith("D") ? pinIdentifier[1..] : pinIdentifier;

        if (UnoHardware.PwmPinToTimerMap.TryGetValue(pinNumber, out var timer))
        {
            return !_hijackedTimers.Contains(timer);
        }

        // If it's not a known PWM pin, it's "safe" in the sense that it doesn't conflict with timers,
        // but it might not support PWM at all. The solver handles capability checks.
        return true;
    }

    public List<string> FilterSafePwmPins(IEnumerable<string> availablePins)
    {
        return availablePins.Where(IsPinSafeForPwm).ToList();
    }
}

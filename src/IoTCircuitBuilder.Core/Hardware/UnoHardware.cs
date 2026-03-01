using System.Collections.Generic;
using IoTCircuitBuilder.Domain.Enums;

namespace IoTCircuitBuilder.Core.Hardware;

public static class UnoHardware
{
    // Mapping physical Arduino Uno pins to their internal hardware timers
    // Timer 0: Pins 5, 6
    // Timer 1: Pins 9, 10
    // Timer 2: Pins 3, 11
    public static readonly Dictionary<string, HardwareTimer> PwmPinToTimerMap = new()
    {
        { "5", HardwareTimer.Timer0 },
        { "6", HardwareTimer.Timer0 },
        { "9", HardwareTimer.Timer1 },
        { "10", HardwareTimer.Timer1 },
        { "3", HardwareTimer.Timer2 },
        { "11", HardwareTimer.Timer2 }
    };
}

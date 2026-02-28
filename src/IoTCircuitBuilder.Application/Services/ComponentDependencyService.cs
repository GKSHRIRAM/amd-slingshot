using System.Collections.Generic;
using System.Linq;
using IoTCircuitBuilder.Application.DTOs;
using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IoTCircuitBuilder.Application.Services;

public interface IComponentDependencyService
{
    (List<string> InjectedTypes, List<string> AdviceWarnings, Dictionary<string, string> PreAssignments, bool NeedsBreadboard) AnalyzeAndInject(IoTCircuitBuilder.Domain.Entities.Board board, List<IoTCircuitBuilder.Domain.Entities.Component> components, string? prompt = null);
}

public class ComponentDependencyService : IComponentDependencyService
{
    private readonly ILogger<ComponentDependencyService> _logger;

    public ComponentDependencyService(ILogger<ComponentDependencyService> logger)
    {
        _logger = logger;
    }

    public (List<string> InjectedTypes, List<string> AdviceWarnings, Dictionary<string, string> PreAssignments, bool NeedsBreadboard) AnalyzeAndInject(IoTCircuitBuilder.Domain.Entities.Board board, List<IoTCircuitBuilder.Domain.Entities.Component> components, string? prompt = null)
    {
        var advice = new List<string>();
        var injectedTypes = new List<string>();
        var preAssignments = new Dictionary<string, string>();
        bool needsBreadboard = false;

        // --- STEP 0: ID GENERATION UTILITIES ---
        var typeCounts = new Dictionary<string, int>();
        foreach (var c in components)
        {
            if (!typeCounts.ContainsKey(c.Type)) typeCounts[c.Type] = 0;
            typeCounts[c.Type]++;
        }

        string GetNextId(string type)
        {
            if (!typeCounts.ContainsKey(type)) typeCounts[type] = 0;
            int idx = typeCounts[type]++;
            return $"{type}_{idx}";
        }

        // --- STEP 1: DETECT INTENT & MISSING CORE PARTS ---
        bool isRobot = !string.IsNullOrEmpty(prompt) && 
                       (prompt.Contains("robot", StringComparison.OrdinalIgnoreCase) || 
                        prompt.Contains("car", StringComparison.OrdinalIgnoreCase) || 
                        prompt.Contains("chassis", StringComparison.OrdinalIgnoreCase));

        int dcMotorCount = components.Count(c => c.Type == "dc_motor");
        int driverCount = components.Count(c => c.Type == "l298n_motor_driver");

        // Force 2WD if robot
        if (isRobot && dcMotorCount < 2)
        {
            int needed = 2 - dcMotorCount;
            for(int i=0; i<needed; i++)
            {
                injectedTypes.Add("dc_motor");
                GetNextId("dc_motor"); // Reserve IDs for injected motors
            }
            dcMotorCount = 2; 
            advice.Add($"🛡️ ROBOTICS SAFETY: Added wheels to complete a 2WD chassis.");
        }

        // Force Driver if motors present
        if (dcMotorCount > 0 && driverCount == 0)
        {
            injectedTypes.Add("l298n_motor_driver");
            GetNextId("l298n_motor_driver"); // Reserve ID: l298n_motor_driver_0
            driverCount = 1;
            advice.Add("🛡️ HARDWARE SAFETY: Motor driver injected to protect the Arduino.");
        }

        // --- STEP 2: GLOBAL POWER BUDGET (Battery Injection) ---
        int totalCurrentDraw = components.Where(c => !c.RequiresExternalPower).Sum(c => c.CurrentDrawMa);
        bool needsBattery = totalCurrentDraw > board.MaxCurrentMa || dcMotorCount > 0 || driverCount > 0 || components.Any(c => c.Type.Contains("servo"));

        if (needsBattery)
        {
            injectedTypes.Add("battery_9v");
            string batId = GetNextId("battery_9v");
            advice.Add("🔌 AUTO-INJECTED 9V BATTERY: Required for high-power motors/drivers.");
            
            // Common Ground Rule
            preAssignments[$"{batId}.GND"] = "GND";

            if (driverCount > 0)
            {
                string driverId = "l298n_motor_driver_0";
                
                // Route Battery to Driver
                preAssignments[$"{batId}.VCC"] = $"{driverId}.12V";
                preAssignments[$"{driverId}.GND"] = "GND";

                // Route Motors to Driver
                if (dcMotorCount >= 1)
                {
                    preAssignments["dc_motor_0.Term1"] = $"{driverId}.OUT1";
                    preAssignments["dc_motor_0.Term2"] = $"{driverId}.OUT2";
                }
                if (dcMotorCount >= 2)
                {
                    preAssignments["dc_motor_1.Term1"] = $"{driverId}.OUT3";
                    preAssignments["dc_motor_1.Term2"] = $"{driverId}.OUT4";
                }
            }
            else
            {
                // No driver, but high draw (maybe a huge LED array). Route battery to VIN.
                preAssignments[$"{batId}.VCC"] = "VIN";
            }
        }

        // --- STEP 3: PASSIVE COMPONENTS (Resistors, Diodes) ---
        int ledCount = components.Count(c => c.Type == "led_red");
        int currentResistors = components.Count(c => c.Type == "resistor");

        if (ledCount > 0 && currentResistors < ledCount)
        {
            int missingResistors = ledCount - currentResistors;
            for (int i = 0; i < missingResistors; i++)
            {
                injectedTypes.Add("resistor");
            }
            advice.Add($"🛡️ AUTO-INJECTED {missingResistors}x RESISTORS: Connected to LEDs to limit current.");
        }

        // Flyback Diodes for any generic motors
        int currentDiodes = components.Count(c => c.Type == "diode");
        if (dcMotorCount > 0)
        {
            int missingDiodes = dcMotorCount - currentDiodes;
            for (int i = 0; i < missingDiodes; i++)
            {
                injectedTypes.Add("diode");
                string diodeId = GetNextId("diode");
                
                // TOPOLOGICAL WIRING: The diode wires directly across the motor terminals!
                preAssignments[$"{diodeId}.ANODE"] = $"dc_motor_{i}.Term1";
                preAssignments[$"{diodeId}.CATHODE"] = $"dc_motor_{i}.Term2";
            }
            if (missingDiodes > 0)
                advice.Add($"⚡ AUTO-INJECTED {missingDiodes}x FLYBACK DIODES: Wired directly across motor terminals to protect the circuit.");
        }

        // --- STEP 4: BREADBOARD DETECTION ---
        int count5V = components.Sum(c => c.PinRequirements.Count(p => p.RequiredCapability == PinCapabilityType.Power5V))
                    + injectedTypes.Count(t => t == "l298n_motor_driver" || t == "relay_module");
        
        int countGND = components.Sum(c => c.PinRequirements.Count(p => p.RequiredCapability == PinCapabilityType.Ground))
                     + injectedTypes.Count(t => t == "battery_9v" || t == "l298n_motor_driver");

        if (count5V > 1 || countGND > 3 || (components.Count + injectedTypes.Count) > 5)
        {
            needsBreadboard = true;
            advice.Add("🥪 BREADBOARD INJECTED: Too many power/signal connections for the Arduino pins alone.");
        }

        return (injectedTypes, advice, preAssignments, needsBreadboard);
    }
}

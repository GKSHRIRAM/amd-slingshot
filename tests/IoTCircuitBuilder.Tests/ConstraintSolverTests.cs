using IoTCircuitBuilder.Core.Algorithms;
using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace IoTCircuitBuilder.Tests;

public class ConstraintSolverTests
{
    private readonly ConstraintSolver _solver;

    public ConstraintSolverTests()
    {
        var logger = new Mock<ILogger<ConstraintSolver>>();
        _solver = new ConstraintSolver(logger.Object);
    }

    private Board CreateArduinoUno()
    {
        var board = new Board
        {
            BoardId = 1,
            Name = "arduino_uno",
            DisplayName = "Arduino Uno R3",
            Voltage = 5.0m,
            MaxCurrentMa = 500,
            LogicLevelV = 5.0m,
            Is5VTolerant = true,
            Pins = new List<Pin>()
        };

        int pinId = 1;
        // Digital D0-D13
        for (int i = 0; i <= 13; i++)
        {
            var pin = new Pin { PinId = pinId, BoardId = 1, PinIdentifier = $"D{i}", Voltage = 5.0m, MaxCurrentMa = 40, Capabilities = new List<PinCapability>() };
            pin.Capabilities.Add(new PinCapability { CapabilityId = pinId * 10, PinId = pinId, CapabilityType = PinCapabilityType.Digital });
            // PWM: D3, D5, D6, D9, D10, D11
            if (i is 3 or 5 or 6 or 9 or 10 or 11)
                pin.Capabilities.Add(new PinCapability { CapabilityId = pinId * 10 + 1, PinId = pinId, CapabilityType = PinCapabilityType.Pwm });
            board.Pins.Add(pin);
            pinId++;
        }

        // Analog A0-A5
        for (int i = 0; i <= 5; i++)
        {
            var pin = new Pin { PinId = pinId, BoardId = 1, PinIdentifier = $"A{i}", Voltage = 5.0m, MaxCurrentMa = 40, Capabilities = new List<PinCapability>() };
            pin.Capabilities.Add(new PinCapability { CapabilityId = pinId * 10, PinId = pinId, CapabilityType = PinCapabilityType.Analog });
            pin.Capabilities.Add(new PinCapability { CapabilityId = pinId * 10 + 1, PinId = pinId, CapabilityType = PinCapabilityType.Digital });
            board.Pins.Add(pin);
            pinId++;
        }

        // Power pins
        var pin5v = new Pin { PinId = pinId++, BoardId = 1, PinIdentifier = "5V", Voltage = 5.0m, MaxCurrentMa = 500, Capabilities = new List<PinCapability>() };
        pin5v.Capabilities.Add(new PinCapability { CapabilityId = 500, PinId = pin5v.PinId, CapabilityType = PinCapabilityType.Power5V });
        board.Pins.Add(pin5v);

        var pinGnd = new Pin { PinId = pinId++, BoardId = 1, PinIdentifier = "GND", Voltage = 0m, MaxCurrentMa = 1000, Capabilities = new List<PinCapability>() };
        pinGnd.Capabilities.Add(new PinCapability { CapabilityId = 501, PinId = pinGnd.PinId, CapabilityType = PinCapabilityType.Ground });
        board.Pins.Add(pinGnd);

        return board;
    }

    // ─── TEST 1: Simple LED Success ─────────────────────────────
    [Fact]
    public async Task Solver_SimpleCase_LED_Success()
    {
        var board = CreateArduinoUno();
        var led = new Component
        {
            ComponentId = 1, Type = "led_red", DisplayName = "Red LED",
            CurrentDrawMa = 20, VoltageMin = 2.0m, VoltageMax = 5.0m,
            RequiresExternalPower = false,
            PinRequirements = new List<ComponentPinRequirement>
            {
                new() { RequirementId = 1, ComponentId = 1, PinName = "ANODE", RequiredCapability = PinCapabilityType.Digital },
                new() { RequirementId = 2, ComponentId = 1, PinName = "CATHODE", RequiredCapability = PinCapabilityType.Ground }
            }
        };

        var result = await _solver.SolveAsync(board, new List<Component> { led });

        result.Success.Should().BeTrue();
        result.PinMapping.Should().ContainKey("led_red_0.ANODE");
        result.PinMapping.Should().ContainKey("led_red_0.CATHODE");
        result.PinMapping["led_red_0.CATHODE"].Should().Be("GND");
    }

    // ─── TEST 2: Current Overload Error ─────────────────────────
    [Fact]
    public async Task Solver_CurrentOverload_ReturnsError()
    {
        var board = CreateArduinoUno();
        var motors = Enumerable.Range(0, 5).Select(i => new Component
        {
            ComponentId = i + 10, Type = "dc_motor", DisplayName = "DC Motor",
            CurrentDrawMa = 200, VoltageMin = 3.0m, VoltageMax = 12.0m,
            RequiresExternalPower = true,
            PinRequirements = new List<ComponentPinRequirement>()
        }).ToList();

        // Total: 5 * 200 = 1000mA > 500mA
        var result = await _solver.SolveAsync(board, motors);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("POWER BUDGET EXCEEDED");
    }

    // ─── TEST 3: Insufficient PWM Pins ──────────────────────────
    [Fact]
    public async Task Solver_InsufficientPWM_ReturnsError()
    {
        var board = CreateArduinoUno();
        // Create 10 servos each needing 1 PWM pin. Uno only has 6 PWM.
        var servos = Enumerable.Range(0, 10).Select(i => new Component
        {
            ComponentId = i + 20, Type = "sg90_servo", DisplayName = "SG90 Servo",
            CurrentDrawMa = 10, VoltageMin = 4.8m, VoltageMax = 6.0m,
            RequiresExternalPower = false,
            PinRequirements = new List<ComponentPinRequirement>
            {
                new() { RequirementId = i * 10 + 100, ComponentId = i + 20, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Pwm },
                new() { RequirementId = i * 10 + 101, ComponentId = i + 20, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V },
                new() { RequirementId = i * 10 + 102, ComponentId = i + 20, PinName = "GND", RequiredCapability = PinCapabilityType.Ground }
            }
        }).ToList();

        var result = await _solver.SolveAsync(board, servos);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("INSUFFICIENT");
        result.Error.Should().Contain("PWM", because: "error should mention the insufficient pin type");
    }

    // ─── TEST 4: Line-Following Robot Success ───────────────────
    [Fact]
    public async Task Solver_LineFollowingRobot_Success()
    {
        var board = CreateArduinoUno();

        var components = new List<Component>
        {
            // 2x IR sensors
            new Component
            {
                ComponentId = 1, Type = "ir_sensor", DisplayName = "IR Sensor",
                CurrentDrawMa = 20, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false,
                PinRequirements = new List<ComponentPinRequirement>
                {
                    new() { RequirementId = 1, ComponentId = 1, PinName = "OUT", RequiredCapability = PinCapabilityType.Digital },
                    new() { RequirementId = 2, ComponentId = 1, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V },
                    new() { RequirementId = 3, ComponentId = 1, PinName = "GND", RequiredCapability = PinCapabilityType.Ground }
                }
            },
            new Component
            {
                ComponentId = 1, Type = "ir_sensor", DisplayName = "IR Sensor",
                CurrentDrawMa = 20, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false,
                PinRequirements = new List<ComponentPinRequirement>
                {
                    new() { RequirementId = 4, ComponentId = 1, PinName = "OUT", RequiredCapability = PinCapabilityType.Digital },
                    new() { RequirementId = 5, ComponentId = 1, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V },
                    new() { RequirementId = 6, ComponentId = 1, PinName = "GND", RequiredCapability = PinCapabilityType.Ground }
                }
            },
            // 1x Motor driver
            new Component
            {
                ComponentId = 3, Type = "l298n_motor_driver", DisplayName = "L298N Motor Driver",
                CurrentDrawMa = 50, VoltageMin = 5.0m, VoltageMax = 46.0m,
                RequiresExternalPower = true,
                PinRequirements = new List<ComponentPinRequirement>
                {
                    new() { RequirementId = 10, ComponentId = 3, PinName = "ENA", RequiredCapability = PinCapabilityType.Pwm },
                    new() { RequirementId = 11, ComponentId = 3, PinName = "IN1", RequiredCapability = PinCapabilityType.Digital },
                    new() { RequirementId = 12, ComponentId = 3, PinName = "IN2", RequiredCapability = PinCapabilityType.Digital },
                    new() { RequirementId = 13, ComponentId = 3, PinName = "ENB", RequiredCapability = PinCapabilityType.Pwm },
                    new() { RequirementId = 14, ComponentId = 3, PinName = "IN3", RequiredCapability = PinCapabilityType.Digital },
                    new() { RequirementId = 15, ComponentId = 3, PinName = "IN4", RequiredCapability = PinCapabilityType.Digital }
                }
            }
        };

        // Total current: 20+20+50 = 90mA (well under 500mA)
        var result = await _solver.SolveAsync(board, components);

        result.Success.Should().BeTrue();
        result.PinMapping.Should().HaveCountGreaterOrEqualTo(8); // 2×3 IR + 6 motor driver = 12 total, but 4 are power/gnd
        result.PinMapping.Should().ContainKey("ir_sensor_0.OUT");
        result.PinMapping.Should().ContainKey("ir_sensor_1.OUT");
        result.PinMapping.Should().ContainKey("l298n_motor_driver_0.ENA");
        result.PinMapping.Should().ContainKey("l298n_motor_driver_0.ENB");

        // Verify unique signal pin assignments
        var signalPins = result.PinMapping
            .Where(kv => kv.Value is not ("5V" or "GND" or "3V3"))
            .Select(kv => kv.Value)
            .ToList();
        signalPins.Should().OnlyHaveUniqueItems("no pin can be assigned twice");
    }

    // ─── TEST 5: No Pin Requirements ────────────────────────────
    [Fact]
    public async Task Solver_ComponentWithNoPins_Success()
    {
        var board = CreateArduinoUno();
        var dcMotor = new Component
        {
            ComponentId = 4, Type = "dc_motor", DisplayName = "DC Motor",
            CurrentDrawMa = 200, VoltageMin = 3.0m, VoltageMax = 12.0m,
            RequiresExternalPower = true,
            PinRequirements = new List<ComponentPinRequirement>() // No direct pin requirements
        };

        var result = await _solver.SolveAsync(board, new List<Component> { dcMotor });

        result.Success.Should().BeTrue();
        result.PinMapping.Should().BeEmpty("DC motors connect through a driver, not directly");
    }
}

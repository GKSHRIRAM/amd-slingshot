using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IoTCircuitBuilder.Infrastructure.Data;

public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedBoards(modelBuilder);
        SeedPins(modelBuilder);
        SeedPinCapabilities(modelBuilder);
        SeedComponents(modelBuilder);
        SeedComponentPinRequirements(modelBuilder);
        SeedLibraries(modelBuilder);
        SeedComponentLibraries(modelBuilder);
        SeedCodeTemplates(modelBuilder);
        SeedPowerDistributionRules(modelBuilder);
    }

    private static void SeedBoards(ModelBuilder mb)
    {
        mb.Entity<Board>().HasData(new Board
        {
            BoardId = 1,
            Name = "arduino_uno",
            DisplayName = "Arduino Uno R3",
            Manufacturer = "Arduino",
            Voltage = 5.0m,
            MaxCurrentMa = 500,
            LogicLevelV = 5.0m,
            Is5VTolerant = true,
            ProcessorSpeedHz = 16_000_000,
            FlashMemoryKb = 32,
            SramKb = 2,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }

    private static void SeedPins(ModelBuilder mb)
    {
        var pins = new List<Pin>();
        int id = 1;

        // Digital pins D0-D13
        for (int i = 0; i <= 13; i++)
        {
            pins.Add(new Pin
            {
                PinId = id++,
                BoardId = 1,
                PinIdentifier = $"D{i}",
                PhysicalPosition = i,
                Voltage = 5.0m,
                MaxCurrentMa = 40
            });
        }

        // Analog pins A0-A5
        for (int i = 0; i <= 5; i++)
        {
            pins.Add(new Pin
            {
                PinId = id++,
                BoardId = 1,
                PinIdentifier = $"A{i}",
                PhysicalPosition = 14 + i,
                Voltage = 5.0m,
                MaxCurrentMa = 40
            });
        }

        // Power pins
        pins.Add(new Pin { PinId = id++, BoardId = 1, PinIdentifier = "5V",  PhysicalPosition = 100, Voltage = 5.0m,  MaxCurrentMa = 500 });
        pins.Add(new Pin { PinId = id++, BoardId = 1, PinIdentifier = "3V3", PhysicalPosition = 101, Voltage = 3.3m,  MaxCurrentMa = 50   });
        pins.Add(new Pin { PinId = id++, BoardId = 1, PinIdentifier = "GND", PhysicalPosition = 102, Voltage = 0m,    MaxCurrentMa = 1000  });

        mb.Entity<Pin>().HasData(pins);
    }

    private static void SeedPinCapabilities(ModelBuilder mb)
    {
        var caps = new List<PinCapability>();
        int id = 1;

        // All digital D0-D13 → Digital capability (PinId 1-14)
        for (int pinId = 1; pinId <= 14; pinId++)
        {
            caps.Add(new PinCapability { CapabilityId = id++, PinId = pinId, CapabilityType = PinCapabilityType.Digital });
        }

        // PWM: D3(PinId=4), D5(6), D6(7), D9(10), D10(11), D11(12)
        int[] pwmPinIds = { 4, 6, 7, 10, 11, 12 };
        foreach (var pinId in pwmPinIds)
        {
            caps.Add(new PinCapability { CapabilityId = id++, PinId = pinId, CapabilityType = PinCapabilityType.Pwm });
        }

        // Analog: A0-A5 (PinId 15-20)
        for (int pinId = 15; pinId <= 20; pinId++)
        {
            caps.Add(new PinCapability { CapabilityId = id++, PinId = pinId, CapabilityType = PinCapabilityType.Analog });
            // Analog pins can also do digital
            caps.Add(new PinCapability { CapabilityId = id++, PinId = pinId, CapabilityType = PinCapabilityType.Digital });
        }

        // I2C: A4(PinId=19) = SDA, A5(PinId=20) = SCL
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 19, CapabilityType = PinCapabilityType.I2cSda });
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 20, CapabilityType = PinCapabilityType.I2cScl });

        // SPI: D10(11)=SS, D11(12)=MOSI, D12(13)=MISO, D13(14)=SCK
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 12, CapabilityType = PinCapabilityType.SpiMosi });
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 13, CapabilityType = PinCapabilityType.SpiMiso });
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 14, CapabilityType = PinCapabilityType.SpiSck });

        // UART: D0(1)=RX, D1(2)=TX
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 1, CapabilityType = PinCapabilityType.UartRx });
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 2, CapabilityType = PinCapabilityType.UartTx });

        // Power pins
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 21, CapabilityType = PinCapabilityType.Power5V });   // 5V
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 22, CapabilityType = PinCapabilityType.Power3V3 });  // 3V3
        caps.Add(new PinCapability { CapabilityId = id++, PinId = 23, CapabilityType = PinCapabilityType.Ground });    // GND

        mb.Entity<PinCapability>().HasData(caps);
    }

    private static void SeedComponents(ModelBuilder mb)
    {
        mb.Entity<Component>().HasData(
            new Component
            {
                ComponentId = 1, Type = "ir_sensor", DisplayName = "TCRT5000 IR Proximity Sensor",
                Category = "sensor", CurrentDrawMa = 20, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 2, Type = "hc_sr04_ultrasonic", DisplayName = "HC-SR04 Ultrasonic Distance Sensor",
                Category = "sensor", CurrentDrawMa = 15, VoltageMin = 5.0m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 3, Type = "l298n_motor_driver", DisplayName = "L298N Dual H-Bridge Motor Driver",
                Category = "motor_driver", CurrentDrawMa = 50, VoltageMin = 5.0m, VoltageMax = 46.0m,
                RequiresExternalPower = true, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 4, Type = "dc_motor", DisplayName = "Generic DC Motor",
                Category = "actuator", CurrentDrawMa = 200, VoltageMin = 3.0m, VoltageMax = 12.0m,
                RequiresExternalPower = true, InterfaceProtocol = "driver",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 5, Type = "sg90_servo", DisplayName = "SG90 Micro Servo",
                Category = "actuator", CurrentDrawMa = 100, VoltageMin = 4.8m, VoltageMax = 6.0m,
                RequiresExternalPower = false, InterfaceProtocol = "pwm",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 6, Type = "led_red", DisplayName = "Red LED 5mm",
                Category = "indicator", CurrentDrawMa = 20, VoltageMin = 2.0m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // ─── NEW COMPONENTS ──────────────────────────────────
            new Component
            {
                ComponentId = 7, Type = "potentiometer", DisplayName = "10K Potentiometer",
                Category = "input", CurrentDrawMa = 1, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "analog",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 8, Type = "bme280", DisplayName = "BME280 Temperature/Humidity/Pressure Sensor",
                Category = "sensor", CurrentDrawMa = 1, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "i2c",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 9, Type = "oled_128x64", DisplayName = "SSD1306 OLED Display 0.96\" 128x64",
                Category = "display", CurrentDrawMa = 20, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "i2c",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 10, Type = "ldr_sensor", DisplayName = "LDR Light Sensor (Photoresistor)",
                Category = "sensor", CurrentDrawMa = 1, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "analog",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 11, Type = "dht11", DisplayName = "DHT11 Temperature & Humidity Sensor",
                Category = "sensor", CurrentDrawMa = 2, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 12, Type = "buzzer", DisplayName = "Piezo Buzzer",
                Category = "actuator", CurrentDrawMa = 30, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 13, Type = "push_button", DisplayName = "Momentary Push Button",
                Category = "input", CurrentDrawMa = 0, VoltageMin = 3.3m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Component
            {
                ComponentId = 14, Type = "relay_module", DisplayName = "5V Relay Module",
                Category = "actuator", CurrentDrawMa = 75, VoltageMin = 5.0m, VoltageMax = 5.0m,
                RequiresExternalPower = false, InterfaceProtocol = "digital",
                IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    private static void SeedComponentPinRequirements(ModelBuilder mb)
    {
        var reqs = new List<ComponentPinRequirement>();
        int id = 1;

        // IR Sensor (ComponentId=1): OUT, VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 1, PinName = "OUT", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 1, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 1, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // HC-SR04 (ComponentId=2): TRIG, ECHO, VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 2, PinName = "TRIG", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 2, PinName = "ECHO", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 2, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 2, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // L298N (ComponentId=3): ENA(PWM), IN1-IN4(Digital), ENB(PWM)
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 3, PinName = "ENA", RequiredCapability = PinCapabilityType.Pwm });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 3, PinName = "IN1", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 3, PinName = "IN2", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 3, PinName = "ENB", RequiredCapability = PinCapabilityType.Pwm });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 3, PinName = "IN3", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 3, PinName = "IN4", RequiredCapability = PinCapabilityType.Digital });

        // DC Motor (ComponentId=4): No direct pin requirements (connects to L298N driver)

        // SG90 Servo (ComponentId=5): SIGNAL(PWM), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 5, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Pwm });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 5, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 5, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // LED (ComponentId=6): ANODE(Digital), CATHODE(GND)
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 6, PinName = "ANODE", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 6, PinName = "CATHODE", RequiredCapability = PinCapabilityType.Ground });

        // ─── NEW COMPONENT PIN REQUIREMENTS ──────────────────────

        // Potentiometer (ComponentId=7): SIGNAL(Analog), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 7, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Analog });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 7, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 7, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // BME280 (ComponentId=8): SDA(I2C), SCL(I2C), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 8, PinName = "SDA", RequiredCapability = PinCapabilityType.I2cSda });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 8, PinName = "SCL", RequiredCapability = PinCapabilityType.I2cScl });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 8, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 8, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // OLED SSD1306 (ComponentId=9): SDA(I2C), SCL(I2C), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 9, PinName = "SDA", RequiredCapability = PinCapabilityType.I2cSda });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 9, PinName = "SCL", RequiredCapability = PinCapabilityType.I2cScl });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 9, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 9, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // LDR Light Sensor (ComponentId=10): SIGNAL(Analog), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 10, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Analog });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 10, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 10, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // DHT11 (ComponentId=11): DATA(Digital), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 11, PinName = "DATA", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 11, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 11, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // Buzzer (ComponentId=12): SIGNAL(Digital), GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 12, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 12, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // Push Button (ComponentId=13): SIGNAL(Digital), GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 13, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 13, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        // Relay Module (ComponentId=14): SIGNAL(Digital), VCC, GND
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 14, PinName = "SIGNAL", RequiredCapability = PinCapabilityType.Digital });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 14, PinName = "VCC", RequiredCapability = PinCapabilityType.Power5V });
        reqs.Add(new ComponentPinRequirement { RequirementId = id++, ComponentId = 14, PinName = "GND", RequiredCapability = PinCapabilityType.Ground });

        mb.Entity<ComponentPinRequirement>().HasData(reqs);
    }

    private static void SeedLibraries(ModelBuilder mb)
    {
        mb.Entity<Library>().HasData(
            new Library { LibraryId = 1, Name = "NewPing", Version = "1.9.7", GithubUrl = "https://github.com/microflo/NewPing", InstallCommand = "#include <NewPing.h>", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Library { LibraryId = 2, Name = "Servo", Version = "1.2.1", GithubUrl = "https://github.com/arduino-libraries/Servo", InstallCommand = "#include <Servo.h>", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Library { LibraryId = 3, Name = "Wire", Version = "builtin", GithubUrl = "https://www.arduino.cc/reference/en/language/functions/communication/wire/", InstallCommand = "#include <Wire.h>", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Library { LibraryId = 4, Name = "Adafruit_BME280", Version = "2.2.4", GithubUrl = "https://github.com/adafruit/Adafruit_BME280_Library", InstallCommand = "#include <Adafruit_BME280.h>", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Library { LibraryId = 5, Name = "Adafruit_SSD1306", Version = "2.5.9", GithubUrl = "https://github.com/adafruit/Adafruit_SSD1306", InstallCommand = "#include <Adafruit_SSD1306.h>", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Library { LibraryId = 6, Name = "DHT", Version = "1.4.6", GithubUrl = "https://github.com/adafruit/DHT-sensor-library", InstallCommand = "#include <DHT.h>", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }

    private static void SeedComponentLibraries(ModelBuilder mb)
    {
        mb.Entity<ComponentLibrary>().HasData(
            new ComponentLibrary { ComponentLibraryId = 1, ComponentId = 2, LibraryId = 1, IsRequired = true },  // HC-SR04 → NewPing
            new ComponentLibrary { ComponentLibraryId = 2, ComponentId = 5, LibraryId = 2, IsRequired = true },  // Servo → Servo.h
            new ComponentLibrary { ComponentLibraryId = 3, ComponentId = 8, LibraryId = 3, IsRequired = true },  // BME280 → Wire.h
            new ComponentLibrary { ComponentLibraryId = 4, ComponentId = 8, LibraryId = 4, IsRequired = true },  // BME280 → Adafruit_BME280
            new ComponentLibrary { ComponentLibraryId = 5, ComponentId = 9, LibraryId = 3, IsRequired = true },  // OLED → Wire.h
            new ComponentLibrary { ComponentLibraryId = 6, ComponentId = 9, LibraryId = 5, IsRequired = true },  // OLED → SSD1306
            new ComponentLibrary { ComponentLibraryId = 7, ComponentId = 11, LibraryId = 6, IsRequired = true }  // DHT11 → DHT
        );
    }

    private static void SeedCodeTemplates(ModelBuilder mb)
    {
        mb.Entity<CodeTemplate>().HasData(
            // ═══════════════════════════════════════════════════════════════
            //  IR Sensor — reads digital output, prints detection status
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 1, ComponentId = 1, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "pinMode({{ pin_out }}, INPUT);  // {{ display_name }}" },
            new CodeTemplate { TemplateId = 2, ComponentId = 1, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"int ir_{{ instance_id }} = digitalRead({{ pin_out }});
Serial.print(""IR Sensor {{ instance_id }}: "");
Serial.println(ir_{{ instance_id }} == LOW ? ""DETECTED"" : ""clear"");" },

            // ═══════════════════════════════════════════════════════════════
            //  HC-SR04 Ultrasonic — measures distance, prints to serial
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 3, ComponentId = 2, TemplateType = "declaration", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "NewPing sonar_{{ instance_id }}({{ pin_trig }}, {{ pin_echo }}, 200);" },
            new CodeTemplate { TemplateId = 4, ComponentId = 2, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"unsigned int distance_{{ instance_id }} = sonar_{{ instance_id }}.ping_cm();
Serial.print(""Distance {{ instance_id }}: "");
Serial.print(distance_{{ instance_id }});
Serial.println("" cm"");" },

            // ═══════════════════════════════════════════════════════════════
            //  L298N Motor Driver — sets up pins, runs motors forward
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 5, ComponentId = 3, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"pinMode({{ pin_ena }}, OUTPUT);  // Motor A speed (PWM)
pinMode({{ pin_in1 }}, OUTPUT);  // Motor A direction
pinMode({{ pin_in2 }}, OUTPUT);
pinMode({{ pin_enb }}, OUTPUT);  // Motor B speed (PWM)
pinMode({{ pin_in3 }}, OUTPUT);  // Motor B direction
pinMode({{ pin_in4 }}, OUTPUT);" },
            new CodeTemplate { TemplateId = 23, ComponentId = 3, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"// Drive both motors FORWARD at 75% speed
digitalWrite({{ pin_in1 }}, HIGH);
digitalWrite({{ pin_in2 }}, LOW);
analogWrite({{ pin_ena }}, 190);  // 0-255 speed

digitalWrite({{ pin_in3 }}, HIGH);
digitalWrite({{ pin_in4 }}, LOW);
analogWrite({{ pin_enb }}, 190);

Serial.println(""Motors: FORWARD at 75%"");" },

            // ═══════════════════════════════════════════════════════════════
            //  Servo — declaration, attach, sweep in loop
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 6, ComponentId = 5, TemplateType = "declaration", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "Servo servo_{{ instance_id }};" },
            new CodeTemplate { TemplateId = 7, ComponentId = 5, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"servo_{{ instance_id }}.attach({{ pin_signal }});
servo_{{ instance_id }}.write(90);  // Center position" },
            new CodeTemplate { TemplateId = 24, ComponentId = 5, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"// Sweep servo {{ instance_id }}: 0° → 180° → 0°
for (int angle = 0; angle <= 180; angle += 5) {
  servo_{{ instance_id }}.write(angle);
  delay(15);
}
for (int angle = 180; angle >= 0; angle -= 5) {
  servo_{{ instance_id }}.write(angle);
  delay(15);
}" },

            // ═══════════════════════════════════════════════════════════════
            //  Red LED — blink pattern
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 8, ComponentId = 6, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "pinMode({{ pin_anode }}, OUTPUT);  // {{ display_name }}" },
            new CodeTemplate { TemplateId = 25, ComponentId = 6, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"digitalWrite({{ pin_anode }}, HIGH);  // LED {{ instance_id }} ON
delay(500);
digitalWrite({{ pin_anode }}, LOW);   // LED {{ instance_id }} OFF
delay(500);" },

            // ═══════════════════════════════════════════════════════════════
            //  Potentiometer — reads analog value, prints mapped range
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 9, ComponentId = 7, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"int pot_{{ instance_id }} = analogRead({{ pin_signal }});  // 0-1023
int mapped_{{ instance_id }} = map(pot_{{ instance_id }}, 0, 1023, 0, 180);  // Map to 0-180
Serial.print(""Pot {{ instance_id }}: "");
Serial.print(pot_{{ instance_id }});
Serial.print("" → mapped: "");
Serial.println(mapped_{{ instance_id }});" },

            // ═══════════════════════════════════════════════════════════════
            //  BME280 — reads temp/humidity/pressure, prints all values
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 10, ComponentId = 8, TemplateType = "declaration", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "Adafruit_BME280 bme_{{ instance_id }};" },
            new CodeTemplate { TemplateId = 11, ComponentId = 8, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"Wire.begin();
if (!bme_{{ instance_id }}.begin(0x76)) {
  Serial.println(""BME280 not found! Check wiring."");
  while (1) delay(10);  // Halt if sensor missing
}" },
            new CodeTemplate { TemplateId = 12, ComponentId = 8, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"float temp_{{ instance_id }} = bme_{{ instance_id }}.readTemperature();
float hum_{{ instance_id }} = bme_{{ instance_id }}.readHumidity();
float pres_{{ instance_id }} = bme_{{ instance_id }}.readPressure() / 100.0F;
Serial.print(""Temp: ""); Serial.print(temp_{{ instance_id }}); Serial.print(""°C  "");
Serial.print(""Humidity: ""); Serial.print(hum_{{ instance_id }}); Serial.print(""%  "");
Serial.print(""Pressure: ""); Serial.print(pres_{{ instance_id }}); Serial.println("" hPa"");" },

            // ═══════════════════════════════════════════════════════════════
            //  SSD1306 OLED — draws text to screen in loop
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 13, ComponentId = 9, TemplateType = "declaration", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "#define SCREEN_WIDTH 128\n#define SCREEN_HEIGHT 64\nAdafruit_SSD1306 display_{{ instance_id }}(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, -1);" },
            new CodeTemplate { TemplateId = 14, ComponentId = 9, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"if (!display_{{ instance_id }}.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
  Serial.println(""OLED not found! Check wiring."");
  while (1) delay(10);  // Halt if display missing
}
display_{{ instance_id }}.clearDisplay();
display_{{ instance_id }}.setTextSize(1);
display_{{ instance_id }}.setTextColor(SSD1306_WHITE);" },
            new CodeTemplate { TemplateId = 26, ComponentId = 9, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"display_{{ instance_id }}.clearDisplay();
display_{{ instance_id }}.setCursor(0, 0);
display_{{ instance_id }}.println(""IoT Circuit Builder"");
display_{{ instance_id }}.println(""──────────────────"");
display_{{ instance_id }}.print(""Uptime: "");
display_{{ instance_id }}.print(millis() / 1000);
display_{{ instance_id }}.println(""s"");
display_{{ instance_id }}.display();" },

            // ═══════════════════════════════════════════════════════════════
            //  LDR Light Sensor — reads analog, prints light level
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 15, ComponentId = 10, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"int light_{{ instance_id }} = analogRead({{ pin_signal }});  // 0=dark, 1023=bright
Serial.print(""Light {{ instance_id }}: "");
Serial.print(light_{{ instance_id }});
Serial.println(light_{{ instance_id }} < 300 ? "" (DARK)"" : "" (BRIGHT)"");" },

            // ═══════════════════════════════════════════════════════════════
            //  DHT11 — reads temp + humidity with NaN check
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 16, ComponentId = 11, TemplateType = "declaration", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "DHT dht_{{ instance_id }}({{ pin_data }}, DHT11);" },
            new CodeTemplate { TemplateId = 17, ComponentId = 11, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "dht_{{ instance_id }}.begin();" },
            new CodeTemplate { TemplateId = 18, ComponentId = 11, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"float temp_{{ instance_id }} = dht_{{ instance_id }}.readTemperature();
float hum_{{ instance_id }} = dht_{{ instance_id }}.readHumidity();
if (isnan(temp_{{ instance_id }}) || isnan(hum_{{ instance_id }})) {
  Serial.println(""DHT {{ instance_id }}: Read failed!"");
} else {
  Serial.print(""DHT {{ instance_id }}: "");
  Serial.print(temp_{{ instance_id }}); Serial.print(""°C  "");
  Serial.print(hum_{{ instance_id }}); Serial.println(""%"");
}" },

            // ═══════════════════════════════════════════════════════════════
            //  Buzzer — plays tone pattern
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 19, ComponentId = 12, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "pinMode({{ pin_signal }}, OUTPUT);  // {{ display_name }}" },
            new CodeTemplate { TemplateId = 27, ComponentId = 12, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"tone({{ pin_signal }}, 1000, 200);  // 1kHz beep for 200ms
delay(300);
tone({{ pin_signal }}, 1500, 200);  // 1.5kHz beep
delay(300);
noTone({{ pin_signal }});
Serial.println(""Buzzer: beep pattern"");" },

            // ═══════════════════════════════════════════════════════════════
            //  Push Button — reads state, controls LED-like behavior
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 20, ComponentId = 13, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = "pinMode({{ pin_signal }}, INPUT_PULLUP);  // {{ display_name }} (LOW = pressed)" },
            new CodeTemplate { TemplateId = 21, ComponentId = 13, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"bool btn_{{ instance_id }} = !digitalRead({{ pin_signal }});  // LOW = pressed
if (btn_{{ instance_id }}) {
  Serial.println(""Button {{ instance_id }}: PRESSED"");
}" },

            // ═══════════════════════════════════════════════════════════════
            //  Relay Module — toggles on/off with 2-second interval
            // ═══════════════════════════════════════════════════════════════
            new CodeTemplate { TemplateId = 22, ComponentId = 14, TemplateType = "setup", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"pinMode({{ pin_signal }}, OUTPUT);
digitalWrite({{ pin_signal }}, HIGH);  // Relay OFF (active LOW)" },
            new CodeTemplate { TemplateId = 28, ComponentId = 14, TemplateType = "loop", Language = "cpp", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TemplateContent = @"digitalWrite({{ pin_signal }}, LOW);   // Relay ON
Serial.println(""Relay {{ instance_id }}: ON"");
delay(2000);
digitalWrite({{ pin_signal }}, HIGH);  // Relay OFF
Serial.println(""Relay {{ instance_id }}: OFF"");
delay(2000);" }
        );
    }

    private static void SeedPowerDistributionRules(ModelBuilder mb)
    {
        mb.Entity<PowerDistributionRule>().HasData(
            new PowerDistributionRule { RuleId = 1, BoardId = 1, PowerSource = "usb", MaxCurrentMa = 500, VoltageV = 5.0m, Description = "USB 2.0 power supply" },
            new PowerDistributionRule { RuleId = 2, BoardId = 1, PowerSource = "barrel", MaxCurrentMa = 1000, VoltageV = 5.0m, Description = "Barrel jack via onboard regulator" },
            new PowerDistributionRule { RuleId = 3, BoardId = 1, PowerSource = "3v3_pin", MaxCurrentMa = 50, VoltageV = 3.3m, Description = "3.3V rail from onboard LDO" }
        );
    }
}

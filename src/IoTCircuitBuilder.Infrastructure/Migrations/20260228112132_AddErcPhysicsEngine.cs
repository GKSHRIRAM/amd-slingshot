using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IoTCircuitBuilder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErcPhysicsEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    Voltage = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    MaxCurrentMa = table.Column<int>(type: "INTEGER", nullable: false),
                    LogicLevelV = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    Is5VTolerant = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessorSpeedHz = table.Column<int>(type: "INTEGER", nullable: true),
                    FlashMemoryKb = table.Column<int>(type: "INTEGER", nullable: true),
                    SramKb = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.BoardId);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentDrawMa = table.Column<int>(type: "INTEGER", nullable: false),
                    VoltageMin = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    VoltageMax = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    RequiresExternalPower = table.Column<bool>(type: "INTEGER", nullable: false),
                    InterfaceProtocol = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresLevelShifter = table.Column<bool>(type: "INTEGER", nullable: false),
                    DatasheetUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.ComponentId);
                });

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    LibraryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    GithubUrl = table.Column<string>(type: "TEXT", nullable: true),
                    InstallCommand = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeprecated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libraries", x => x.LibraryId);
                });

            migrationBuilder.CreateTable(
                name: "Pins",
                columns: table => new
                {
                    PinId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    PinIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    PhysicalPosition = table.Column<int>(type: "INTEGER", nullable: true),
                    Voltage = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    MaxCurrentMa = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseErcType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pins", x => x.PinId);
                    table.ForeignKey(
                        name: "FK_Pins_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "BoardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PowerDistributionRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    PowerSource = table.Column<string>(type: "TEXT", nullable: false),
                    MaxCurrentMa = table.Column<int>(type: "INTEGER", nullable: false),
                    VoltageV = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerDistributionRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_PowerDistributionRules_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "BoardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateContent = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeTemplates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_CodeTemplates_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "ComponentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentPinRequirements",
                columns: table => new
                {
                    RequirementId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    PinName = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredCapability = table.Column<string>(type: "TEXT", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ErcType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentPinRequirements", x => x.RequirementId);
                    table.ForeignKey(
                        name: "FK_ComponentPinRequirements_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "ComponentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "I2cAddresses",
                columns: table => new
                {
                    I2cAddressId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultAddress = table.Column<string>(type: "TEXT", nullable: false),
                    AlternateAddresses = table.Column<string>(type: "TEXT", nullable: true),
                    IsConfigurable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I2cAddresses", x => x.I2cAddressId);
                    table.ForeignKey(
                        name: "FK_I2cAddresses_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "ComponentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentLibraries",
                columns: table => new
                {
                    ComponentLibraryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LibraryId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentLibraries", x => x.ComponentLibraryId);
                    table.ForeignKey(
                        name: "FK_ComponentLibraries_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "ComponentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentLibraries_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "LibraryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PinCapabilities",
                columns: table => new
                {
                    CapabilityId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PinId = table.Column<int>(type: "INTEGER", nullable: false),
                    CapabilityType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinCapabilities", x => x.CapabilityId);
                    table.ForeignKey(
                        name: "FK_PinCapabilities_Pins_PinId",
                        column: x => x.PinId,
                        principalTable: "Pins",
                        principalColumn: "PinId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Boards",
                columns: new[] { "BoardId", "CreatedAt", "DisplayName", "FlashMemoryKb", "Is5VTolerant", "IsActive", "LogicLevelV", "Manufacturer", "MaxCurrentMa", "Name", "ProcessorSpeedHz", "SramKb", "Voltage" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arduino Uno R3", 32, true, true, 5.0m, "Arduino", 500, "arduino_uno", 16000000, 2, 5.0m });

            migrationBuilder.InsertData(
                table: "Components",
                columns: new[] { "ComponentId", "Category", "CreatedAt", "CurrentDrawMa", "DatasheetUrl", "Description", "DisplayName", "ImageUrl", "InterfaceProtocol", "IsActive", "Manufacturer", "RequiresExternalPower", "RequiresLevelShifter", "Type", "VoltageMax", "VoltageMin" },
                values: new object[,]
                {
                    { 1, "sensor", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, null, null, "TCRT5000 IR Proximity Sensor", null, "digital", true, null, false, false, "ir_sensor", 5.0m, 3.3m },
                    { 2, "sensor", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, null, null, "HC-SR04 Ultrasonic Distance Sensor", null, "digital", true, null, false, false, "hc_sr04_ultrasonic", 5.0m, 5.0m },
                    { 3, "motor_driver", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 50, null, null, "L298N Dual H-Bridge Motor Driver", null, "digital", true, null, true, false, "l298n_motor_driver", 46.0m, 5.0m },
                    { 4, "actuator", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 200, null, null, "Generic DC Motor", null, "driver", true, null, true, false, "dc_motor", 12.0m, 3.0m },
                    { 5, "actuator", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 100, null, null, "SG90 Micro Servo", null, "pwm", true, null, false, false, "sg90_servo", 6.0m, 4.8m },
                    { 6, "indicator", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, null, null, "Red LED 5mm", null, "digital", true, null, false, false, "led_red", 5.0m, 2.0m },
                    { 7, "input", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "10K Potentiometer", null, "analog", true, null, false, false, "potentiometer", 5.0m, 3.3m },
                    { 8, "sensor", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "BME280 Temperature/Humidity/Pressure Sensor", null, "i2c", true, null, false, false, "bme280", 5.0m, 3.3m },
                    { 9, "display", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, null, null, "SSD1306 OLED Display 0.96\" 128x64", null, "i2c", true, null, false, false, "oled_128x64", 5.0m, 3.3m },
                    { 10, "sensor", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "LDR Light Sensor (Photoresistor)", null, "analog", true, null, false, false, "ldr_sensor", 5.0m, 3.3m },
                    { 11, "sensor", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "DHT11 Temperature & Humidity Sensor", null, "digital", true, null, false, false, "dht11", 5.0m, 3.3m },
                    { 12, "actuator", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, null, "Piezo Buzzer", null, "digital", true, null, false, false, "buzzer", 5.0m, 3.3m },
                    { 13, "input", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Momentary Push Button", null, "digital", true, null, false, false, "push_button", 5.0m, 3.3m },
                    { 14, "actuator", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 75, null, null, "5V Relay Module", null, "digital", true, null, false, false, "relay_module", 5.0m, 5.0m },
                    { 15, "power", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "9V Battery", null, "power", true, null, false, false, "battery_9v", 9.0m, 9.0m },
                    { 16, "passive", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Resistor (Inline)", null, "analog", true, null, false, false, "resistor", 100m, 0m },
                    { 17, "passive", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Diode (1N4001)", null, "analog", true, null, false, false, "diode", 100m, 0m },
                    { 18, "passive", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Ceramic Capacitor (100nF)", null, "analog", true, null, false, false, "capacitor_ceramic", 50m, 0m },
                    { 19, "passive", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Electrolytic Capacitor (10uF)", null, "analog", true, null, false, false, "capacitor_electrolytic", 50m, 0m }
                });

            migrationBuilder.InsertData(
                table: "Libraries",
                columns: new[] { "LibraryId", "CreatedAt", "GithubUrl", "InstallCommand", "IsDeprecated", "Name", "Version" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://github.com/microflo/NewPing", "#include <NewPing.h>", false, "NewPing", "1.9.7" },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://github.com/arduino-libraries/Servo", "#include <Servo.h>", false, "Servo", "1.2.1" },
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://www.arduino.cc/reference/en/language/functions/communication/wire/", "#include <Wire.h>", false, "Wire", "builtin" },
                    { 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://github.com/adafruit/Adafruit_BME280_Library", "#include <Adafruit_BME280.h>", false, "Adafruit_BME280", "2.2.4" },
                    { 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://github.com/adafruit/Adafruit_SSD1306", "#include <Adafruit_SSD1306.h>", false, "Adafruit_SSD1306", "2.5.9" },
                    { 6, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://github.com/adafruit/DHT-sensor-library", "#include <DHT.h>", false, "DHT", "1.4.6" }
                });

            migrationBuilder.InsertData(
                table: "CodeTemplates",
                columns: new[] { "TemplateId", "ComponentId", "CreatedAt", "Language", "TemplateContent", "TemplateType" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "pinMode({{ pin_out }}, INPUT);  // {{ display_name }}", "setup" },
                    { 2, 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "int ir_{{ instance_id }} = digitalRead({{ pin_out }});\r\nSerial.print(\"IR Sensor {{ instance_id }}: \");\r\nSerial.println(ir_{{ instance_id }} == LOW ? \"DETECTED\" : \"clear\");", "loop" },
                    { 3, 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "NewPing sonar_{{ instance_id }}({{ pin_trig }}, {{ pin_echo }}, 200);", "declaration" },
                    { 4, 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "unsigned int distance_{{ instance_id }} = sonar_{{ instance_id }}.ping_cm();\r\nSerial.print(\"Distance {{ instance_id }}: \");\r\nSerial.print(distance_{{ instance_id }});\r\nSerial.println(\" cm\");", "loop" },
                    { 5, 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "pinMode({{ pin_ena }}, OUTPUT);  // Motor A speed (PWM)\r\npinMode({{ pin_in1 }}, OUTPUT);  // Motor A direction\r\npinMode({{ pin_in2 }}, OUTPUT);\r\npinMode({{ pin_enb }}, OUTPUT);  // Motor B speed (PWM)\r\npinMode({{ pin_in3 }}, OUTPUT);  // Motor B direction\r\npinMode({{ pin_in4 }}, OUTPUT);", "setup" },
                    { 6, 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "Servo servo_{{ instance_id }};", "declaration" },
                    { 7, 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "servo_{{ instance_id }}.attach({{ pin_signal }});\r\nservo_{{ instance_id }}.write(90);  // Center position", "setup" },
                    { 8, 6, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "pinMode({{ pin_anode }}, OUTPUT);  // {{ display_name }}", "setup" },
                    { 9, 7, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "int pot_{{ instance_id }} = analogRead({{ pin_signal }});  // 0-1023\r\nint mapped_{{ instance_id }} = map(pot_{{ instance_id }}, 0, 1023, 0, 180);  // Map to 0-180\r\nSerial.print(\"Pot {{ instance_id }}: \");\r\nSerial.print(pot_{{ instance_id }});\r\nSerial.print(\" → mapped: \");\r\nSerial.println(mapped_{{ instance_id }});", "loop" },
                    { 10, 8, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "Adafruit_BME280 bme_{{ instance_id }};", "declaration" },
                    { 11, 8, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "Wire.begin();\r\nif (!bme_{{ instance_id }}.begin(0x76)) {\r\n  Serial.println(\"BME280 not found! Check wiring.\");\r\n  while (1) delay(10);  // Halt if sensor missing\r\n}", "setup" },
                    { 12, 8, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "float temp_{{ instance_id }} = bme_{{ instance_id }}.readTemperature();\r\nfloat hum_{{ instance_id }} = bme_{{ instance_id }}.readHumidity();\r\nfloat pres_{{ instance_id }} = bme_{{ instance_id }}.readPressure() / 100.0F;\r\nSerial.print(\"Temp: \"); Serial.print(temp_{{ instance_id }}); Serial.print(\"°C  \");\r\nSerial.print(\"Humidity: \"); Serial.print(hum_{{ instance_id }}); Serial.print(\"%  \");\r\nSerial.print(\"Pressure: \"); Serial.print(pres_{{ instance_id }}); Serial.println(\" hPa\");", "loop" },
                    { 13, 9, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "#define SCREEN_WIDTH 128\n#define SCREEN_HEIGHT 64\nAdafruit_SSD1306 display_{{ instance_id }}(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, -1);", "declaration" },
                    { 14, 9, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "if (!display_{{ instance_id }}.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {\r\n  Serial.println(\"OLED not found! Check wiring.\");\r\n  while (1) delay(10);  // Halt if display missing\r\n}\r\ndisplay_{{ instance_id }}.clearDisplay();\r\ndisplay_{{ instance_id }}.setTextSize(1);\r\ndisplay_{{ instance_id }}.setTextColor(SSD1306_WHITE);", "setup" },
                    { 15, 10, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "int light_{{ instance_id }} = analogRead({{ pin_signal }});  // 0=dark, 1023=bright\r\nSerial.print(\"Light {{ instance_id }}: \");\r\nSerial.print(light_{{ instance_id }});\r\nSerial.println(light_{{ instance_id }} < 300 ? \" (DARK)\" : \" (BRIGHT)\");", "loop" },
                    { 16, 11, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "DHT dht_{{ instance_id }}({{ pin_data }}, DHT11);", "declaration" },
                    { 17, 11, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "dht_{{ instance_id }}.begin();", "setup" },
                    { 18, 11, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "float temp_{{ instance_id }} = dht_{{ instance_id }}.readTemperature();\r\nfloat hum_{{ instance_id }} = dht_{{ instance_id }}.readHumidity();\r\nif (isnan(temp_{{ instance_id }}) || isnan(hum_{{ instance_id }})) {\r\n  Serial.println(\"DHT {{ instance_id }}: Read failed!\");\r\n} else {\r\n  Serial.print(\"DHT {{ instance_id }}: \");\r\n  Serial.print(temp_{{ instance_id }}); Serial.print(\"°C  \");\r\n  Serial.print(hum_{{ instance_id }}); Serial.println(\"%\");\r\n}", "loop" },
                    { 19, 12, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "pinMode({{ pin_signal }}, OUTPUT);  // {{ display_name }}", "setup" },
                    { 20, 13, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "pinMode({{ pin_signal }}, INPUT_PULLUP);  // {{ display_name }} (LOW = pressed)", "setup" },
                    { 21, 13, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "bool btn_{{ instance_id }} = !digitalRead({{ pin_signal }});  // LOW = pressed\r\nif (btn_{{ instance_id }}) {\r\n  Serial.println(\"Button {{ instance_id }}: PRESSED\");\r\n}", "loop" },
                    { 22, 14, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "pinMode({{ pin_signal }}, OUTPUT);\r\ndigitalWrite({{ pin_signal }}, HIGH);  // Relay OFF (active LOW)", "setup" },
                    { 23, 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "// Drive both motors FORWARD at 75% speed\r\ndigitalWrite({{ pin_in1 }}, HIGH);\r\ndigitalWrite({{ pin_in2 }}, LOW);\r\nanalogWrite({{ pin_ena }}, 190);  // 0-255 speed\r\n\r\ndigitalWrite({{ pin_in3 }}, HIGH);\r\ndigitalWrite({{ pin_in4 }}, LOW);\r\nanalogWrite({{ pin_enb }}, 190);\r\n\r\nSerial.println(\"Motors: FORWARD at 75%\");", "loop" },
                    { 24, 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "// Sweep servo {{ instance_id }}: 0° → 180° → 0°\r\nfor (int angle = 0; angle <= 180; angle += 5) {\r\n  servo_{{ instance_id }}.write(angle);\r\n  delay(15);\r\n}\r\nfor (int angle = 180; angle >= 0; angle -= 5) {\r\n  servo_{{ instance_id }}.write(angle);\r\n  delay(15);\r\n}", "loop" },
                    { 25, 6, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "digitalWrite({{ pin_anode }}, HIGH);  // LED {{ instance_id }} ON\r\ndelay(500);\r\ndigitalWrite({{ pin_anode }}, LOW);   // LED {{ instance_id }} OFF\r\ndelay(500);", "loop" },
                    { 26, 9, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "display_{{ instance_id }}.clearDisplay();\r\ndisplay_{{ instance_id }}.setCursor(0, 0);\r\ndisplay_{{ instance_id }}.println(\"IoT Circuit Builder\");\r\ndisplay_{{ instance_id }}.println(\"──────────────────\");\r\ndisplay_{{ instance_id }}.print(\"Uptime: \");\r\ndisplay_{{ instance_id }}.print(millis() / 1000);\r\ndisplay_{{ instance_id }}.println(\"s\");\r\ndisplay_{{ instance_id }}.display();", "loop" },
                    { 27, 12, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "tone({{ pin_signal }}, 1000, 200);  // 1kHz beep for 200ms\r\ndelay(300);\r\ntone({{ pin_signal }}, 1500, 200);  // 1.5kHz beep\r\ndelay(300);\r\nnoTone({{ pin_signal }});\r\nSerial.println(\"Buzzer: beep pattern\");", "loop" },
                    { 28, 14, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpp", "digitalWrite({{ pin_signal }}, LOW);   // Relay ON\r\nSerial.println(\"Relay {{ instance_id }}: ON\");\r\ndelay(2000);\r\ndigitalWrite({{ pin_signal }}, HIGH);  // Relay OFF\r\nSerial.println(\"Relay {{ instance_id }}: OFF\");\r\ndelay(2000);", "loop" }
                });

            migrationBuilder.InsertData(
                table: "ComponentLibraries",
                columns: new[] { "ComponentLibraryId", "ComponentId", "IsRequired", "LibraryId" },
                values: new object[,]
                {
                    { 1, 2, true, 1 },
                    { 2, 5, true, 2 },
                    { 3, 8, true, 3 },
                    { 4, 8, true, 4 },
                    { 5, 9, true, 3 },
                    { 6, 9, true, 5 },
                    { 7, 11, true, 6 }
                });

            migrationBuilder.InsertData(
                table: "ComponentPinRequirements",
                columns: new[] { "RequirementId", "ComponentId", "Description", "ErcType", "IsOptional", "PinName", "RequiredCapability" },
                values: new object[,]
                {
                    { 1, 1, null, 2, false, "OUT", "Digital" },
                    { 2, 1, null, 4, false, "VCC", "Power5V" },
                    { 3, 1, null, 4, false, "GND", "Ground" },
                    { 4, 2, null, 1, false, "TRIG", "Digital" },
                    { 5, 2, null, 2, false, "ECHO", "Digital" },
                    { 6, 2, null, 4, false, "VCC", "Power5V" },
                    { 7, 2, null, 4, false, "GND", "Ground" },
                    { 8, 3, null, 1, false, "ENA", "Pwm" },
                    { 9, 3, null, 1, false, "IN1", "Digital" },
                    { 10, 3, null, 1, false, "IN2", "Digital" },
                    { 11, 3, null, 1, false, "ENB", "Pwm" },
                    { 12, 3, null, 1, false, "IN3", "Digital" },
                    { 13, 3, null, 1, false, "IN4", "Digital" },
                    { 14, 5, null, 1, false, "SIGNAL", "Pwm" },
                    { 15, 5, null, 4, false, "VCC", "Power5V" },
                    { 16, 5, null, 4, false, "GND", "Ground" },
                    { 17, 6, null, 1, false, "ANODE", "Digital" },
                    { 18, 6, null, 4, false, "CATHODE", "Ground" },
                    { 19, 7, null, 2, false, "SIGNAL", "Analog" },
                    { 20, 7, null, 4, false, "VCC", "Power5V" },
                    { 21, 7, null, 4, false, "GND", "Ground" },
                    { 22, 8, null, 3, false, "SDA", "I2cSda" },
                    { 23, 8, null, 3, false, "SCL", "I2cScl" },
                    { 24, 8, null, 4, false, "VCC", "Power5V" },
                    { 25, 8, null, 4, false, "GND", "Ground" },
                    { 26, 9, null, 3, false, "SDA", "I2cSda" },
                    { 27, 9, null, 3, false, "SCL", "I2cScl" },
                    { 28, 9, null, 4, false, "VCC", "Power5V" },
                    { 29, 9, null, 4, false, "GND", "Ground" },
                    { 30, 10, null, 2, false, "SIGNAL", "Analog" },
                    { 31, 10, null, 4, false, "VCC", "Power5V" },
                    { 32, 10, null, 4, false, "GND", "Ground" },
                    { 33, 11, null, 3, false, "DATA", "Digital" },
                    { 34, 11, null, 4, false, "VCC", "Power5V" },
                    { 35, 11, null, 4, false, "GND", "Ground" },
                    { 36, 12, null, 1, false, "SIGNAL", "Digital" },
                    { 37, 12, null, 4, false, "GND", "Ground" },
                    { 38, 13, null, 2, false, "SIGNAL", "Digital" },
                    { 39, 13, null, 4, false, "GND", "Ground" },
                    { 40, 14, null, 1, false, "SIGNAL", "Digital" },
                    { 41, 14, null, 4, false, "VCC", "Power5V" },
                    { 42, 14, null, 4, false, "GND", "Ground" },
                    { 43, 15, null, 5, false, "VCC", "Power5V" },
                    { 44, 15, null, 5, false, "GND", "Ground" },
                    { 45, 16, null, 6, false, "PIN1", "Analog" },
                    { 46, 16, null, 6, false, "PIN2", "Analog" },
                    { 47, 17, null, 6, false, "ANODE", "Analog" },
                    { 48, 17, null, 6, false, "CATHODE", "Analog" },
                    { 49, 18, null, 6, false, "PIN1", "Analog" },
                    { 50, 18, null, 6, false, "PIN2", "Analog" },
                    { 51, 19, null, 6, false, "ANODE", "Analog" },
                    { 52, 19, null, 6, false, "CATHODE", "Ground" }
                });

            migrationBuilder.InsertData(
                table: "Pins",
                columns: new[] { "PinId", "BaseErcType", "BoardId", "MaxCurrentMa", "PhysicalPosition", "PinIdentifier", "Voltage" },
                values: new object[,]
                {
                    { 1, 3, 1, 40, 0, "D0", 5.0m },
                    { 2, 3, 1, 40, 1, "D1", 5.0m },
                    { 3, 3, 1, 40, 2, "D2", 5.0m },
                    { 4, 3, 1, 40, 3, "D3", 5.0m },
                    { 5, 3, 1, 40, 4, "D4", 5.0m },
                    { 6, 3, 1, 40, 5, "D5", 5.0m },
                    { 7, 3, 1, 40, 6, "D6", 5.0m },
                    { 8, 3, 1, 40, 7, "D7", 5.0m },
                    { 9, 3, 1, 40, 8, "D8", 5.0m },
                    { 10, 3, 1, 40, 9, "D9", 5.0m },
                    { 11, 3, 1, 40, 10, "D10", 5.0m },
                    { 12, 3, 1, 40, 11, "D11", 5.0m },
                    { 13, 3, 1, 40, 12, "D12", 5.0m },
                    { 14, 3, 1, 40, 13, "D13", 5.0m },
                    { 15, 1, 1, 40, 14, "A0", 5.0m },
                    { 16, 1, 1, 40, 15, "A1", 5.0m },
                    { 17, 1, 1, 40, 16, "A2", 5.0m },
                    { 18, 1, 1, 40, 17, "A3", 5.0m },
                    { 19, 1, 1, 40, 18, "A4", 5.0m },
                    { 20, 1, 1, 40, 19, "A5", 5.0m },
                    { 21, 5, 1, 500, 100, "5V", 5.0m },
                    { 22, 5, 1, 50, 101, "3V3", 3.3m },
                    { 23, 5, 1, 1000, 102, "GND", 0m }
                });

            migrationBuilder.InsertData(
                table: "PowerDistributionRules",
                columns: new[] { "RuleId", "BoardId", "Description", "MaxCurrentMa", "PowerSource", "VoltageV" },
                values: new object[,]
                {
                    { 1, 1, "USB 2.0 power supply", 500, "usb", 5.0m },
                    { 2, 1, "Barrel jack via onboard regulator", 1000, "barrel", 5.0m },
                    { 3, 1, "3.3V rail from onboard LDO", 50, "3v3_pin", 3.3m }
                });

            migrationBuilder.InsertData(
                table: "PinCapabilities",
                columns: new[] { "CapabilityId", "CapabilityType", "PinId" },
                values: new object[,]
                {
                    { 1, "Digital", 1 },
                    { 2, "Digital", 2 },
                    { 3, "Digital", 3 },
                    { 4, "Digital", 4 },
                    { 5, "Digital", 5 },
                    { 6, "Digital", 6 },
                    { 7, "Digital", 7 },
                    { 8, "Digital", 8 },
                    { 9, "Digital", 9 },
                    { 10, "Digital", 10 },
                    { 11, "Digital", 11 },
                    { 12, "Digital", 12 },
                    { 13, "Digital", 13 },
                    { 14, "Digital", 14 },
                    { 15, "Pwm", 4 },
                    { 16, "Pwm", 6 },
                    { 17, "Pwm", 7 },
                    { 18, "Pwm", 10 },
                    { 19, "Pwm", 11 },
                    { 20, "Pwm", 12 },
                    { 21, "Analog", 15 },
                    { 22, "Digital", 15 },
                    { 23, "Analog", 16 },
                    { 24, "Digital", 16 },
                    { 25, "Analog", 17 },
                    { 26, "Digital", 17 },
                    { 27, "Analog", 18 },
                    { 28, "Digital", 18 },
                    { 29, "Analog", 19 },
                    { 30, "Digital", 19 },
                    { 31, "Analog", 20 },
                    { 32, "Digital", 20 },
                    { 33, "I2cSda", 19 },
                    { 34, "I2cScl", 20 },
                    { 35, "SpiMosi", 12 },
                    { 36, "SpiMiso", 13 },
                    { 37, "SpiSck", 14 },
                    { 38, "UartRx", 1 },
                    { 39, "UartTx", 2 },
                    { 40, "Power5V", 21 },
                    { 41, "Power3V3", 22 },
                    { 42, "Ground", 23 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boards_Name",
                table: "Boards",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeTemplates_ComponentId",
                table: "CodeTemplates",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentLibraries_ComponentId_LibraryId",
                table: "ComponentLibraries",
                columns: new[] { "ComponentId", "LibraryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentLibraries_LibraryId",
                table: "ComponentLibraries",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinRequirements_ComponentId_PinName",
                table: "ComponentPinRequirements",
                columns: new[] { "ComponentId", "PinName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_Type",
                table: "Components",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I2cAddresses_ComponentId",
                table: "I2cAddresses",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_Name_Version",
                table: "Libraries",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PinCapabilities_PinId_CapabilityType",
                table: "PinCapabilities",
                columns: new[] { "PinId", "CapabilityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pins_BoardId_PinIdentifier",
                table: "Pins",
                columns: new[] { "BoardId", "PinIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PowerDistributionRules_BoardId",
                table: "PowerDistributionRules",
                column: "BoardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeTemplates");

            migrationBuilder.DropTable(
                name: "ComponentLibraries");

            migrationBuilder.DropTable(
                name: "ComponentPinRequirements");

            migrationBuilder.DropTable(
                name: "I2cAddresses");

            migrationBuilder.DropTable(
                name: "PinCapabilities");

            migrationBuilder.DropTable(
                name: "PowerDistributionRules");

            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "Pins");

            migrationBuilder.DropTable(
                name: "Boards");
        }
    }
}

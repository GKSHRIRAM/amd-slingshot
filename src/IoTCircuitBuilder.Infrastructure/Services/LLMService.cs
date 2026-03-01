using System.Text.Json;
using IoTCircuitBuilder.Application.DTOs;
using IoTCircuitBuilder.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IoTCircuitBuilder.Infrastructure.Services;

public class LLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LLMService> _logger;
    private readonly string _geminiApiKey;
    private readonly string _groqApiKey;
    private readonly string _perplexityApiKey;

    private const string SystemPromptOrchestrator = @"You are the master 'Orchestrator' AI for an IoT circuit generator.
Your job is to analyze the user's project request and determine if it requires a SINGLE microcontroller board or a NETWORK topology of multiple boards communicating together.

DO NOT OUTPUT INDIVIDUAL SENSORS/ACTUATORS (except for the required communication hardware).
Your ONLY output is a JSON blueprint mapping out the network topology.
You MUST use lowercase for all component types and identifiers.

SENSORS: hc_sr04_ultrasonic, ir_sensor, dht11, bme280, ldr_sensor, mpu6050_gyro, tcs3200_color_sensor, hc_sr501_pir
ACTUATORS: sg90_servo, dc_motor, buzzer, bldc_motor
DRIVERS: l298n_motor_driver, esc_30a
DISPLAYS: oled_128x64
INPUT: push_button, potentiometer
OUTPUT: led_red, relay_module
COMMUNICATION: bluetooth_hc05, rf_transmitter_receiver, esp8266_01_wifi, rc522_rfid
POWER: lipo_battery_3s, battery_9v, battery_4xaa

CRITICAL RULE FOR SMARTPHONES/PCs:
If the user mentions controlling the device via a smartphone, mobile app, laptop, or existing external device:
1. The topology MUST BE 'single_board'.
2. DO NOT create a board for the smartphone.
3. Only output boards for the actual Arduino microcontrollers you are physically building.

1. topologies: 'single_board', 'transmitter_receiver', 'mesh_network'
2. communication_hardware: Must be one of: 'bluetooth_hc05', 'rf_transmitter_receiver', 'nrf24l01_spi_module', 'esp8266_wifi'. If single_board, leave null.
3. shared_payload: If networked, write a C++ struct definition representing the data they will send to each other.
4. boards: Array of boards. Give each a clear role. CRITICAL: The downstream BOM Agent cannot read the user's prompt! You MUST EXPLICITLY LIST every sensor and actuator intended for that board in the `role` string (e.g. 'Read DHT11 and control SG90_servo and DC_motor' instead of 'Conveyor belt control'). Failure to list a component in the `role` means it will be MISSING from the final circuit.
5. hardware_class: For EVERY board, you MUST classify its physical movement type. Choose EXACTLY ONE from:
   - 'STATIONARY_STATIC': No movement (e.g. Weather station, Air quality monitor)
   - 'STATIONARY_KINEMATIC': Bolted down, but moves (e.g. Solar tracker, Smart dustbin)
   - 'MOBILE_ROBOTICS': Drives around (e.g. RC Car, Robot)
   - 'UI_CONTROLLER': Human input only (e.g. Remote control, Joystick)
6. board: ALWAYS output 'arduino_uno' for the board field. Other microcontrollers are NOT currently supported by the physical engine.

OUTPUT STRICT JSON ONLY:
{
  ""topology"": ""transmitter_receiver"",
  ""communication_hardware"": ""rf_transmitter_receiver"",
  ""shared_payload"": ""struct SensorData { float temp; float humidity; };"",
  ""boards"": [
    {
      ""board_id"": ""board_0"",
      ""role"": ""Read temperature/humidity from DHT11 and send via RF"",
      ""hardware_class"": ""STATIONARY_STATIC"",
      ""board"": ""arduino_uno"",
      ""logic_type"": ""manual_control""
    },
    {
      ""board_id"": ""board_1"",
      ""role"": ""Receive RF data and display temperature on OLED screen"",
      ""hardware_class"": ""STATIONARY_STATIC"",
      ""board"": ""arduino_uno"",
      ""logic_type"": ""manual_control""
    }
  ]
}";

    private const string SystemPromptBOMAgent = @"You are a strict Bill of Materials (BOM) Agent for an individual Arduino board.
Your job is to parse a specific 'role' description and 'hardware_class' for a generic board and output ONLY the electronic components needed to fulfill that exact role.
CRITICAL: Every 'type' field MUST be in lowercase and match the EXACT names provided below.

═══════════════════════════════════════════════════════════════
CRITICAL: OUTPUT ONLY VALID JSON - NO MARKDOWN, NO EXPLANATIONS
═══════════════════════════════════════════════════════════════

AVAILABLE COMPONENTS (USE EXACT NAMES):
SENSORS: hc_sr04_ultrasonic, ir_sensor, dht11, bme280, ldr_sensor, mpu6050_gyro, tcs3200_color_sensor, hc_sr501_pir
ACTUATORS: sg90_servo, dc_motor, buzzer, bldc_motor
DRIVERS: l298n_motor_driver, esc_30a
DISPLAYS: oled_128x64
INPUT: push_button, potentiometer
OUTPUT: led_red, relay_module
COMMUNICATION: bluetooth_hc05, rf_transmitter, rf_receiver, esp8266_01_wifi, rc522_rfid
PASSIVES: resistor, capacitor_ceramic, diode
POWER: lipo_battery_3s

8. MANDATORY RULES:
   - You MUST include every sensor, actuator, and driver mentioned in the 'ROLE' description. If the role says 'motor', you MUST output both 'dc_motor' and 'l298n_motor_driver'.
   - GHOST COMPONENT RULE: If you omit a component mentioned in the ROLE, the circuit will fail.
   - You will be provided with 'Mandatory Communication Hardware'. You MUST include it in your output JSON array if it is not null.
   - DO NOT include: battery, breadboard, wires, arduino. (The C# engine adds these automatically).
   - ABSOLUTE PHYSICS RULE: You are physically forbidden from suggesting components that violate your HARDWARE CLASS.

OUTPUT STRICT JSON ONLY:
{
  ""components"": [
    { ""type"": ""dht11"", ""quantity"": 1, ""purpose"": ""Measure temperature"" },
    { ""type"": ""bluetooth_hc05"", ""quantity"": 1, ""purpose"": ""Required communication hardware"" }
  ]
}
";

    private const string SystemPromptFirmwareAgent = @"You are a strict Embedded C++ Logic Synthesizer for an EDA compiler. 
Your ONLY job is to write the operational logic for an Arduino board based on a provided hardware header.

CRITICAL COMPILER LAWS:
1. DO NOT write `#include` statements.
2. DO NOT write `#define` macros for pins.
3. DO NOT redefine the shared payload `struct`.
4. The C# Linker has already written the hardware headers. You MUST use the exact macro names provided in the user prompt.
5. Do not use Markdown block formatting (```json). Output raw, parseable JSON.
6. If a struct `SensorData` is provided in the header, use it for transmission.

OUTPUT STRICT JSON ONLY:
{
  ""global_variables"": ""Adafruit_BME280 bme; \n RH_ASK rfDriver(2000, PIN_RF_TRANSMITTER_0_DATA, 11, 10);"",
  ""setup_code"": ""bme.begin(); \n rfDriver.init();"",
  ""loop_code"": ""SensorData payload; \n payload.temperature = bme.readTemperature(); \n rfDriver.send((uint8_t *)&payload, sizeof(payload));""
}";

    public LLMService(HttpClient httpClient, IConfiguration config, ILogger<LLMService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? config["Gemini:ApiKey"] ?? "";
        _groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? config["Groq:ApiKey"] ?? "";
        _perplexityApiKey = Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY") ?? config["Perplexity:ApiKey"] ?? "";
    }

    // --- STAGE 1: ORCHESTRATOR ---
    public async Task<ProjectIntent> ParseIntentAsync(string prompt)
    {
        _logger.LogInformation("Orchestrator parsing started for user request");
        var systemInstruction = SystemPromptOrchestrator;
        var userInstruction = $"Analyze this request and output the network topology and boards array: {prompt}";

        var responseText = await CallLLMFallbackChainAsync(systemInstruction, userInstruction);
        
        try
        {
            var intent = JsonSerializer.Deserialize<ProjectIntent>(CleanJson(responseText), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return intent ?? throw new InvalidOperationException("Failed to deserialize ProjectIntent");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Orchestrator JSON: {Response}", responseText);
            throw;
        }
    }

    // --- STAGE 2: BOM AGENT ---
    private class BomWrapper { public List<ComponentIntent> Components { get; set; } = new(); }

    public async Task<List<ComponentIntent>> ParseBOMAsync(string role, string hardwareClass, string? communicationHardware, List<string>? catalog = null)
    {
        _logger.LogInformation("BOM Agent parsing started for role: {Role}, Class: {Class}", role, hardwareClass);
        
        string catalogInjection = "";
        if (catalog != null && catalog.Any())
        {
            catalogInjection = $"\nCRITICAL RULE: You may ONLY select component IDs from this exact list:\n[{string.Join(", ", catalog)}]\nDo not invent component IDs.";
        }

        var systemInstruction = SystemPromptBOMAgent + catalogInjection;
        var userInstruction = $"Generate the BOM JSON for this specific board:\nROLE: '{role}'\nHARDWARE CLASS: '{hardwareClass}'\nMandatory Communication Hardware: {communicationHardware ?? "none"}\nRULE: You are a {hardwareClass}. You are physically forbidden from suggesting components that violate this class. DO NOT add components that belong to other parts of the system.";

        var responseText = await CallLLMFallbackChainAsync(systemInstruction, userInstruction);
        
        _logger.LogInformation("=== RAW LLM BOM RESPONSE ===");
        _logger.LogInformation(responseText);
        _logger.LogInformation("============================");

        try
        {
            var wrapper = JsonSerializer.Deserialize<BomWrapper>(CleanJson(responseText), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return wrapper?.Components ?? new List<ComponentIntent>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse BOM JSON: {Response}", responseText);
            throw;
        }
    }

    // --- STAGE 3: FIRMWARE AGENT ---
    public async Task<FirmwareAgentResponse> GenerateFirmwareLogicAsync(string header, string role, List<IoTCircuitBuilder.Domain.Entities.Component> components)
    {
        _logger.LogInformation("Firmware Agent parsing started for role: {Role}", role);
        var systemInstruction = SystemPromptFirmwareAgent;
        var componentNames = string.Join(", ", components.Select(c => c.DisplayName ?? c.Type));
        
        var userInstruction = $@"Generate the C++ logic block JSON for this board.
ROLE: '{role}'
HARDWARE INVENTORY: {componentNames}

--- IMMUTABLE HEADER PROVIDED BY C# BACKEND (USE THESE MACROS/STRUCTS) ---
{header}
-------------------------------------------------------------------------";

        var responseText = await CallLLMFallbackChainAsync(systemInstruction, userInstruction, requireJson: true);
        
        try
        {
            var response = JsonSerializer.Deserialize<FirmwareAgentResponse>(CleanJson(responseText), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response ?? new FirmwareAgentResponse();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Firmware JSON: {Response}", responseText);
            throw;
        }
    }

    private string CleanJson(string textContent)
    {
        textContent = textContent.Trim();
        if (textContent.StartsWith("```"))
        {
            textContent = textContent.Replace("```json", "").Replace("```", "").Trim();
        }
        return textContent;
    }

    private async Task<string> CallLLMFallbackChainAsync(string systemInstruction, string userInstruction, bool requireJson = true)
    {
        try
        {
            if (!string.IsNullOrEmpty(_perplexityApiKey))
                return await CallPerplexityAsync(systemInstruction, userInstruction);
        }
        catch (Exception ex) { _logger.LogWarning("Perplexity API failed: {Message}", ex.Message); }

        try
        {
            return await CallGeminiAsync(systemInstruction, userInstruction, requireJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Gemini API failed: {Message}", ex.Message);
            try
            {
                return await CallGroqAsync(systemInstruction, userInstruction, requireJson);
            }
            catch (Exception groqEx)
            {
                throw new InvalidOperationException("All LLM services failed to respond.", groqEx);
            }
        }
    }

    private async Task<string> CallGeminiAsync(string systemInstruction, string userInstruction, bool requireJson)
    {
        object requestBody;
        if (requireJson)
        {
            requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userInstruction } } } },
                generationConfig = new { temperature = 0.1, responseMimeType = "application/json" }
            };
        }
        else
        {
            requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userInstruction } } } },
                generationConfig = new { temperature = 0.1 }
            };
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _geminiApiKey);

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
            throw new Exception(responseJson);

        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;
    }

    private async Task<string> CallPerplexityAsync(string systemInstruction, string userInstruction)
    {
        var requestBody = new
        {
            model = "sonar-pro",
            messages = new[]
            {
                new { role = "system", content = systemInstruction },
                new { role = "user", content = userInstruction }
            },
            temperature = 0.1,
            top_k = 0,
            top_p = 0.9,
            return_citations = false
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.perplexity.ai/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_perplexityApiKey}");

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
            throw new Exception(responseJson);

        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
    }

    private async Task<string> CallGroqAsync(string systemInstruction, string userInstruction, bool requireJson)
    {
        object requestBody;
        if (requireJson)
        {
            requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = systemInstruction },
                    new { role = "user", content = userInstruction }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1
            };
        }
        else
        {
            requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = systemInstruction },
                    new { role = "user", content = userInstruction }
                },
                temperature = 0.1
            };
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_groqApiKey}");

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(responseJson);

        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
    }
}

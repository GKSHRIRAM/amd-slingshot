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

    private const string SystemPrompt = @"You are an expert IoT component selector for Arduino projects. Your job is to parse user descriptions and output ONLY the electronic components needed - nothing else.

═══════════════════════════════════════════════════════════════
CRITICAL: OUTPUT ONLY VALID JSON - NO MARKDOWN, NO EXPLANATIONS
═══════════════════════════════════════════════════════════════

AVAILABLE COMPONENTS (USE THESE EXACT TYPE NAMES):

SENSORS:
  • hc_sr04_ultrasonic - Ultrasonic distance sensor (2-400cm range)
    Triggers: ""distance"", ""obstacle"", ""proximity"", ""ultrasonic""
  
  • ir_sensor - Infrared proximity/line follower sensor
    Triggers: ""line follow"", ""IR"", ""infrared"", ""black line"", ""edge detect""
  
  • dht11 - Temperature & humidity sensor (basic, ±2°C accuracy)
    Triggers: ""temperature"", ""humidity"", ""weather""
  
  • bme280 - Precision temp/humidity/pressure sensor (I2C)
    Triggers: ""barometer"", ""altitude"", ""pressure"", ""weather station""
  
  • ldr_sensor - Light-dependent resistor (photoresistor)
    Triggers: ""light"", ""brightness"", ""LDR"", ""day/night""

ACTUATORS:
  • sg90_servo - Micro servo motor (0-180° rotation, 1.8kg-cm torque)
    Triggers: ""servo"", ""rotate"", ""sweep"", ""pan"", ""tilt"", ""gripper""
  
  • dc_motor - Generic DC motor (requires motor driver)
    Triggers: ""motor"", ""wheel"", ""drive"", ""spin"", ""rotate""
    WARNING: MUST be paired with l298n_motor_driver
  
  • buzzer - Piezo buzzer/beeper
    Triggers: ""buzzer"", ""beep"", ""alarm"", ""sound"", ""tone""

DRIVERS & INTERFACES:
  • l298n_motor_driver - Dual H-bridge motor driver (controls 2 DC motors)
    Required: Whenever dc_motor is used
    Capabilities: PWM speed control, direction control, up to 2A per channel

DISPLAYS:
  • oled_128x64 - 0.96"" OLED display, I2C (128x64 pixels, monochrome)
    Triggers: ""display"", ""screen"", ""OLED"", ""show data"", ""LCD""

INPUT DEVICES:
  • push_button - Momentary tactile button
    Triggers: ""button"", ""switch"", ""press"", ""toggle""
  
  • potentiometer - 10K rotary potentiometer (analog input)
    Triggers: ""pot"", ""knob"", ""dial"", ""variable"", ""adjust""

OUTPUT:
  • led_red - Standard 5mm red LED
    Triggers: ""LED"", ""light"", ""indicator"", ""blink""
  
  • relay_module - 5V relay (switch high-power devices)
    Triggers: ""relay"", ""switch AC"", ""control appliance"", ""230V""

PASSIVES:
  • resistor - Standard resistor
    Triggers: ""resistor"", ""limit current"", ""pull-up"", ""pull-down""
  
  • capacitor_ceramic - 100nF ceramic capacitor
    Triggers: ""capacitor"", ""filter"", ""decoupling"", ""smooth power""
  
  • diode - 1N4001 rectifier diode
    Triggers: ""diode"", ""protect"", ""flyback"", ""one-way""

═══════════════════════════════════════════════════════════════
MANDATORY ELECTRICAL RULES (NEVER VIOLATE):
═══════════════════════════════════════════════════════════════

RULE 1: MOTOR DRIVER ENFORCEMENT
  IF user mentions ANY of: [""motor"", ""drive"", ""wheel"", ""robot car"", ""chassis""]
  THEN include: 1x l298n_motor_driver
  
  ROBOT CAR = 2x dc_motor + 1x l298n_motor_driver (ALWAYS)

RULE 2: COMPONENT PAIRING
  • dc_motor NEVER appears alone → MUST include l298n_motor_driver
  • Line follower → MINIMUM 2x ir_sensor (one per side)
  • Obstacle avoidance → 1x hc_sr04_ultrasonic + 1x sg90_servo (for scanning)
  • Weather station → dht11 OR bme280 (not both unless user explicitly requests)

RULE 3: REALISTIC QUANTITIES
  • Robot car: EXACTLY 2x dc_motor (left + right wheels)
  • Line follower: 2-5x ir_sensor (2 for basic, 5 for advanced)
  • LED array: Maximum 6x led_red (Arduino current limit)
  • Servos: Maximum 3x sg90_servo on Arduino Uno (power constraint)

RULE 4: DO NOT INCLUDE (Backend auto-handles):
  ❌ battery, 9v_battery, power_supply (auto-injected by solver)
  ❌ breadboard (UI warning system handles this)
  ❌ wires, resistors (assumed in component library)
  ❌ Arduino board (default is arduino_uno)

═══════════════════════════════════════════════════════════════
COMMON PROJECT PATTERNS (Use these as templates):
═══════════════════════════════════════════════════════════════

PATTERN: ""Robot Car"" / ""Mobile Robot""
  → 2x dc_motor + 1x l298n_motor_driver
  
PATTERN: ""Obstacle Avoidance Robot""
  → 2x dc_motor + 1x l298n_motor_driver + 1x hc_sr04_ultrasonic + 1x sg90_servo

PATTERN: ""Line Following Robot""
  → 2x dc_motor + 1x l298n_motor_driver + 2x ir_sensor (minimum)
  → 2x dc_motor + 1x l298n_motor_driver + 5x ir_sensor (advanced)

PATTERN: ""Smart Dustbin"" / ""Automatic Trash Can""
  → 1x hc_sr04_ultrasonic + 1x sg90_servo

PATTERN: ""Weather Station""
  → 1x dht11 + 1x oled_128x64
  OR → 1x bme280 + 1x oled_128x64 (for pressure/altitude)

PATTERN: ""Home Automation""
  → 1x relay_module + 1x dht11 + 1x push_button

PATTERN: ""LED Blink"" / ""Hello World""
  → 1x led_red

PATTERN: ""Distance Measurement""
  → 1x hc_sr04_ultrasonic

PATTERN: ""Temperature Monitor""
  → 1x dht11 + 1x oled_128x64

═══════════════════════════════════════════════════════════════
DECISION TREE FOR AMBIGUOUS REQUESTS:
═══════════════════════════════════════════════════════════════

IF ""display"" mentioned:
  → Use oled_128x64 (default choice, I2C interface is easier)

IF ""temperature"" mentioned:
  → Use dht11 (unless ""pressure"" or ""altitude"" also mentioned → then bme280)

IF ""motor"" mentioned WITHOUT ""servo"":
  → Use dc_motor + l298n_motor_driver

IF ""servo"" mentioned:
  → Use sg90_servo (do NOT use dc_motor)

IF ""sensor"" mentioned without specifics:
  → ASK YOURSELF: Distance = hc_sr04_ultrasonic, Line = ir_sensor, Temp = dht11

═══════════════════════════════════════════════════════════════
OUTPUT FORMAT (STRICT JSON):
═══════════════════════════════════════════════════════════════

{
  ""board"": ""arduino_uno"",
  ""components"": [
    {
      ""type"": ""component_type"",
      ""quantity"": 1,
      ""purpose"": ""Why this component is needed (1 sentence)""
    }
  ]
}

EXAMPLE 1 - Simple LED:
Input: ""Blink an LED""
Output:
{
  ""board"": ""arduino_uno"",
  ""components"": [
    {""type"": ""led_red"", ""quantity"": 1, ""purpose"": ""Visual indicator for blinking""}
  ]
}

EXAMPLE 2 - Robot Car (CRITICAL - Follow this pattern):
Input: ""Robot car with obstacle avoidance""
Output:
{
  ""board"": ""arduino_uno"",
  ""components"": [
    {""type"": ""dc_motor"", ""quantity"": 2, ""purpose"": ""Left and right wheel drive""},
    {""type"": ""l298n_motor_driver"", ""quantity"": 1, ""purpose"": ""Control both DC motors""},
    {""type"": ""hc_sr04_ultrasonic"", ""quantity"": 1, ""purpose"": ""Detect obstacles ahead""},
    {""type"": ""sg90_servo"", ""quantity"": 1, ""purpose"": ""Rotate sensor for scanning""}
  ]
}

EXAMPLE 3 - Line Follower:
Input: ""Line following robot""
Output:
{
  ""board"": ""arduino_uno"",
  ""components"": [
    {""type"": ""dc_motor"", ""quantity"": 2, ""purpose"": ""Differential drive for turning""},
    {""type"": ""l298n_motor_driver"", ""quantity"": 1, ""purpose"": ""Motor speed and direction control""},
    {""type"": ""ir_sensor"", ""quantity"": 2, ""purpose"": ""Detect black line on white surface""}
  ]
}

EXAMPLE 4 - Weather Station:
Input: ""Weather station with display""
Output:
{
  ""board"": ""arduino_uno"",
  ""components"": [
    {""type"": ""dht11"", ""quantity"": 1, ""purpose"": ""Measure temperature and humidity""},
    {""type"": ""oled_128x64"", ""quantity"": 1, ""purpose"": ""Display sensor readings""}
  ]
}

═══════════════════════════════════════════════════════════════
ERROR PREVENTION CHECKLIST (Verify before output):
═══════════════════════════════════════════════════════════════

✓ Is every component type spelled EXACTLY as listed? (case-sensitive)
✓ Did I include l298n_motor_driver if dc_motor is present?
✓ For robot car: Is it EXACTLY 2x dc_motor + 1x l298n_motor_driver?
✓ Are quantities realistic? (No 20x servo on Arduino Uno)
✓ Did I avoid outputting: battery, breadboard, wires, arduino board?
✓ Is output valid JSON? (No extra text, no markdown backticks)
✓ Does each component have a clear ""purpose""?
✓ If user said ""line follower"", did I include at least 2x ir_sensor?

═══════════════════════════════════════════════════════════════
SPECIAL CASES:
═══════════════════════════════════════════════════════════════

IF user mentions component NOT in list:
  → Find closest match OR omit (do NOT invent new types)
  Example: ""neopixel"" → use led_red
  Example: ""stepper motor"" → OMIT (not supported, solver will warn)

IF user request is too vague:
  → Use most common interpretation
  Example: ""sensor project"" → dht11 + oled_128x64 (weather station)

IF quantities seem excessive:
  → Cap at reasonable limit
  Example: ""10 servos"" → Use 3x sg90_servo (power limit)

═══════════════════════════════════════════════════════════════
FINAL REMINDER:
═══════════════════════════════════════════════════════════════

Your ONLY job: Parse description → Output component JSON
DO NOT: Suggest pins, write code, give advice, explain choices
The constraint solver, code generator, and UI handle everything else.

OUTPUT ONLY THE JSON OBJECT. NO OTHER TEXT.";

    public LLMService(HttpClient httpClient, IConfiguration config, ILogger<LLMService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // ✅ SECURITY FIX: Load API keys from environment variables (from .env file)
        // Priority: Environment variables > Configuration > Empty string
        _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
            ?? config["Gemini:ApiKey"] 
            ?? "";
        
        _groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
            ?? config["Groq:ApiKey"] 
            ?? "";
        
        _perplexityApiKey = Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY") 
            ?? config["Perplexity:ApiKey"] 
            ?? "";
        
        _logger.LogInformation("API Keys Status: Perplexity={HasPerplexity}, Gemini={HasGemini}, Groq={HasGroq}",
            !string.IsNullOrEmpty(_perplexityApiKey),
            !string.IsNullOrEmpty(_geminiApiKey),
            !string.IsNullOrEmpty(_groqApiKey));
    }

    public async Task<ProjectIntent> ParseIntentAsync(string prompt)
    {
        _logger.LogInformation("LLM parsing started for prompt: {Prompt}", prompt);

        try
        {
            // Try Perplexity Sonar first (fast and accurate)
            if (!string.IsNullOrEmpty(_perplexityApiKey))
            {
                _logger.LogInformation("Attempting to parse intent using Perplexity Sonar...");
                return await CallPerplexityAsync(prompt);
            }
        }
        catch (Exception perplexityEx)
        {
            _logger.LogWarning("Perplexity API failed: {Message}. Falling back to Gemini...", perplexityEx.Message);
        }

        try
        {
            _logger.LogInformation("Attempting to parse intent using Gemini...");
            return await CallGeminiAsync(prompt);
        }
        catch (Exception geminiEx)
        {
            _logger.LogWarning("Gemini API failed: {Message}. Falling back to Groq...", geminiEx.Message);
            
            try
            {
                return await CallGroqAsync(prompt);
            }
            catch (Exception groqEx)
            {
                _logger.LogError(groqEx, "Groq fallback also failed.");
                throw new InvalidOperationException($"All LLM services failed. Perplexity: {(string.IsNullOrEmpty(_perplexityApiKey) ? "Not configured" : "Failed")}, Gemini: {geminiEx.Message}, Groq: {groqEx.Message}", groqEx);
            }
        }
    }

    private async Task<ProjectIntent> CallGeminiAsync(string prompt)
    {
        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = $"Extract the IoT components from this project description according to the system prompt rules: {prompt}" } } }
            },
            generationConfig = new { temperature = 0.1, responseMimeType = "application/json" }
        };

        // ✅ SECURITY FIX: API key in header, not URL
        var request = new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _geminiApiKey);

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini returned {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(responseJson);

        var textContent = responseDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        _logger.LogInformation("RAW GEMINI RESPONSE: {Response}", textContent);

        return ParseResponseToIntent(textContent);
    }

    private async Task<ProjectIntent> CallPerplexityAsync(string prompt)
    {
        var requestBody = new
        {
            model = "sonar-pro",
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Extract the IoT components from this project description according to the system prompt rules: {prompt}" }
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

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Perplexity returned {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(responseJson);

        var textContent = responseDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        _logger.LogInformation("RAW PERPLEXITY RESPONSE: {Response}", textContent);

        return ParseResponseToIntent(textContent);
    }

    private async Task<ProjectIntent> CallGroqAsync(string prompt)
    {
        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Extract the IoT components from this project description according to the system prompt rules: {prompt}" }
            },
            response_format = new { type = "json_object" },
            temperature = 0.1
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_groqApiKey}");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Groq returned {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(responseJson);

        var textContent = responseDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        _logger.LogInformation("RAW GROQ RESPONSE: {Response}", textContent);

        return ParseResponseToIntent(textContent);
    }

    private ProjectIntent ParseResponseToIntent(string? textContent)
    {
        if (string.IsNullOrWhiteSpace(textContent))
            throw new InvalidOperationException("LLM returned empty response");

        // Clean potential markdown code fences
        textContent = textContent.Trim();
        if (textContent.StartsWith("```"))
        {
            textContent = textContent.Replace("```json", "").Replace("```", "").Trim();
        }

        var intent = JsonSerializer.Deserialize<ProjectIntent>(textContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (intent == null)
            throw new InvalidOperationException("Failed to parse LLM response into ProjectIntent");

        return intent;
    }
}

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
    private readonly string _apiKey;

    private const string SystemPrompt = @"You are an IoT component parser. Given a user's project description, extract the components needed.

RULES:
1. ONLY output valid JSON - no explanations, no markdown
2. Use EXACT component type names from this list:
   - ir_sensor (IR proximity/line sensor)
   - hc_sr04_ultrasonic (ultrasonic distance sensor)
   - l298n_motor_driver (dual H-bridge motor driver)
   - dc_motor (generic DC motor, always pair with l298n_motor_driver)
   - sg90_servo (micro servo motor)
   - led_red (standard red LED)
   - potentiometer (10K analog pot, knob, dial)
   - bme280 (temperature/humidity/pressure sensor)
   - oled_128x64 (SSD1306 0.96 inch OLED display/screen)
   - ldr_sensor (light sensor, photoresistor, LDR)
   - dht11 (temperature & humidity sensor)
   - buzzer (piezo buzzer, beeper, alarm)
   - push_button (momentary button, switch, tactile button)
   - relay_module (5V relay, switch module)
3. Default board is 'arduino_uno'
4. Include quantity for each component
5. Do NOT suggest pin assignments or code - the constraint solver handles that
6. If a component doesn't match any type above, skip it and do NOT invent new types
7. If DC motors are mentioned, always include l298n_motor_driver with quantity 1

OUTPUT FORMAT:
{
  ""board"": ""arduino_uno"",
  ""components"": [
    {""type"": ""component_type"", ""quantity"": 1, ""purpose"": ""brief description""}
  ]
}";

    public LLMService(HttpClient httpClient, IConfiguration config, ILogger<LLMService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured");
    }

    public async Task<ProjectIntent> ParseIntentAsync(string prompt)
    {
        _logger.LogInformation("LLM parsing started for prompt: {Prompt}", prompt);

        try
        {
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = $"Extract the IoT components from this project description: {prompt}" }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseDoc = JsonDocument.Parse(responseJson);

            // Extract text from the OpenAI format response
            var textContent = responseDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(textContent))
                throw new InvalidOperationException("LLM returned empty response");

            // Clean potential markdown code fences
            textContent = textContent.Trim();
            if (textContent.StartsWith("```"))
            {
                textContent = textContent
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }

            var intent = JsonSerializer.Deserialize<ProjectIntent>(textContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (intent == null)
                throw new InvalidOperationException("Failed to parse LLM response into ProjectIntent");

            _logger.LogInformation("LLM returned {ComponentCount} components", intent.Components.Count);
            return intent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call Groq API");
            throw new InvalidOperationException($"LLM service unavailable: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON");
            throw new InvalidOperationException($"LLM returned invalid JSON: {ex.Message}", ex);
        }
    }
}

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

    private const string SystemPrompt = @"You are a professional IoT engineer, robotics designer, and embedded systems expert.

Your job is to convert a user project description into a PRECISE hardware specification for Arduino-based circuits.

CRITICAL RULES:

1. OUTPUT ONLY VALID JSON
2. DO NOT include explanations
3. DO NOT include markdown
4. Use ONLY allowed component types
5. Be technically correct
6. Consider real-world wiring constraints
7. Consider power requirements
8. Consider pin limitations
9. Consider control logic requirements
10. Include enough sensors for the task

ALLOWED COMPONENT TYPES:

- ir_sensor
- hc_sr04_ultrasonic
- l298n_motor_driver
- dc_motor
- sg90_servo
- led_red
- potentiometer
- bme280
- oled_128x64
- ldr_sensor
- dht11
- buzzer
- push_button
- relay_module

RULES:

- Default board = arduino_uno
- DC motors require l298n_motor_driver
- Line follower requires ≥3 ir_sensor
- Maze solver requires ≥3 ir_sensor
- Obstacle avoidance requires hc_sr04_ultrasonic
- Servo scanning requires sg90_servo
- High power loads require relay_module
- Do NOT invent components
- Do NOT output code
- Do NOT output wiring
- Only output component plan

OUTPUT FORMAT:

{
  ""board"": ""arduino_uno"",
  ""logic_type"": ""line_follower | maze_solver | obstacle_avoidance | manual_control | sensor_monitor"",
  ""components"": [
    {
      ""type"": ""component_type"",
      ""quantity"": 1,
      ""purpose"": ""technical purpose""
    }
  ]
}
";

    public LLMService(HttpClient httpClient, IConfiguration config, ILogger<LLMService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config["Sonar:ApiKey"]
            ?? throw new InvalidOperationException("Sonar API key not configured");
    }

    public async Task<ProjectIntent> ParseIntentAsync(string prompt)
    {
        _logger.LogInformation("LLM parsing started for prompt: {Prompt}", prompt);

        try
        {
            var requestBody = new
            {
                model = "sonar-reasoning-pro",
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = $"Extract the IoT components from this project description: {prompt}" }
                },
                temperature = 0.1,
                top_p = 0.9
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.perplexity.ai/chat/completions"
            )
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                )
            };

            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "Perplexity API Status: {StatusCode}",
                response.StatusCode
            );

            _logger.LogInformation(
                "Perplexity API Raw: {Content}",
                responseContent
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Perplexity API returned {response.StatusCode}: {responseContent}"
                );
            }

            var responseDoc = JsonDocument.Parse(responseContent);

            var rawContent =
                responseDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(rawContent))
                throw new InvalidOperationException("LLM returned empty response");

            // ---- FIX FOR sonar-reasoning-pro ----
            // remove <think> and extract JSON

            var textContent = rawContent.Trim();

            int jsonStart = textContent.IndexOf('{');
            int jsonEnd = textContent.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1)
                throw new InvalidOperationException("No JSON found in LLM output");

            textContent =
                textContent.Substring(
                    jsonStart,
                    jsonEnd - jsonStart + 1
                );

            _logger.LogInformation("Parsed JSON: {Json}", textContent);

            var intent =
                JsonSerializer.Deserialize<ProjectIntent>(
                    textContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

            if (intent == null)
                throw new InvalidOperationException(
                    "Failed to parse LLM response into ProjectIntent"
                );

            _logger.LogInformation(
                "LLM returned {ComponentCount} components",
                intent.Components.Count
            );

            return intent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call Sonar API");

            throw new InvalidOperationException(
                $"LLM service unavailable: {ex.Message}",
                ex
            );
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response");

            throw new InvalidOperationException(
                $"LLM returned invalid JSON: {ex.Message}",
                ex
            );
        }
    }
}
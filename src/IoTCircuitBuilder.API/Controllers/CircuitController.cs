using IoTCircuitBuilder.Application.DTOs;
using IoTCircuitBuilder.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IoTCircuitBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CircuitController : ControllerBase
{
    private readonly ICircuitGenerationService _circuitService;
    private readonly ILogger<CircuitController> _logger;

    public CircuitController(ICircuitGenerationService circuitService, ILogger<CircuitController> logger)
    {
        _circuitService = circuitService;
        _logger = logger;
    }

    /// <summary>
    /// Generate an IoT circuit from a natural language prompt.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateCircuitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenerateCircuitResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateCircuit([FromBody] GenerateCircuitRequest request)
    {
        // ✅ SECURITY FIX: Input validation with strict limits
        const int maxPromptLength = 5000;
        const int minPromptLength = 5;

        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(GenerateCircuitResponse.Fail("Prompt cannot be empty."));
        }

        if (request.Prompt.Length < minPromptLength)
        {
            return BadRequest(GenerateCircuitResponse.Fail($"Prompt must be at least {minPromptLength} characters."));
        }

        if (request.Prompt.Length > maxPromptLength)
        {
            return BadRequest(GenerateCircuitResponse.Fail($"Prompt cannot exceed {maxPromptLength} characters."));
        }

        // ✅ SECURITY FIX: Sanitize input to prevent injection
        var sanitizedPrompt = System.Net.WebUtility.HtmlEncode(request.Prompt.Trim());

        // ✅ SECURITY FIX: Safe logging without sensitive data
        _logger.LogInformation("Received circuit generation request (length: {PromptLength} chars, timestamp: {Timestamp})",
            sanitizedPrompt.Length,
            DateTime.UtcNow);

        var result = await _circuitService.GenerateCircuitAsync(sanitizedPrompt);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

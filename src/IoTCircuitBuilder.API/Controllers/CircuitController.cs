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
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(GenerateCircuitResponse.Fail("Prompt cannot be empty."));
        }

        _logger.LogInformation("Received circuit generation request: {Prompt}", request.Prompt);

        var result = await _circuitService.GenerateCircuitAsync(request.Prompt);

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

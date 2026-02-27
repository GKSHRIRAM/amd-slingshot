namespace IoTCircuitBuilder.Application.DTOs;

public class GenerateCircuitRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? BoardOverride { get; set; }
}

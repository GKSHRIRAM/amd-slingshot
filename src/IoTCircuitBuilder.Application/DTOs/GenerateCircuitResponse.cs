namespace IoTCircuitBuilder.Application.DTOs;

public class GenerateCircuitResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> GlobalWarnings { get; set; } = new();
    
    public List<CircuitBoardResult> Boards { get; set; } = new();

    public static GenerateCircuitResponse Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}

public class CircuitBoardResult
{
    public string BoardId { get; set; } = "board_0";
    public string Role { get; set; } = string.Empty;
    public Dictionary<string, string> PinMapping { get; set; } = new();
    public string? GeneratedCode { get; set; }
    public List<string> ComponentsUsed { get; set; } = new();
    public bool NeedsBreadboard { get; set; }
    public List<string> Warnings { get; set; } = new();
}

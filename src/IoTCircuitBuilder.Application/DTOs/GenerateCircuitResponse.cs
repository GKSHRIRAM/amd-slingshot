namespace IoTCircuitBuilder.Application.DTOs;

public class GenerateCircuitResponse
{
    public bool Success { get; set; }
    public Dictionary<string, string>? PinMapping { get; set; }
    public string? GeneratedCode { get; set; }
    public List<string>? ComponentsUsed { get; set; }
    public string? Error { get; set; }
    public bool NeedsBreadboard { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static GenerateCircuitResponse Ok(
        Dictionary<string, string> pinMapping,
        string code,
        List<string> components,
        bool needsBreadboard = false,
        List<string>? warnings = null) => new()
    {
        Success = true,
        PinMapping = pinMapping,
        GeneratedCode = code,
        ComponentsUsed = components,
        NeedsBreadboard = needsBreadboard,
        Warnings = warnings ?? new()
    };

    public static GenerateCircuitResponse Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}

namespace IoTCircuitBuilder.Domain.ValueObjects;

public class SolverResult
{
    public bool Success { get; set; }
    public Dictionary<string, string> PinMapping { get; set; } = new();
    public string? Error { get; set; }
    public int BacktrackCount { get; set; }
    public bool NeedsBreadboard { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static SolverResult Succeeded(
        Dictionary<string, string> mapping,
        int backtrackCount,
        bool needsBreadboard = false,
        List<string>? warnings = null)
    {
        return new SolverResult
        {
            Success = true,
            PinMapping = mapping,
            BacktrackCount = backtrackCount,
            NeedsBreadboard = needsBreadboard,
            Warnings = warnings ?? new List<string>()
        };
    }

    public static SolverResult Failed(string error)
    {
        return new SolverResult
        {
            Success = false,
            Error = error
        };
    }
}

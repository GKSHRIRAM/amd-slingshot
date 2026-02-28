namespace IoTCircuitBuilder.Application.DTOs;

public class ProjectIntent
{
    public string Board { get; set; } = "arduino_uno";

    // NEW — needed for correct code generation
    public string Logic_Type { get; set; } = "manual_control";

    public List<ComponentIntent> Components { get; set; } = new();
}

public class ComponentIntent
{
    public string Type { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public string? Purpose { get; set; }
}
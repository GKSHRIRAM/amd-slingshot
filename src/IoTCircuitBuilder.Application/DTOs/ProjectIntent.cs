namespace IoTCircuitBuilder.Application.DTOs;

public class ProjectIntent
{
    public string Board { get; set; } = "arduino_uno";
    public List<ComponentIntent> Components { get; set; } = new();
}

public class ComponentIntent
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? Purpose { get; set; }
}

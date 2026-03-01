namespace IoTCircuitBuilder.Application.DTOs;

public class ProjectIntent
{
    public string Topology { get; set; } = "single_board";
    public string? Communication_Hardware { get; set; }
    public string? Shared_Payload { get; set; }
    public List<BoardIntent> Boards { get; set; } = new();
}

public class BoardIntent
{
    public string Board_Id { get; set; } = "board_0";
    public string Role { get; set; } = string.Empty;
    public string Hardware_Class { get; set; } = "STATIONARY_STATIC";
    public string Board { get; set; } = "arduino_uno";
    public string Logic_Type { get; set; } = "manual_control";
    public List<ComponentIntent> Components { get; set; } = new();
}

public class ComponentIntent
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? Purpose { get; set; }
}
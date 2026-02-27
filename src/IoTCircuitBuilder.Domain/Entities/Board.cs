namespace IoTCircuitBuilder.Domain.Entities;

public class Board
{
    public int BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Voltage { get; set; }
    public int MaxCurrentMa { get; set; }
    public decimal LogicLevelV { get; set; }
    public bool Is5VTolerant { get; set; }
    public int? ProcessorSpeedHz { get; set; }
    public int? FlashMemoryKb { get; set; }
    public int? SramKb { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Pin> Pins { get; set; } = new List<Pin>();
    public ICollection<PowerDistributionRule> PowerDistributionRules { get; set; } = new List<PowerDistributionRule>();
}

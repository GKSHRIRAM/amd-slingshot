namespace IoTCircuitBuilder.Domain.Entities;

public class PowerDistributionRule
{
    public int RuleId { get; set; }
    public int BoardId { get; set; }
    public string PowerSource { get; set; } = string.Empty;   // "usb", "barrel", "vin"
    public int MaxCurrentMa { get; set; }
    public decimal VoltageV { get; set; }
    public string? Description { get; set; }

    // Navigation
    public Board Board { get; set; } = null!;
}

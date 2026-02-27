namespace IoTCircuitBuilder.Domain.Entities;

public class I2cAddress
{
    public int I2cAddressId { get; set; }
    public int ComponentId { get; set; }
    public string DefaultAddress { get; set; } = string.Empty;    // "0x68"
    public string[]? AlternateAddresses { get; set; }
    public bool IsConfigurable { get; set; }

    // Navigation
    public Component Component { get; set; } = null!;
}

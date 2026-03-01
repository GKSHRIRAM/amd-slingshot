namespace IoTCircuitBuilder.Domain.Entities;

public class Component
{
    public int ComponentId { get; set; }
    public string Type { get; set; } = string.Empty;              // "l298n_motor_driver"
    public string? DisplayName { get; set; }                       // "L298N Dual H-Bridge"
    public string? Category { get; set; }                          // "motor_driver"
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public int CurrentDrawMa { get; set; }
    public decimal VoltageMin { get; set; }
    public decimal VoltageMax { get; set; }
    public float LogicVoltage { get; set; } = 5.0f;
    public int RoutingPriority { get; set; } = 3; // 0=Bus, 1=TimerHijack, 2=HighActuator, 3=Generic
    public bool RequiresExternalPower { get; set; }
    public string? InterfaceProtocol { get; set; }                 // "digital", "i2c", "spi", etc.
    public bool RequiresLevelShifter { get; set; }
    public string? DatasheetUrl { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ComponentPinRequirement> PinRequirements { get; set; } = new List<ComponentPinRequirement>();
    public ICollection<ComponentLibrary> ComponentLibraries { get; set; } = new List<ComponentLibrary>();
    public ICollection<I2cAddress> I2cAddresses { get; set; } = new List<I2cAddress>();
    public ICollection<CodeTemplate> CodeTemplates { get; set; } = new List<CodeTemplate>();
}

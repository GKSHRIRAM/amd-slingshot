using IoTCircuitBuilder.Domain.Enums;

namespace IoTCircuitBuilder.Domain.Entities;

public class PinCapability
{
    public int CapabilityId { get; set; }
    public int PinId { get; set; }
    public PinCapabilityType CapabilityType { get; set; }

    // Navigation
    public Pin Pin { get; set; } = null!;
}

using IoTCircuitBuilder.Domain.Enums;

namespace IoTCircuitBuilder.Domain.Entities;

public class ComponentPinRequirement
{
    public int RequirementId { get; set; }
    public int ComponentId { get; set; }
    public string PinName { get; set; } = string.Empty;         // "ENA", "TRIG", "OUT"
    public PinCapabilityType RequiredCapability { get; set; }
    public bool IsOptional { get; set; }
    public string? Description { get; set; }

    // Electrical Identity for ERC Checking
    public ErcPinType ErcType { get; set; } = ErcPinType.Unspecified;

    // Navigation
    public Component Component { get; set; } = null!;
}

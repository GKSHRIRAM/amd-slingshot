using IoTCircuitBuilder.Domain.Enums;

namespace IoTCircuitBuilder.Domain.Entities;

public class Pin
{
    public int PinId { get; set; }
    public int BoardId { get; set; }
    public string PinIdentifier { get; set; } = string.Empty;   // "D3", "A5", "5V", "GND"
    public int? PhysicalPosition { get; set; }
    public decimal Voltage { get; set; }
    public int MaxCurrentMa { get; set; }

    // Electrical Identity for ERC Checking
    public ErcPinType BaseErcType { get; set; } = ErcPinType.Unspecified;

    // Navigation
    public Board Board { get; set; } = null!;
    public ICollection<PinCapability> Capabilities { get; set; } = new List<PinCapability>();
}

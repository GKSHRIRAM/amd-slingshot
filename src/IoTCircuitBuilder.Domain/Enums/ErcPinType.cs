namespace IoTCircuitBuilder.Domain.Enums;

/// <summary>
/// KiCad-style Electrical Rule Checking (ERC) Pin Types
/// Defines the strict physical electrical identity of a pin to prevent invalid connections (e.g., Short Circuits).
/// </summary>
public enum ErcPinType
{
    Unspecified = 0,
    Input = 1,
    Output = 2,
    Bidirectional = 3,
    PowerIn = 4,
    PowerOut = 5,
    Passive = 6
}

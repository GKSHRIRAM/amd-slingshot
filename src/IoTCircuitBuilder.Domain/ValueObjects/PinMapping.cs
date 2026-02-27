namespace IoTCircuitBuilder.Domain.ValueObjects;

public class PinMapping
{
    /// <summary>
    /// Key: "component_type_index.PIN_NAME" (e.g., "ir_sensor_0.OUT")
    /// Value: Board pin identifier (e.g., "D2")
    /// </summary>
    public Dictionary<string, string> Mappings { get; set; } = new();

    public void Add(string componentPin, string boardPin)
    {
        Mappings[componentPin] = boardPin;
    }

    public bool HasMapping(string componentPin) => Mappings.ContainsKey(componentPin);
}

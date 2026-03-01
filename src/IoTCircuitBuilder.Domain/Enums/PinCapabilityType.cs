namespace IoTCircuitBuilder.Domain.Enums;

public enum PinCapabilityType
{
    Digital,
    Analog,
    Pwm,
    I2cSda,
    I2cScl,
    SpiMosi,
    SpiMiso,
    SpiSck,
    UartTx,
    UartRx,
    Power5V,
    Power3V3,
    PowerVin,
    Ground,
    HardwareOnly
}

using IoTCircuitBuilder.Domain.Enums;

namespace IoTCircuitBuilder.Core.Validation;

public enum ErcConnectionStatus
{
    Valid,
    Warning,
    Error
}

/// <summary>
/// A native implementation of the KiCad Electrical Rules Checker (ERC) Collision Matrix.
/// Determines if it is physically safe to connect two pins together.
/// </summary>
public static class ErcCollisionMatrix
{
    public static ErcConnectionStatus CheckConnection(ErcPinType pinA, ErcPinType pinB)
    {
        // Symmetric matrix: Check(A, B) is same as Check(B, A). We normalize order to simplify logic.
        var type1 = (ErcPinType)Math.Min((int)pinA, (int)pinB);
        var type2 = (ErcPinType)Math.Max((int)pinA, (int)pinB);

        // Passive and Unspecified can generally connect to anything safely in a high-level abstraction
        if (type1 == ErcPinType.Unspecified || type2 == ErcPinType.Unspecified) return ErcConnectionStatus.Valid;
        if (type1 == ErcPinType.Passive || type2 == ErcPinType.Passive) return ErcConnectionStatus.Valid;

        // Specific collision rules based on standard EDA matrices:
        switch (type1)
        {
            case ErcPinType.Input:
                // Input can connect to anything (Output, PowerOut, Bidirectional) safely
                return ErcConnectionStatus.Valid;

            case ErcPinType.Output:
                // Output to Output -> SHORT CIRCUIT!
                if (type2 == ErcPinType.Output) return ErcConnectionStatus.Error;
                // Output to Bidirectional -> Warning (Bus conflict potential)
                if (type2 == ErcPinType.Bidirectional) return ErcConnectionStatus.Warning;
                // Output to PowerIn -> Warning (Usually logic can't drive power pins)
                if (type2 == ErcPinType.PowerIn) return ErcConnectionStatus.Warning;
                // Output to PowerOut -> ERROR (Short circuiting logic pin to a voltage rail)
                if (type2 == ErcPinType.PowerOut) return ErcConnectionStatus.Error;
                break;

            case ErcPinType.Bidirectional:
                // Bidirectional to Bidirectional -> Warning
                if (type2 == ErcPinType.Bidirectional) return ErcConnectionStatus.Warning;
                // Bidirectional to PowerIn -> Warning
                if (type2 == ErcPinType.PowerIn) return ErcConnectionStatus.Warning;
                // Bidirectional to PowerOut -> ERROR
                if (type2 == ErcPinType.PowerOut) return ErcConnectionStatus.Error;
                break;

            case ErcPinType.PowerIn:
                // PowerIn to PowerIn -> Valid (e.g. chaining two VCC pins together to the same rail)
                if (type2 == ErcPinType.PowerIn) return ErcConnectionStatus.Valid;
                // PowerIn to PowerOut -> Valid (This is exactly how things get powered)
                if (type2 == ErcPinType.PowerOut) return ErcConnectionStatus.Valid;
                break;

            case ErcPinType.PowerOut:
                // PowerOut to PowerOut -> ERROR (Voltage fight / dead short if different voltages)
                if (type2 == ErcPinType.PowerOut) return ErcConnectionStatus.Error;
                break;
        }

        return ErcConnectionStatus.Valid;
    }
}

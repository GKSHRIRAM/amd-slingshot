using IoTCircuitBuilder.Domain.Entities;

namespace IoTCircuitBuilder.Application.Interfaces;

public interface ICodeGenerator
{
    Task<string> GenerateCodeAsync(Dictionary<string, string> pinMapping, List<Component> components, string logicType, string role, string? sharedPayload = null);
}

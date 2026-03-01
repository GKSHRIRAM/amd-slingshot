using IoTCircuitBuilder.Domain.Entities;

namespace IoTCircuitBuilder.Application.Interfaces;

public interface IComponentRepository
{
    Task<List<Component>> GetComponentsByTypesAsync(List<string> types);
    Task<Component?> GetComponentByTypeAsync(string type);
    Task<bool> CheckI2cConflictsAsync(List<int> componentIds);
    Task<List<string>> GetAllComponentTypesAsync();
}

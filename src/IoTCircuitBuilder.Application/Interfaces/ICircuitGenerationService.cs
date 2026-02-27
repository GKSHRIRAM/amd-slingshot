using IoTCircuitBuilder.Application.DTOs;

namespace IoTCircuitBuilder.Application.Interfaces;

public interface ICircuitGenerationService
{
    Task<GenerateCircuitResponse> GenerateCircuitAsync(string prompt);
}

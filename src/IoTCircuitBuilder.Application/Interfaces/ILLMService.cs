using IoTCircuitBuilder.Application.DTOs;

namespace IoTCircuitBuilder.Application.Interfaces;

public interface ILLMService
{
    Task<ProjectIntent> ParseIntentAsync(string prompt);
}

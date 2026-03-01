using IoTCircuitBuilder.Application.DTOs;

namespace IoTCircuitBuilder.Application.Interfaces;

public interface ILLMService
{
    Task<ProjectIntent> ParseIntentAsync(string prompt);
    Task<List<ComponentIntent>> ParseBOMAsync(string role, string hardwareClass, string? communicationHardware, List<string>? catalog = null);
    Task<FirmwareAgentResponse> GenerateFirmwareLogicAsync(string header, string role, List<IoTCircuitBuilder.Domain.Entities.Component> components);
}

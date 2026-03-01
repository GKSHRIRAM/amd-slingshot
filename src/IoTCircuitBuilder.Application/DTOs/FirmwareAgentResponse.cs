using System.Text.Json.Serialization;

namespace IoTCircuitBuilder.Application.DTOs;

public class FirmwareAgentResponse 
{
    [JsonPropertyName("global_variables")]
    public string GlobalVariables { get; set; } = "";
    
    [JsonPropertyName("setup_code")]
    public string SetupCode { get; set; } = "";
    
    [JsonPropertyName("loop_code")]
    public string LoopCode { get; set; } = "";
}

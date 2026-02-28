using System.ComponentModel.DataAnnotations;

namespace IoTCircuitBuilder.Application.DTOs;

public class GenerateCircuitRequest
{
    [Required]
    [MaxLength(2000)]
    public string Prompt { get; set; } = string.Empty;
    public string? BoardOverride { get; set; }
}

namespace IoTCircuitBuilder.Domain.Entities;

public class CodeTemplate
{
    public int TemplateId { get; set; }
    public int ComponentId { get; set; }
    public string TemplateType { get; set; } = string.Empty;    // "setup", "loop", "declaration"
    public string TemplateContent { get; set; } = string.Empty;
    public string Language { get; set; } = "cpp";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Component Component { get; set; } = null!;
}

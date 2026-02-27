namespace IoTCircuitBuilder.Domain.Entities;

public class ComponentLibrary
{
    public int ComponentLibraryId { get; set; }
    public int ComponentId { get; set; }
    public int LibraryId { get; set; }
    public bool IsRequired { get; set; } = true;

    // Navigation
    public Component Component { get; set; } = null!;
    public Library Library { get; set; } = null!;
}

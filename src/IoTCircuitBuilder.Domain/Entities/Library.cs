namespace IoTCircuitBuilder.Domain.Entities;

public class Library
{
    public int LibraryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? GithubUrl { get; set; }
    public string? InstallCommand { get; set; }
    public bool IsDeprecated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ComponentLibrary> ComponentLibraries { get; set; } = new List<ComponentLibrary>();
}

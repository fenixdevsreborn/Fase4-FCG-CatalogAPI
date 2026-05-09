namespace CatalogAPI.Application.DTOs;

public class GameMetadataDto
{
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static GameMetadataDto Empty() => new();
}

namespace CatalogAPI.Application.DTOs;

public class GameDto
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string Genre { get; set; } = string.Empty;
  public string ImageUrl { get; set; } = string.Empty;
  public string Developer { get; set; } = string.Empty;
  public DateTimeOffset ReleaseDate { get; set; }
  public List<string> Tags { get; set; } = [];
  public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

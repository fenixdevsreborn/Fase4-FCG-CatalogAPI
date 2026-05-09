namespace CatalogAPI.Application.DTOs;

public class UpdateGameDto
{
  public string? Name { get; set; }
  public string? Description { get; set; }
  public decimal? Price { get; set; }
  public string? Genre { get; set; }
  public string? ImageUrl { get; set; }
  public string? Developer { get; set; }
  public DateTimeOffset? ReleaseDate { get; set; }
  public List<string>? Tags { get; set; }
  public Dictionary<string, string>? Metadata { get; set; }
}

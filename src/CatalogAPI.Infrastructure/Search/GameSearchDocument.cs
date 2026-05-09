using CatalogAPI.Application.DTOs;

namespace CatalogAPI.Infrastructure.Search;

public sealed class GameSearchDocument
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

    public static GameSearchDocument FromDto(GameDto game)
    {
        return new GameSearchDocument
        {
            Id = game.Id,
            Name = game.Name,
            Description = game.Description,
            Price = game.Price,
            Genre = game.Genre,
            ImageUrl = game.ImageUrl,
            Developer = game.Developer,
            ReleaseDate = game.ReleaseDate,
            Tags = [.. game.Tags],
            Metadata = new Dictionary<string, string>(game.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    public GameDto ToDto()
    {
        return new GameDto
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Price = Price,
            Genre = Genre,
            ImageUrl = ImageUrl,
            Developer = Developer,
            ReleaseDate = ReleaseDate,
            Tags = [.. Tags],
            Metadata = new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }
}

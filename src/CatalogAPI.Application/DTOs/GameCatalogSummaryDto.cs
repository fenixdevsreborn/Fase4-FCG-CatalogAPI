namespace CatalogAPI.Application.DTOs;

public sealed class GameCatalogSummaryDto
{
    public int TotalGames { get; set; }
    public int TotalGenres { get; set; }
    public decimal AveragePrice { get; set; }
    public List<GenreSummaryDto> Genres { get; set; } = [];
    public GameSearchStatusDto SearchStatus { get; set; } = new();
}

public sealed class GenreSummaryDto
{
    public string Genre { get; set; } = string.Empty;
    public int Count { get; set; }
}

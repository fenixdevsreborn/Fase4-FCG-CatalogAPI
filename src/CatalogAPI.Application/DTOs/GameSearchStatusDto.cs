namespace CatalogAPI.Application.DTOs;

public sealed class GameSearchStatusDto
{
    public bool Enabled { get; set; }
    public bool Available { get; set; }
    public string Provider { get; set; } = "PostgresFallback";
    public string IndexName { get; set; } = string.Empty;
    public int DatabaseCount { get; set; }
    public int? IndexedCount { get; set; }
    public int? MissingCount { get; set; }
    public string? Error { get; set; }
}

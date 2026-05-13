namespace CatalogAPI.Application.DTOs;

public sealed class GameReindexResultDto
{
    public int DatabaseCount { get; set; }
    public int IndexedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
    public List<string> Errors { get; set; } = [];
}

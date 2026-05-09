namespace CatalogAPI.Application.BackgroundJobs;

public sealed record SyncGameReadModelMessage(
    Guid GameId,
    bool TagsProvided,
    bool MetadataProvided,
    IReadOnlyCollection<string>? Tags,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record DeleteGameReadModelMessage(Guid GameId);

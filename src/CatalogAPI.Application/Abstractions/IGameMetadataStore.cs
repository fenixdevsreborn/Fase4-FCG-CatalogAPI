using CatalogAPI.Application.DTOs;

namespace CatalogAPI.Application.Abstractions;

public interface IGameMetadataStore
{
    Task<GameMetadataDto?> GetAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, GameMetadataDto>> GetManyAsync(
        IEnumerable<Guid> gameIds,
        CancellationToken cancellationToken = default);
    Task UpsertAsync(Guid gameId, GameMetadataDto metadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default);
}

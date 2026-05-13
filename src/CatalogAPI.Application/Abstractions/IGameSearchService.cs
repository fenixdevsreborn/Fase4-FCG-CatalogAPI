using CatalogAPI.Application.DTOs;

namespace CatalogAPI.Application.Abstractions;

public interface IGameSearchService
{
    bool IsEnabled { get; }
    string ProviderName { get; }
    Task<PaginatedResultDto<GameDto>> SearchAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<GameSearchStatusDto> GetStatusAsync(int databaseCount, CancellationToken cancellationToken = default);
    Task IndexAsync(GameDto game, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default);
}

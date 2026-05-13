using CatalogAPI.Application.DTOs;

namespace CatalogAPI.Application.Abstractions;

public interface IGameSearchMaintenanceService
{
    Task<GameSearchStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<GameReindexResultDto> ReindexAsync(CancellationToken cancellationToken = default);
    Task<GameCatalogSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

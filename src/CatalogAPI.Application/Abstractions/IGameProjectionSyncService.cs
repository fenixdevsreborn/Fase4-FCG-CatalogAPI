using CatalogAPI.Application.BackgroundJobs;

namespace CatalogAPI.Application.Abstractions;

public interface IGameProjectionSyncService
{
    Task SyncAsync(SyncGameReadModelMessage message, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default);
}

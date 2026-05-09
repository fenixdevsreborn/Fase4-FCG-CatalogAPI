namespace CatalogAPI.Application.Abstractions;

public interface ICatalogCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<string> GetCatalogVersionAsync(CancellationToken cancellationToken = default);
    Task InvalidateCatalogAsync(CancellationToken cancellationToken = default);
}

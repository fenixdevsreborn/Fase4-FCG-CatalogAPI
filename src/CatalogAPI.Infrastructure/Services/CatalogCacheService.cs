using System.Text.Json;
using CatalogAPI.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace CatalogAPI.Infrastructure.Services;

public sealed class CatalogCacheService : ICatalogCacheService
{
    private const string CatalogVersionKey = "catalog:version";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly IDistributedCache _distributedCache;

    public CatalogCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(value, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _distributedCache.SetStringAsync(key, payload, options, cancellationToken);
    }

    public async Task<string> GetCatalogVersionAsync(CancellationToken cancellationToken = default)
    {
        var version = await _distributedCache.GetStringAsync(CatalogVersionKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        version = "v1";
        await _distributedCache.SetStringAsync(
            CatalogVersionKey,
            version,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) },
            cancellationToken);

        return version;
    }

    public async Task InvalidateCatalogAsync(CancellationToken cancellationToken = default)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        await _distributedCache.SetStringAsync(
            CatalogVersionKey,
            newVersion,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) },
            cancellationToken);
    }
}

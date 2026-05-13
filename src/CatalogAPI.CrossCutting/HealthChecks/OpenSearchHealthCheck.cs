using CatalogAPI.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CatalogAPI.CrossCutting.HealthChecks;

public sealed class OpenSearchHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OpenSearchHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var maintenance = scope.ServiceProvider.GetRequiredService<IGameSearchMaintenanceService>();
        var status = await maintenance.GetStatusAsync(cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["enabled"] = status.Enabled,
            ["available"] = status.Available,
            ["provider"] = status.Provider,
            ["indexName"] = status.IndexName,
            ["databaseCount"] = status.DatabaseCount
        };

        if (status.IndexedCount.HasValue)
        {
            data["indexedCount"] = status.IndexedCount.Value;
        }

        if (!status.Enabled)
        {
            return HealthCheckResult.Healthy("OpenSearch is disabled.", data);
        }

        return status.Available
            ? HealthCheckResult.Healthy("OpenSearch is available.", data)
            : HealthCheckResult.Unhealthy(status.Error ?? "OpenSearch is unavailable.", data: data);
    }
}

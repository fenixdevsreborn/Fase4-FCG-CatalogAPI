using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace CatalogAPI.CrossCutting.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RedisHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration["CatalogCache:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Healthy("Redis is disabled; memory cache is being used.");
        }

        try
        {
            await using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var endpoint = connection.GetEndPoints().FirstOrDefault();
            if (endpoint == null)
            {
                return HealthCheckResult.Unhealthy("Redis returned no endpoints.");
            }

            var database = connection.GetDatabase();
            await database.PingAsync();

            return HealthCheckResult.Healthy("Redis is available.", new Dictionary<string, object>
            {
                ["endpoint"] = endpoint.ToString() ?? string.Empty
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", ex);
        }
    }
}

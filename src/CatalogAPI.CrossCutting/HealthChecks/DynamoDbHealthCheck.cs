using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CatalogAPI.CrossCutting.HealthChecks;

public sealed class DynamoDbHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public DynamoDbHealthCheck(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue<bool>("DynamoDb:Enabled"))
        {
            return HealthCheckResult.Healthy("DynamoDB is disabled.");
        }

        var tableName = _configuration["DynamoDb:TableName"];
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return HealthCheckResult.Unhealthy("DynamoDb:TableName is not configured.");
        }

        var dynamoDb = _serviceProvider.GetService<IAmazonDynamoDB>();
        if (dynamoDb == null)
        {
            return HealthCheckResult.Unhealthy("DynamoDB client is not registered.");
        }

        try
        {
            var response = await dynamoDb.DescribeTableAsync(tableName, cancellationToken);
            var data = new Dictionary<string, object>
            {
                ["tableName"] = tableName,
                ["status"] = response.Table.TableStatus.Value
            };

            return response.Table.TableStatus == TableStatus.ACTIVE
                ? HealthCheckResult.Healthy("DynamoDB table is active.", data)
                : HealthCheckResult.Degraded("DynamoDB table is not active.", data: data);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("DynamoDB health check failed.", ex);
        }
    }
}

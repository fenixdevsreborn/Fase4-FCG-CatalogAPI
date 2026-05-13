using Amazon;
using Amazon.DynamoDBv2;
using CatalogAPI.Application.Abstractions;
using CatalogAPI.Domain.Interfaces;
using CatalogAPI.Infrastructure.Configuration;
using CatalogAPI.Infrastructure.BackgroundServices;
using CatalogAPI.Infrastructure.Data;
using CatalogAPI.Infrastructure.Data.Seeders;
using CatalogAPI.Infrastructure.Repositories;
using CatalogAPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogAPI.CrossCutting.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.Configure<CatalogCacheOptions>(configuration.GetSection("CatalogCache"));
        services.Configure<DynamoDbOptions>(configuration.GetSection("DynamoDb"));
        services.Configure<OpenSearchOptions>(configuration.GetSection("OpenSearch"));

        var cacheOptions = configuration.GetSection("CatalogCache").Get<CatalogCacheOptions>() ?? new CatalogCacheOptions();
        if (!string.IsNullOrWhiteSpace(cacheOptions.ConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.ConnectionString;
                options.InstanceName = cacheOptions.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        var dynamoDbOptions = configuration.GetSection("DynamoDb").Get<DynamoDbOptions>() ?? new DynamoDbOptions();
        if (dynamoDbOptions.Enabled)
        {
            services.AddSingleton<IAmazonDynamoDB>(_ =>
            {
                var config = new AmazonDynamoDBConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(dynamoDbOptions.Region)
                };

                if (!string.IsNullOrWhiteSpace(dynamoDbOptions.ServiceUrl))
                {
                    config.ServiceURL = dynamoDbOptions.ServiceUrl;
                }

                return new AmazonDynamoDBClient(config);
            });
        }

        // Add DbContext
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CatalogDatabase")));

        // Add Repositories
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IUserGameRepository, UserGameRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Add Manual Outbox implementation
        services.AddScoped<CatalogAPI.Domain.Interfaces.IOutbox, ManualOutbox>();

        // Add Background Service to process Outbox messages
        services.AddHostedService<OutboxProcessorService>();

        services.AddSingleton<ICatalogCacheService, CatalogCacheService>();
        services.AddSingleton<IGameMetadataStore, GameMetadataStore>();
        services.AddSingleton<IGameSearchService, OpenSearchGameSearchService>();
        services.AddScoped<IGameProjectionSyncService, GameProjectionSyncService>();
        services.AddScoped<IGameSearchMaintenanceService, GameSearchMaintenanceService>();

        // Register Seeders
        services.AddScoped<ISeeder, GameSeeder>();
        services.AddScoped<DatabaseSeederService>();

        return services;
    }
}

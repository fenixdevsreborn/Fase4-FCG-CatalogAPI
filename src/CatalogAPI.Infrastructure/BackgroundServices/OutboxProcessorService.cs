using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.BackgroundJobs;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using System.Reflection;
using System.Text.Json;

namespace CatalogAPI.Infrastructure.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private const int BatchSize = 100;
    private const int ProcessingIntervalSeconds = 5;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Service stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var unprocessedMessages = await outboxRepository.GetUnprocessedMessagesAsync(BatchSize, cancellationToken);

        if (unprocessedMessages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages", unprocessedMessages.Count);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                // Deserialize the message payload
                var eventType = GetTypeFromAssemblies(message.EventType);
                if (eventType == null)
                {
                    _logger.LogWarning("Event type {EventType} not found, marking as processed", message.EventType);
                    await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
                    continue;
                }

                // Use the same JsonSerializerOptions as ManualOutbox for consistency
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // PascalCase (padrão)
                };
                var eventObject = JsonSerializer.Deserialize(message.Payload, eventType, jsonOptions);
                if (eventObject == null)
                {
                    _logger.LogWarning("Failed to deserialize message {MessageId}", message.Id);
                    await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
                    continue;
                }

                if (eventObject is SyncGameReadModelMessage syncMessage)
                {
                    var projectionSyncService = scope.ServiceProvider.GetRequiredService<IGameProjectionSyncService>();
                    await projectionSyncService.SyncAsync(syncMessage, cancellationToken);
                }
                else if (eventObject is DeleteGameReadModelMessage deleteMessage)
                {
                    var projectionSyncService = scope.ServiceProvider.GetRequiredService<IGameProjectionSyncService>();
                    await projectionSyncService.DeleteAsync(deleteMessage.GameId, cancellationToken);
                }
                else
                {
                    // Publish to MassTransit/RabbitMQ
                    var publishMethod = typeof(IPublishEndpoint).GetMethods()
                        .FirstOrDefault(m => m.Name == "Publish" &&
                                            m.IsGenericMethod &&
                                            m.GetParameters().Length == 2 &&
                                            m.GetParameters()[1].ParameterType == typeof(CancellationToken));

                    if (publishMethod != null)
                    {
                        var genericMethod = publishMethod.MakeGenericMethod(eventType);
                        await (Task)genericMethod.Invoke(publishEndpoint, new[] { eventObject, cancellationToken })!;

                        if (eventObject is OrderPlacedEvent orderPlacedEvent)
                        {
                            _logger.LogInformation(
                                "[CatalogAPI] Published OrderPlacedEvent. OrderId: {OrderId}, GameId: {GameId}, UserId: {UserId}",
                                orderPlacedEvent.OrderId, orderPlacedEvent.GameId, orderPlacedEvent.UserId);
                        }
                        else
                        {
                            _logger.LogDebug("Published message of type {EventType}", message.EventType);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not publish message of type {EventType}", message.EventType);
                    }
                }

                // Mark as processed
                await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogDebug("Processed outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                // Don't mark as processed on error - will retry on next iteration
            }
        }

        // Save all changes
        await outboxRepository.SaveChangesAsync(cancellationToken);
    }

    private static Type? GetTypeFromAssemblies(string typeName)
    {
        // First try Type.GetType (works for AssemblyQualifiedName or types in mscorlib)
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // Search in all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        // If not found, try to match by full name (without assembly info)
        var typeNameWithoutAssembly = typeName.Split(',')[0].Trim();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetTypes().FirstOrDefault(t => t.FullName == typeNameWithoutAssembly);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}

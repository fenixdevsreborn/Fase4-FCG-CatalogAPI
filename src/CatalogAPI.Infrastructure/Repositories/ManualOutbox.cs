using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using System.Text.Json;

namespace CatalogAPI.Infrastructure.Repositories;

public class ManualOutbox : IOutbox
{
  private readonly IOutboxMessageRepository _outboxRepository;
  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    PropertyNamingPolicy = null, // Usa PascalCase (padrão)
    WriteIndented = false
  };

  public ManualOutbox(IOutboxMessageRepository outboxRepository)
  {
    _outboxRepository = outboxRepository;
  }

  public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
  {
    var outboxMessage = new OutboxMessage
    {
      Id = Guid.NewGuid(),
      EventType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
      Payload = JsonSerializer.Serialize(message, JsonOptions),
      CreatedAt = DateTimeOffset.UtcNow,
      CorrelationId = ExtractCorrelationId(message) ?? Guid.NewGuid().ToString()
    };

    await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
  }

  public async Task SaveChangesAndFlushMessagesAsync(CancellationToken cancellationToken = default)
  {
    // Save changes to database (this commits the transaction)
    await _outboxRepository.SaveChangesAsync(cancellationToken);
    // Messages will be processed by the background service
  }

  private static string? ExtractCorrelationId<T>(T message) where T : class
  {
    // Try to extract CorrelationId from the message if it has that property
    var correlationIdProperty = typeof(T).GetProperty("CorrelationId");
    if (correlationIdProperty != null)
    {
      var value = correlationIdProperty.GetValue(message);
      return value?.ToString();
    }
    return null;
  }
}

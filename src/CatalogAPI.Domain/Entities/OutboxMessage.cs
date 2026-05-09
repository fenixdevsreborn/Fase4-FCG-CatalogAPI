namespace CatalogAPI.Domain.Entities;

public class OutboxMessage
{
  public Guid Id { get; set; }
  public string EventType { get; set; } = string.Empty;
  public string Payload { get; set; } = string.Empty;
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? ProcessedAt { get; set; }
  public string CorrelationId { get; set; } = string.Empty;
}

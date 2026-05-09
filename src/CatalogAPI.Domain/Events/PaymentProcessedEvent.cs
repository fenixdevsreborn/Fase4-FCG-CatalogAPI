namespace CatalogAPI.Domain.Events;

public class PaymentProcessedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty; // "Approved" ou "Rejected"
    public DateTime OccurredAt { get; set; }

    public PaymentProcessedEvent()
    {
    }

    public PaymentProcessedEvent(Guid orderId, Guid userId, Guid gameId, decimal price, string status)
    {
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Price = price;
        Status = status;
        OccurredAt = DateTime.UtcNow;
    }
}

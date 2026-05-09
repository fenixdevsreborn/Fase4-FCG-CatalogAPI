namespace Shared.Contracts.Events;

/// <summary>
/// Evento publicado pelo PaymentsAPI após processar um pagamento.
/// Consumido pelo CatalogAPI e NotificationsAPI.
/// </summary>
public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    
    public Guid UserId { get; init; }
    
    public Guid GameId { get; init; }
    
    /// <summary>
    /// Status do pagamento: "Approved" ou "Rejected"
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public PaymentProcessedEvent() { }

    public PaymentProcessedEvent(Guid orderId, Guid userId, Guid gameId, string status)
    {
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Status = status;
    }
}

namespace CatalogAPI.Domain.Events;

public class OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid GameId { get; set; }
    
    public decimal Price { get; set; }

    public OrderPlacedEvent()
    {
    }

    public OrderPlacedEvent(Guid orderId, Guid userId, Guid gameId, decimal price)
    {
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Price = price;
    }
}

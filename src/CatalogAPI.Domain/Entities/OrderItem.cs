namespace CatalogAPI.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public OrderItemStatus Status { get; set; }
    
    public Order Order { get; set; } = null!;
    public Game Game { get; set; } = null!;
}

public enum OrderItemStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

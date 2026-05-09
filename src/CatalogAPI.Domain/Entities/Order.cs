namespace CatalogAPI.Domain.Entities;

public class Order
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public OrderStatus Status { get; set; }
  public decimal TotalPrice { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }

  public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Completed = 3
}

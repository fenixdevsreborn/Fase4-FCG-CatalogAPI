namespace CatalogAPI.Application.DTOs;

public record CreateOrderRequest
{
    public Guid GameId { get; init; }
}

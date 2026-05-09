using Mediator;

namespace CatalogAPI.Application.UseCases.Orders.CreateOrder;

public record CreateOrderCommand(
    Guid UserId,
    Guid GameId,
    Guid CorrelationId
) : ICommand<Guid>;

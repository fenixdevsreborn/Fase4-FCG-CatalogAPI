using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace CatalogAPI.Application.UseCases.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
  private readonly IGameRepository _gameRepository;
  private readonly IUserGameRepository _userGameRepository;
  private readonly IOrderRepository _orderRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IOutbox _outbox;
  private readonly ILogger<CreateOrderCommandHandler> _logger;

  public CreateOrderCommandHandler(
      IGameRepository gameRepository,
      IUserGameRepository userGameRepository,
      IOrderRepository orderRepository,
      IUnitOfWork unitOfWork,
      IOutbox outbox,
      ILogger<CreateOrderCommandHandler> logger)
  {
    _gameRepository = gameRepository;
    _userGameRepository = userGameRepository;
    _orderRepository = orderRepository;
    _unitOfWork = unitOfWork;
    _outbox = outbox;
    _logger = logger;
  }

  public async ValueTask<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Creating order for GameId: {GameId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
        command.GameId, command.UserId, command.CorrelationId);

    // Validate game exists
    var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);
    if (game == null)
    {
      throw new GameNotFoundException(command.GameId);
    }

    // Check if user already owns the game
    var existingPurchase = await _userGameRepository.GetByUserAndGameAsync(command.UserId, command.GameId, cancellationToken);
    if (existingPurchase != null)
    {
      throw new GameAlreadyPurchasedException(command.UserId, command.GameId);
    }

    try
    {
      // Create Order
      var orderId = Guid.NewGuid();
      var order = new Order
      {
        Id = orderId,
        UserId = command.UserId,
        Status = OrderStatus.Pending,
        TotalPrice = game.Price,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
      };

      // Create OrderItem
      var orderItem = new OrderItem
      {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        GameId = command.GameId,
        Price = game.Price,
        Status = OrderItemStatus.Pending
      };

      order.OrderItems.Add(orderItem);

      // Save Order
      await _orderRepository.AddAsync(order, cancellationToken);

      // Create OrderPlacedEvent
      var orderPlacedEvent = new OrderPlacedEvent(orderId, command.UserId, command.GameId, game.Price);

      // Publish event via Outbox
      await _outbox.PublishAsync(orderPlacedEvent, cancellationToken);

      // Save changes and flush messages
      await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

      _logger.LogInformation("Order created successfully. OrderId: {OrderId}, GameId: {GameId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
          orderId, command.GameId, command.UserId, command.CorrelationId);

      return orderId;
    }
    catch (Exception ex) when (ex is not GameAlreadyPurchasedException && ex is not GameNotFoundException)
    {
      _logger.LogError(ex, "Error creating order for GameId: {GameId}, UserId: {UserId}",
          command.GameId, command.UserId);
      throw;
    }
  }
}

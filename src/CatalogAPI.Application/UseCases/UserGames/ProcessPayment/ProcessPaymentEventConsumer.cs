using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace CatalogAPI.Application.UseCases.UserGames.ProcessPayment;

public sealed class ProcessPaymentEventConsumer : IConsumer<PaymentProcessedEvent>
{
  private readonly IGameRepository _gameRepository;
  private readonly IUserGameRepository _userGameRepository;
  private readonly IOrderRepository _orderRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<ProcessPaymentEventConsumer> _logger;

  public ProcessPaymentEventConsumer(
      IGameRepository gameRepository,
      IUserGameRepository userGameRepository,
      IOrderRepository orderRepository,
      IUnitOfWork unitOfWork,
      ILogger<ProcessPaymentEventConsumer> logger)
  {
    _gameRepository = gameRepository;
    _userGameRepository = userGameRepository;
    _orderRepository = orderRepository;
    _unitOfWork = unitOfWork;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
  {
    var message = context.Message;
    var correlationId = context.CorrelationId ?? context.ConversationId;

    _logger.LogInformation(
      "[CatalogAPI] PaymentProcessedEvent recebido. OrderId: {OrderId}, Status: {Status}, UserId: {UserId}, GameId: {GameId}, CorrelationId: {CorrelationId}",
      message.OrderId, message.Status, message.UserId, message.GameId, correlationId);

    // Get Order with Items
    var order = await _orderRepository.GetByIdWithItemsAsync(message.OrderId, context.CancellationToken);
    if (order == null)
    {
      _logger.LogError("Order not found when processing payment. OrderId: {OrderId}, GameId: {GameId}",
          message.OrderId, message.GameId);
      return;
    }

    // Find the OrderItem for this GameId
    var orderItem = order.OrderItems.FirstOrDefault(oi => oi.GameId == message.GameId);
    if (orderItem == null)
    {
      _logger.LogError("OrderItem not found for OrderId: {OrderId}, GameId: {GameId}",
          message.OrderId, message.GameId);
      return;
    }

    // Update OrderItem status
    if (message.Status == "Approved")
    {
      orderItem.Status = OrderItemStatus.Approved;
      _logger.LogInformation("[CatalogAPI] OrderItem aprovado. OrderId: {OrderId}, GameId: {GameId}, CorrelationId: {CorrelationId}",
        message.OrderId, message.GameId, correlationId);
    }
    else
    {
      orderItem.Status = OrderItemStatus.Rejected;
      _logger.LogInformation("[CatalogAPI] OrderItem rejeitado. OrderId: {OrderId}, GameId: {GameId}, CorrelationId: {CorrelationId}",
        message.OrderId, message.GameId, correlationId);
    }

    // Update Order status based on all items
    var allApproved = order.OrderItems.All(oi => oi.Status == OrderItemStatus.Approved);
    var anyRejected = order.OrderItems.Any(oi => oi.Status == OrderItemStatus.Rejected);

    if (allApproved)
    {
      order.Status = OrderStatus.Completed;
    }
    else if (anyRejected && order.OrderItems.All(oi => oi.Status != OrderItemStatus.Pending))
    {
      order.Status = OrderStatus.Rejected;
    }
    else
    {
      order.Status = OrderStatus.Approved; // Some items approved
    }

    order.UpdatedAt = DateTimeOffset.UtcNow;

    // Only add game to library if payment was approved
    if (message.Status == "Approved")
    {
      // Validate game exists
      var game = await _gameRepository.GetByIdAsync(message.GameId, context.CancellationToken);
      if (game == null)
      {
        _logger.LogError("Game not found when processing payment. GameId: {GameId}, OrderId: {OrderId}",
            message.GameId, message.OrderId);
        await _orderRepository.UpdateAsync(order, context.CancellationToken);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        return;
      }

      // Check if user already owns the game (idempotency check)
      var existingPurchase = await _userGameRepository.GetByUserAndGameAsync(message.UserId, message.GameId, context.CancellationToken);
      if (existingPurchase != null)
      {
        _logger.LogInformation("User already owns this game. Skipping. UserId: {UserId}, GameId: {GameId}, OrderId: {OrderId}",
            message.UserId, message.GameId, message.OrderId);
        await _orderRepository.UpdateAsync(order, context.CancellationToken);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        return;
      }

      try
      {
        // Add game to user's library
        var userGame = new UserGame
        {
          UserId = message.UserId,
          GameId = message.GameId,
          PurchaseDate = DateTimeOffset.UtcNow
        };

        await _userGameRepository.AddAsync(userGame, context.CancellationToken);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error adding game to library after payment approval. UserId: {UserId}, GameId: {GameId}, OrderId: {OrderId}",
            message.UserId, message.GameId, message.OrderId);
        throw;
      }
    }
    else
    {
      _logger.LogWarning("Payment was not approved (Status: {Status}). Game will not be added to library. OrderId: {OrderId}",
          message.Status, message.OrderId);
    }

    // Update Order and save changes
    await _orderRepository.UpdateAsync(order, context.CancellationToken);
    await _unitOfWork.SaveChangesAsync(context.CancellationToken);

    _logger.LogInformation(
      "[CatalogAPI] Pagamento processado com sucesso. OrderId: {OrderId}, OrderStatus: {OrderStatus}, GameId: {GameId}, UserId: {UserId}, PaymentStatus: {PaymentStatus}, CorrelationId: {CorrelationId}",
      message.OrderId, order.Status, message.GameId, message.UserId, message.Status, correlationId);
  }
}

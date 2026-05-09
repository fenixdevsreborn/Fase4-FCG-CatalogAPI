using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace CatalogAPI.Application.UseCases.UserGames.PurchaseGame;

public sealed class PurchaseGameCommandHandler : ICommandHandler<PurchaseGameCommand, Guid>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserGameRepository _userGameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutbox _outbox;
    private readonly ILogger<PurchaseGameCommandHandler> _logger;

    public PurchaseGameCommandHandler(
        IGameRepository gameRepository,
        IUserGameRepository userGameRepository,
        IUnitOfWork unitOfWork,
        IOutbox outbox,
        ILogger<PurchaseGameCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _userGameRepository = userGameRepository;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _logger = logger;
    }

    public async ValueTask<Guid> Handle(PurchaseGameCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing purchase for GameId: {GameId}, UserId: {UserId}, CorrelationId: {CorrelationId}", 
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
            // Create OrderPlacedEvent - do NOT add to library yet
            // The game will be added to library only after PaymentProcessedEvent with status "Approved"
            var orderId = Guid.NewGuid();
            var orderPlacedEvent = new OrderPlacedEvent(orderId, command.UserId, command.GameId, game.Price);

            // Publish event via Wolverine Outbox - message will be persisted and sent after transaction commits
            await _outbox.PublishAsync(orderPlacedEvent, cancellationToken);

            // Save changes and flush messages - commits transaction and sends messages via Outbox
            // Wolverine Outbox automatically manages the transaction
            await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
            
            _logger.LogInformation("OrderPlacedEvent published successfully. GameId: {GameId}, UserId: {UserId}, CorrelationId: {CorrelationId}. Waiting for payment processing...", 
                command.GameId, command.UserId, command.CorrelationId);

            // Return gameId to indicate order was placed (not yet purchased)
            return command.GameId;
        }
        catch (Exception ex) when (ex is not GameAlreadyPurchasedException && ex is not GameNotFoundException)
        {
            _logger.LogError(ex, "Error processing purchase for GameId: {GameId}, UserId: {UserId}", 
                command.GameId, command.UserId);
            throw;
        }
    }
}

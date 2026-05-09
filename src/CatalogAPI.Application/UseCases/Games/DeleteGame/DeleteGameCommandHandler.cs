using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.BackgroundJobs;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Application.UseCases.Games.DeleteGame;

public sealed class DeleteGameCommandHandler : ICommandHandler<DeleteGameCommand, bool>
{
    private readonly IGameRepository _gameRepository;
    private readonly IOutbox _outbox;
    private readonly ICatalogCacheService _catalogCacheService;
    private readonly ILogger<DeleteGameCommandHandler> _logger;

    public DeleteGameCommandHandler(
        IGameRepository gameRepository,
        IOutbox outbox,
        ICatalogCacheService catalogCacheService,
        ILogger<DeleteGameCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _outbox = outbox;
        _catalogCacheService = catalogCacheService;
        _logger = logger;
    }

    public async ValueTask<bool> Handle(DeleteGameCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting game: {GameId}", command.GameId);

        // Check if game exists
        var exists = await _gameRepository.ExistsAsync(command.GameId, cancellationToken);
        if (!exists)
        {
            throw new GameNotFoundException(command.GameId);
        }

        // Delete game (cascade delete will handle UserGames)
        await _gameRepository.DeleteAsync(command.GameId, cancellationToken);

        await _outbox.PublishAsync(new DeleteGameReadModelMessage(command.GameId), cancellationToken);
        await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await TryInvalidateCacheAsync(cancellationToken);

        _logger.LogInformation("Game deleted successfully. GameId: {GameId}", command.GameId);

        return true;
    }

    private async Task TryInvalidateCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _catalogCacheService.InvalidateCatalogAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog cache invalidation failed after game deletion");
        }
    }
}

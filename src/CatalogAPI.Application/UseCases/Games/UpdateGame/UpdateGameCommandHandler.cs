using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.BackgroundJobs;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Application.UseCases.Games.UpdateGame;

public sealed class UpdateGameCommandHandler : ICommandHandler<UpdateGameCommand, bool>
{
    private readonly IGameRepository _gameRepository;
    private readonly IOutbox _outbox;
    private readonly ICatalogCacheService _catalogCacheService;
    private readonly ILogger<UpdateGameCommandHandler> _logger;

    public UpdateGameCommandHandler(
        IGameRepository gameRepository,
        IOutbox outbox,
        ICatalogCacheService catalogCacheService,
        ILogger<UpdateGameCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _outbox = outbox;
        _catalogCacheService = catalogCacheService;
        _logger = logger;
    }

    public async ValueTask<bool> Handle(UpdateGameCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating game: {GameId}", command.GameId);

        // Get existing game with tracking for update
        var game = await _gameRepository.GetByIdForUpdateAsync(command.GameId, cancellationToken);
        if (game == null)
        {
            throw new GameNotFoundException(command.GameId);
        }

        // Update properties if provided
        if (command.Game.Name != null)
            game.Name = command.Game.Name;

        if (command.Game.Description != null)
            game.Description = command.Game.Description;

        if (command.Game.Price.HasValue)
            game.Price = command.Game.Price.Value;

        if (command.Game.Genre != null)
            game.Genre = command.Game.Genre;

        if (command.Game.ImageUrl != null)
            game.ImageUrl = command.Game.ImageUrl;

        if (command.Game.Developer != null)
            game.Developer = command.Game.Developer;

        if (command.Game.ReleaseDate.HasValue)
            game.ReleaseDate = command.Game.ReleaseDate.Value;

        // Update game in repository
        await _gameRepository.UpdateAsync(game, cancellationToken);

        var syncMessage = new SyncGameReadModelMessage(
            game.Id,
            TagsProvided: command.Game.Tags != null,
            MetadataProvided: command.Game.Metadata != null,
            Tags: command.Game.Tags,
            Metadata: command.Game.Metadata);

        await _outbox.PublishAsync(syncMessage, cancellationToken);
        await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await TryInvalidateCacheAsync(cancellationToken);

        _logger.LogInformation("Game updated successfully. GameId: {GameId}", command.GameId);

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
            _logger.LogWarning(ex, "Catalog cache invalidation failed after game update");
        }
    }
}

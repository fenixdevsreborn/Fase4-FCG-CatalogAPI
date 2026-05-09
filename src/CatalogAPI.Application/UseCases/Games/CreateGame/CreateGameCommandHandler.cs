using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.BackgroundJobs;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using Mapster;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Application.UseCases.Games.CreateGame;

public sealed class CreateGameCommandHandler : ICommandHandler<CreateGameCommand, Guid>
{
    private readonly IGameRepository _gameRepository;
    private readonly IOutbox _outbox;
    private readonly ICatalogCacheService _catalogCacheService;
    private readonly ILogger<CreateGameCommandHandler> _logger;

    public CreateGameCommandHandler(
        IGameRepository gameRepository,
        IOutbox outbox,
        ICatalogCacheService catalogCacheService,
        ILogger<CreateGameCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _outbox = outbox;
        _catalogCacheService = catalogCacheService;
        _logger = logger;
    }

    public async ValueTask<Guid> Handle(CreateGameCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new game: {GameName}", command.Game.Name);

        // Check if game with same name already exists
        var existsByName = await _gameRepository.ExistsByNameAsync(command.Game.Name, cancellationToken);
        if (existsByName)
        {
            throw new GameAlreadyExistsException(command.Game.Name);
        }

        // Map DTO to entity
        var game = command.Game.Adapt<Game>();
        game.Id = Guid.NewGuid();

        // Add game to repository
        await _gameRepository.AddAsync(game, cancellationToken);

        var syncMessage = new SyncGameReadModelMessage(
            game.Id,
            TagsProvided: true,
            MetadataProvided: true,
            Tags: command.Game.Tags ?? [],
            Metadata: command.Game.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        await _outbox.PublishAsync(syncMessage, cancellationToken);
        await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await TryInvalidateCacheAsync(cancellationToken);

        _logger.LogInformation("Game created successfully. GameId: {GameId}, Name: {GameName}", 
            game.Id, game.Name);

        return game.Id;
    }

    private async Task TryInvalidateCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _catalogCacheService.InvalidateCatalogAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog cache invalidation failed after game creation");
        }
    }
}

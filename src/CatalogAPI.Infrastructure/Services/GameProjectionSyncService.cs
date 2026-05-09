using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.BackgroundJobs;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Infrastructure.Services;

public sealed class GameProjectionSyncService : IGameProjectionSyncService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameMetadataStore _gameMetadataStore;
    private readonly IGameSearchService _gameSearchService;
    private readonly ILogger<GameProjectionSyncService> _logger;

    public GameProjectionSyncService(
        IGameRepository gameRepository,
        IGameMetadataStore gameMetadataStore,
        IGameSearchService gameSearchService,
        ILogger<GameProjectionSyncService> logger)
    {
        _gameRepository = gameRepository;
        _gameMetadataStore = gameMetadataStore;
        _gameSearchService = gameSearchService;
        _logger = logger;
    }

    public async Task SyncAsync(SyncGameReadModelMessage message, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(message.GameId, cancellationToken);
        if (game == null)
        {
            _logger.LogWarning("Skipping game read model sync because game {GameId} was not found", message.GameId);
            return;
        }

        var metadata = await _gameMetadataStore.GetAsync(message.GameId, cancellationToken) ?? GameMetadataDto.Empty();

        if (message.TagsProvided)
        {
            metadata.Tags = message.Tags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        if (message.MetadataProvided)
        {
            metadata.Metadata = message.Metadata?
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => pair.Value?.Trim() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        await _gameMetadataStore.UpsertAsync(message.GameId, metadata, cancellationToken);

        var gameDto = game.Adapt<GameDto>();
        gameDto.Tags = metadata.Tags;
        gameDto.Metadata = metadata.Metadata;

        await _gameSearchService.IndexAsync(gameDto, cancellationToken);

        _logger.LogInformation("Synchronized game projections for {GameId}", message.GameId);
    }

    public async Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        await _gameMetadataStore.DeleteAsync(gameId, cancellationToken);
        await _gameSearchService.DeleteAsync(gameId, cancellationToken);
        _logger.LogInformation("Deleted game projections for {GameId}", gameId);
    }
}

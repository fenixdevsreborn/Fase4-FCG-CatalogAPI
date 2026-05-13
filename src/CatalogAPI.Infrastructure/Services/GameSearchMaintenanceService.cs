using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Infrastructure.Services;

public sealed class GameSearchMaintenanceService : IGameSearchMaintenanceService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameMetadataStore _gameMetadataStore;
    private readonly IGameSearchService _gameSearchService;
    private readonly ILogger<GameSearchMaintenanceService> _logger;

    public GameSearchMaintenanceService(
        IGameRepository gameRepository,
        IGameMetadataStore gameMetadataStore,
        IGameSearchService gameSearchService,
        ILogger<GameSearchMaintenanceService> logger)
    {
        _gameRepository = gameRepository;
        _gameMetadataStore = gameMetadataStore;
        _gameSearchService = gameSearchService;
        _logger = logger;
    }

    public async Task<GameSearchStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var databaseCount = await _gameRepository.GetTotalCountAsync(cancellationToken);
        return await _gameSearchService.GetStatusAsync(databaseCount, cancellationToken);
    }

    public async Task<GameReindexResultDto> ReindexAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var games = await _gameRepository.GetAllForIndexingAsync(cancellationToken);
        var result = new GameReindexResultDto
        {
            DatabaseCount = games.Count,
            StartedAt = startedAt
        };

        if (!_gameSearchService.IsEnabled)
        {
            result.FailedCount = games.Count;
            result.Errors.Add("OpenSearch is disabled.");
            result.FinishedAt = DateTimeOffset.UtcNow;
            return result;
        }

        var metadataMap = await _gameMetadataStore.GetManyAsync(games.Select(game => game.Id), cancellationToken);

        foreach (var game in games)
        {
            try
            {
                var dto = ToDto(game, metadataMap.TryGetValue(game.Id, out var metadata) ? metadata : null);
                await _gameSearchService.IndexAsync(dto, cancellationToken);
                result.IndexedCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.FailedCount++;
                result.Errors.Add($"{game.Id}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to reindex game {GameId}", game.Id);
            }
        }

        result.FinishedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Game search reindex finished. DatabaseCount: {DatabaseCount}, IndexedCount: {IndexedCount}, FailedCount: {FailedCount}",
            result.DatabaseCount,
            result.IndexedCount,
            result.FailedCount);

        return result;
    }

    public async Task<GameCatalogSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetAllForIndexingAsync(cancellationToken);
        var status = await _gameSearchService.GetStatusAsync(games.Count, cancellationToken);

        return new GameCatalogSummaryDto
        {
            TotalGames = games.Count,
            TotalGenres = games
                .Select(game => game.Genre)
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            AveragePrice = games.Count == 0 ? 0 : Math.Round(games.Average(game => game.Price), 2),
            Genres = games
                .GroupBy(game => string.IsNullOrWhiteSpace(game.Genre) ? "Uncategorized" : game.Genre.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new GenreSummaryDto { Genre = group.Key, Count = group.Count() })
                .OrderByDescending(genre => genre.Count)
                .ThenBy(genre => genre.Genre)
                .ToList(),
            SearchStatus = status
        };
    }

    private static GameDto ToDto(Game game, GameMetadataDto? metadata)
    {
        return new GameDto
        {
            Id = game.Id,
            Name = game.Name,
            Description = game.Description,
            Price = game.Price,
            Genre = game.Genre,
            ImageUrl = game.ImageUrl,
            Developer = game.Developer,
            ReleaseDate = game.ReleaseDate,
            Tags = metadata?.Tags ?? [],
            Metadata = metadata?.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}

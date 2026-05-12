using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Interfaces;
using Mapster;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Application.UseCases.Games.SearchGames;

public sealed class SearchGamesQueryHandler : IQueryHandler<SearchGamesQuery, PaginatedResultDto<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSearchService _gameSearchService;
    private readonly IGameMetadataStore _gameMetadataStore;
    private readonly ICatalogCacheService _catalogCacheService;
    private readonly ILogger<SearchGamesQueryHandler> _logger;

    public SearchGamesQueryHandler(
        IGameRepository gameRepository,
        IGameSearchService gameSearchService,
        IGameMetadataStore gameMetadataStore,
        ICatalogCacheService catalogCacheService,
        ILogger<SearchGamesQueryHandler> logger)
    {
        _gameRepository = gameRepository;
        _gameSearchService = gameSearchService;
        _gameMetadataStore = gameMetadataStore;
        _catalogCacheService = catalogCacheService;
        _logger = logger;
    }

    public async ValueTask<PaginatedResultDto<GameDto>> Handle(SearchGamesQuery query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Query.Trim();
        var catalogVersion = await _catalogCacheService.GetCatalogVersionAsync(cancellationToken);
        var cacheKey =
            $"catalog:search:{catalogVersion}:q:{normalizedQuery.ToLowerInvariant()}:page:{query.PageNumber}:size:{query.PageSize}";

        var cachedResult = await _catalogCacheService.GetAsync<PaginatedResultDto<GameDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        PaginatedResultDto<GameDto> result;

        if (_gameSearchService.IsEnabled)
        {
            try
            {
                result = await _gameSearchService.SearchAsync(
                    normalizedQuery,
                    query.PageNumber,
                    query.PageSize,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "OpenSearch search failed for query {Query}. Falling back to repository search.",
                    normalizedQuery);

                result = await SearchRepositoryAsync(
                    normalizedQuery,
                    query.PageNumber,
                    query.PageSize,
                    cancellationToken);
            }
        }
        else
        {
            result = await SearchRepositoryAsync(
                normalizedQuery,
                query.PageNumber,
                query.PageSize,
                cancellationToken);
        }

        await _catalogCacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
    }

    private async Task<PaginatedResultDto<GameDto>> SearchRepositoryAsync(
        string normalizedQuery,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var games = await _gameRepository.SearchAsync(
            normalizedQuery,
            pageNumber,
            pageSize,
            cancellationToken);
        var totalCount = await _gameRepository.SearchTotalCountAsync(normalizedQuery, cancellationToken);

        var gameDtos = games.Adapt<List<GameDto>>();
        await EnrichWithMetadataAsync(gameDtos, cancellationToken);

        return new PaginatedResultDto<GameDto>
        {
            Items = gameDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task EnrichWithMetadataAsync(IReadOnlyCollection<GameDto> games, CancellationToken cancellationToken)
    {
        if (games.Count == 0)
        {
            return;
        }

        var metadataMap = await _gameMetadataStore.GetManyAsync(games.Select(game => game.Id), cancellationToken);

        foreach (var game in games)
        {
            if (metadataMap.TryGetValue(game.Id, out var metadata))
            {
                game.Tags = metadata.Tags;
                game.Metadata = metadata.Metadata;
            }
        }
    }
}

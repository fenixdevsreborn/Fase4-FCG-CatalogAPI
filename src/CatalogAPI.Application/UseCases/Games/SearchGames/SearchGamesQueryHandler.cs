using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Interfaces;
using Mapster;
using Mediator;

namespace CatalogAPI.Application.UseCases.Games.SearchGames;

public sealed class SearchGamesQueryHandler : IQueryHandler<SearchGamesQuery, PaginatedResultDto<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSearchService _gameSearchService;
    private readonly IGameMetadataStore _gameMetadataStore;
    private readonly ICatalogCacheService _catalogCacheService;

    public SearchGamesQueryHandler(
        IGameRepository gameRepository,
        IGameSearchService gameSearchService,
        IGameMetadataStore gameMetadataStore,
        ICatalogCacheService catalogCacheService)
    {
        _gameRepository = gameRepository;
        _gameSearchService = gameSearchService;
        _gameMetadataStore = gameMetadataStore;
        _catalogCacheService = catalogCacheService;
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
            result = await _gameSearchService.SearchAsync(
                normalizedQuery,
                query.PageNumber,
                query.PageSize,
                cancellationToken);
        }
        else
        {
            var games = await _gameRepository.SearchAsync(
                normalizedQuery,
                query.PageNumber,
                query.PageSize,
                cancellationToken);
            var totalCount = await _gameRepository.SearchTotalCountAsync(normalizedQuery, cancellationToken);

            var gameDtos = games.Adapt<List<GameDto>>();
            await EnrichWithMetadataAsync(gameDtos, cancellationToken);

            result = new PaginatedResultDto<GameDto>
            {
                Items = gameDtos,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        await _catalogCacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
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

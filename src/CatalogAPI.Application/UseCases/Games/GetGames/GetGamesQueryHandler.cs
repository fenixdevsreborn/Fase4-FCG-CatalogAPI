using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Interfaces;
using Mapster;
using Mediator;

namespace CatalogAPI.Application.UseCases.Games.GetGames;

public sealed class GetGamesQueryHandler : IQueryHandler<GetGamesQuery, PaginatedResultDto<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameMetadataStore _gameMetadataStore;
    private readonly ICatalogCacheService _catalogCacheService;

    public GetGamesQueryHandler(
        IGameRepository gameRepository,
        IGameMetadataStore gameMetadataStore,
        ICatalogCacheService catalogCacheService)
    {
        _gameRepository = gameRepository;
        _gameMetadataStore = gameMetadataStore;
        _catalogCacheService = catalogCacheService;
    }

    public async ValueTask<PaginatedResultDto<GameDto>> Handle(GetGamesQuery query, CancellationToken cancellationToken)
    {
        var catalogVersion = await _catalogCacheService.GetCatalogVersionAsync(cancellationToken);
        var cacheKey = $"catalog:games:{catalogVersion}:page:{query.PageNumber}:size:{query.PageSize}";
        var cachedResult = await _catalogCacheService.GetAsync<PaginatedResultDto<GameDto>>(cacheKey, cancellationToken);

        if (cachedResult != null)
        {
            return cachedResult;
        }

        var games = await _gameRepository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
        var totalCount = await _gameRepository.GetTotalCountAsync(cancellationToken);

        var gameDtos = games.Adapt<List<GameDto>>();
        var metadataMap = await _gameMetadataStore.GetManyAsync(gameDtos.Select(game => game.Id), cancellationToken);

        foreach (var gameDto in gameDtos)
        {
            if (metadataMap.TryGetValue(gameDto.Id, out var metadata))
            {
                gameDto.Tags = metadata.Tags;
                gameDto.Metadata = metadata.Metadata;
            }
        }

        var result = new PaginatedResultDto<GameDto>
        {
            Items = gameDtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        await _catalogCacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
    }
}

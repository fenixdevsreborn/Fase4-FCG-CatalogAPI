using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.Games.GetGames;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace CatalogAPI.Tests;

public class GetGamesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCachedResult_WhenCacheHasValue()
    {
        var gameRepository = new Mock<IGameRepository>(MockBehavior.Strict);
        var metadataStore = new Mock<IGameMetadataStore>(MockBehavior.Strict);
        var cacheService = new Mock<ICatalogCacheService>();

        var cachedResult = new PaginatedResultDto<GameDto>
        {
            Items = [new GameDto { Id = Guid.NewGuid(), Name = "Cached Game" }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };

        cacheService.Setup(cache => cache.GetCatalogVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync("v1");
        cacheService.Setup(cache => cache.GetAsync<PaginatedResultDto<GameDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var handler = new GetGamesQueryHandler(gameRepository.Object, metadataStore.Object, cacheService.Object);

        var result = await handler.Handle(new GetGamesQuery(1, 20), CancellationToken.None);

        result.Should().BeSameAs(cachedResult);
    }

    [Fact]
    public async Task Handle_ShouldEnrichGamesWithMetadataAndCacheResult()
    {
        var gameRepository = new Mock<IGameRepository>();
        var metadataStore = new Mock<IGameMetadataStore>();
        var cacheService = new Mock<ICatalogCacheService>();
        var gameId = Guid.NewGuid();

        cacheService.Setup(cache => cache.GetCatalogVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync("v2");
        cacheService.Setup(cache => cache.GetAsync<PaginatedResultDto<GameDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResultDto<GameDto>?)null);

        gameRepository.Setup(repository => repository.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Game
                {
                    Id = gameId,
                    Name = "Forza Horizon",
                    Description = "Racing game",
                    Price = 99.90m,
                    Genre = "Racing",
                    Developer = "Playground Games",
                    ImageUrl = string.Empty,
                    ReleaseDate = DateTimeOffset.UtcNow
                }
            ]);
        gameRepository.Setup(repository => repository.GetTotalCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        metadataStore.Setup(store => store.GetManyAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, GameMetadataDto>
            {
                [gameId] = new()
                {
                    Tags = ["featured"],
                    Metadata = new Dictionary<string, string> { ["audience"] = "teen" }
                }
            });

        var handler = new GetGamesQueryHandler(gameRepository.Object, metadataStore.Object, cacheService.Object);

        var result = await handler.Handle(new GetGamesQuery(1, 20), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Tags.Should().Contain("featured");
        result.Items[0].Metadata.Should().ContainKey("audience");
        cacheService.Verify(
            cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<PaginatedResultDto<GameDto>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.Games.SearchGames;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace CatalogAPI.Tests;

public class SearchGamesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFallbackToRepository_WhenOpenSearchIsDisabled()
    {
        var gameRepository = new Mock<IGameRepository>();
        var searchService = new Mock<IGameSearchService>();
        var metadataStore = new Mock<IGameMetadataStore>();
        var cacheService = new Mock<ICatalogCacheService>();
        var gameId = Guid.NewGuid();

        searchService.SetupGet(service => service.IsEnabled).Returns(false);
        cacheService.Setup(cache => cache.GetCatalogVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync("v3");
        cacheService.Setup(cache => cache.GetAsync<PaginatedResultDto<GameDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResultDto<GameDto>?)null);

        gameRepository
            .Setup(repository => repository.SearchAsync("hal", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Game
                {
                    Id = gameId,
                    Name = "Halo Infinite",
                    Description = "Shooter",
                    Price = 150m,
                    Genre = "FPS",
                    Developer = "343",
                    ImageUrl = string.Empty,
                    ReleaseDate = DateTimeOffset.UtcNow
                }
            ]);
        gameRepository
            .Setup(repository => repository.SearchTotalCountAsync("hal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        metadataStore.Setup(store => store.GetManyAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, GameMetadataDto>
            {
                [gameId] = new()
                {
                    Tags = ["space"],
                    Metadata = new Dictionary<string, string> { ["franchise"] = "halo" }
                }
            });

        var handler = new SearchGamesQueryHandler(
            gameRepository.Object,
            searchService.Object,
            metadataStore.Object,
            cacheService.Object);

        var result = await handler.Handle(new SearchGamesQuery("hal", 1, 20), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Halo Infinite");
        result.Items[0].Tags.Should().Contain("space");
        cacheService.Verify(
            cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<PaginatedResultDto<GameDto>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

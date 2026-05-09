using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.BackgroundJobs;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.Games.CreateGame;
using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CatalogAPI.Tests;

public class CreateGameCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPublishReadModelSyncAndInvalidateCache()
    {
        var gameRepository = new Mock<IGameRepository>();
        var outbox = new Mock<IOutbox>();
        var cacheService = new Mock<ICatalogCacheService>();
        var logger = new Mock<ILogger<CreateGameCommandHandler>>();

        gameRepository
            .Setup(repository => repository.ExistsByNameAsync("Halo Infinite", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        gameRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game game, CancellationToken _) => game);

        var handler = new CreateGameCommandHandler(
            gameRepository.Object,
            outbox.Object,
            cacheService.Object,
            logger.Object);

        var command = new CreateGameCommand(new CreateGameDto
        {
            Name = "Halo Infinite",
            Description = "Sci-fi shooter",
            Price = 199.90m,
            Genre = "FPS",
            Developer = "343 Industries",
            ReleaseDate = DateTimeOffset.UtcNow,
            Tags = ["co-op", "space"],
            Metadata = new Dictionary<string, string> { ["edition"] = "standard" }
        });

        var gameId = await handler.Handle(command, CancellationToken.None);

        gameId.Should().NotBeEmpty();

        outbox.Verify(
            publisher => publisher.PublishAsync(
                It.Is<SyncGameReadModelMessage>(message =>
                    message.GameId == gameId &&
                    message.TagsProvided &&
                    message.MetadataProvided &&
                    message.Tags != null &&
                    message.Tags.Contains("co-op") &&
                    message.Metadata != null &&
                    message.Metadata["edition"] == "standard"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        outbox.Verify(publisher => publisher.SaveChangesAndFlushMessagesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(cache => cache.InvalidateCatalogAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

using Mediator;

namespace CatalogAPI.Application.UseCases.Games.DeleteGame;

public sealed record DeleteGameCommand(Guid GameId) : ICommand<bool>;

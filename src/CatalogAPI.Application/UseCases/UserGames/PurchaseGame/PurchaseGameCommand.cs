using Mediator;

namespace CatalogAPI.Application.UseCases.UserGames.PurchaseGame;

public sealed record PurchaseGameCommand(Guid GameId, Guid CorrelationId, Guid UserId) : ICommand<Guid>;

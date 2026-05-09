using CatalogAPI.Application.DTOs;
using Mediator;

namespace CatalogAPI.Application.UseCases.Games.CreateGame;

public sealed record CreateGameCommand(CreateGameDto Game) : ICommand<Guid>;

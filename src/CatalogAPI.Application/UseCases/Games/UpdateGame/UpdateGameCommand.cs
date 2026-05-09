using CatalogAPI.Application.DTOs;
using Mediator;

namespace CatalogAPI.Application.UseCases.Games.UpdateGame;

public sealed record UpdateGameCommand(Guid GameId, UpdateGameDto Game) : ICommand<bool>;

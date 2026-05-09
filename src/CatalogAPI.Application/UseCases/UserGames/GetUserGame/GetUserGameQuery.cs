using CatalogAPI.Application.DTOs;
using Mediator;

namespace CatalogAPI.Application.UseCases.UserGames.GetUserGame;

public sealed record GetUserGameQuery(Guid UserId, Guid GameId) : IQuery<UserGameDto?>;

using CatalogAPI.Application.DTOs;
using Mediator;

namespace CatalogAPI.Application.UseCases.UserGames.GetUserGames;

public sealed record GetUserGamesQuery(Guid UserId) : IQuery<PaginatedResultDto<UserGameDto>>;

using CatalogAPI.Application.DTOs;
using Mediator;

namespace CatalogAPI.Application.UseCases.Games.GetGames;

public sealed record GetGamesQuery(int PageNumber = 1, int PageSize = 20) : IQuery<PaginatedResultDto<GameDto>>;

using CatalogAPI.Application.DTOs;
using Mediator;

namespace CatalogAPI.Application.UseCases.Games.SearchGames;

public sealed record SearchGamesQuery(string Query, int PageNumber = 1, int PageSize = 20)
    : IQuery<PaginatedResultDto<GameDto>>;

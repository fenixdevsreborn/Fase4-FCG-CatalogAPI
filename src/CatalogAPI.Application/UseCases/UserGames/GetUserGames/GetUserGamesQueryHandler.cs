using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Interfaces;
using Mediator;

namespace CatalogAPI.Application.UseCases.UserGames.GetUserGames;

public sealed class GetUserGamesQueryHandler : IQueryHandler<GetUserGamesQuery, PaginatedResultDto<UserGameDto>>
{
    private readonly IUserGameRepository _userGameRepository;

    public GetUserGamesQueryHandler(IUserGameRepository userGameRepository)
    {
        _userGameRepository = userGameRepository;
    }

    public async ValueTask<PaginatedResultDto<UserGameDto>> Handle(GetUserGamesQuery request, CancellationToken cancellationToken)
    {
        var userGames = await _userGameRepository.GetByUserWithGameAsync(request.UserId, cancellationToken);
        var totalCount = await _userGameRepository.GetCountByUserAsync(request.UserId, cancellationToken);

        var userGameDtos = userGames.Select(ug => new UserGameDto
        {
            UserId = ug.UserId,
            GameId = ug.GameId,
            PurchaseDate = ug.PurchaseDate,
            Game = new GameDto
            {
                Id = ug.Game.Id,
                Name = ug.Game.Name,
                Description = ug.Game.Description,
                Price = ug.Game.Price,
                Genre = ug.Game.Genre,
                ImageUrl = ug.Game.ImageUrl,
                Developer = ug.Game.Developer,
                ReleaseDate = ug.Game.ReleaseDate
            }
        }).ToList();

        return new PaginatedResultDto<UserGameDto>
        {
            Items = userGameDtos,
            TotalCount = totalCount,
            PageNumber = 1,
            PageSize = userGameDtos.Count
        };
    }
}

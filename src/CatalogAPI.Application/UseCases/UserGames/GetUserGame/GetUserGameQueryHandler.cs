using CatalogAPI.Application.DTOs;
using CatalogAPI.Domain.Interfaces;
using Mediator;

namespace CatalogAPI.Application.UseCases.UserGames.GetUserGame;

public sealed class GetUserGameQueryHandler : IQueryHandler<GetUserGameQuery, UserGameDto?>
{
    private readonly IUserGameRepository _userGameRepository;

    public GetUserGameQueryHandler(IUserGameRepository userGameRepository)
    {
        _userGameRepository = userGameRepository;
    }

    public async ValueTask<UserGameDto?> Handle(GetUserGameQuery request, CancellationToken cancellationToken)
    {
        var userGame = await _userGameRepository.GetByUserAndGameWithGameAsync(request.UserId, request.GameId, cancellationToken);

        if (userGame == null)
        {
            return null;
        }

        return new UserGameDto
        {
            UserId = userGame.UserId,
            GameId = userGame.GameId,
            PurchaseDate = userGame.PurchaseDate,
            Game = new GameDto
            {
                Id = userGame.Game.Id,
                Name = userGame.Game.Name,
                Description = userGame.Game.Description,
                Price = userGame.Game.Price,
                Genre = userGame.Game.Genre,
                ImageUrl = userGame.Game.ImageUrl,
                Developer = userGame.Game.Developer,
                ReleaseDate = userGame.Game.ReleaseDate
            }
        };
    }
}

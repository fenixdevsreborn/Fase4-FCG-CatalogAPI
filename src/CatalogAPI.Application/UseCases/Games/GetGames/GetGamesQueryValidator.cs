using FluentValidation;

namespace CatalogAPI.Application.UseCases.Games.GetGames;

public class GetGamesQueryValidator : AbstractValidator<GetGamesQuery>
{
    public GetGamesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}

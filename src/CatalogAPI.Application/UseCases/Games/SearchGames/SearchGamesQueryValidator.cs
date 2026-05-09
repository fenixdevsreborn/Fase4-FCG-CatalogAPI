using FluentValidation;

namespace CatalogAPI.Application.UseCases.Games.SearchGames;

public sealed class SearchGamesQueryValidator : AbstractValidator<SearchGamesQuery>
{
    public SearchGamesQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(120);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}

using FluentValidation;

namespace CatalogAPI.Application.UseCases.Games.DeleteGame;

public class DeleteGameCommandValidator : AbstractValidator<DeleteGameCommand>
{
    public DeleteGameCommandValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("GameId is required.");
    }
}

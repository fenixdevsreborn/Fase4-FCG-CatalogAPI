using FluentValidation;

namespace CatalogAPI.Application.UseCases.Games.UpdateGame;

public class UpdateGameCommandValidator : AbstractValidator<UpdateGameCommand>
{
  public UpdateGameCommandValidator()
  {
    RuleFor(x => x.GameId)
        .NotEmpty()
        .WithMessage("GameId is required.");

    RuleFor(x => x.Game)
        .NotNull()
        .WithMessage("Game data is required.");

    When(x => x.Game != null, () =>
    {
      RuleFor(x => x.Game.Name)
              .MaximumLength(200)
              .When(x => !string.IsNullOrEmpty(x.Game.Name))
              .WithMessage("Name must not exceed 200 characters.");

      RuleFor(x => x.Game.Description)
              .MaximumLength(1000)
              .When(x => !string.IsNullOrEmpty(x.Game.Description))
              .WithMessage("Description must not exceed 1000 characters.");

      RuleFor(x => x.Game.Price)
              .GreaterThan(0)
              .When(x => x.Game.Price.HasValue)
              .WithMessage("Price must be greater than 0.");

      RuleFor(x => x.Game.Genre)
              .MaximumLength(100)
              .When(x => !string.IsNullOrEmpty(x.Game.Genre))
              .WithMessage("Genre must not exceed 100 characters.");

      RuleFor(x => x.Game.ImageUrl)
              .MaximumLength(500)
              .When(x => !string.IsNullOrEmpty(x.Game.ImageUrl))
              .WithMessage("ImageUrl must not exceed 500 characters.")
              .Must(BeValidUrl)
              .When(x => !string.IsNullOrEmpty(x.Game.ImageUrl))
              .WithMessage("ImageUrl must be a valid URL.");

      RuleFor(x => x.Game.Developer)
              .MaximumLength(200)
              .When(x => !string.IsNullOrEmpty(x.Game.Developer))
              .WithMessage("Developer must not exceed 200 characters.");

      RuleFor(x => x.Game.ReleaseDate)
              .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddYears(10))
              .When(x => x.Game.ReleaseDate.HasValue)
              .WithMessage("ReleaseDate cannot be more than 10 years in the future.");
    });
  }

  private static bool BeValidUrl(string? url)
  {
    if (string.IsNullOrEmpty(url))
      return true;

    return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
           (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
  }
}

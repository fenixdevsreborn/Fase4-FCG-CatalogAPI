using CatalogAPI.Application.DTOs;
using FluentValidation;

namespace CatalogAPI.Application.UseCases.Games.CreateGame;

public class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
  public CreateGameCommandValidator()
  {
    RuleFor(x => x.Game)
        .NotNull()
        .WithMessage("Game data is required.");

    When(x => x.Game != null, () =>
    {
      RuleFor(x => x.Game.Name)
              .NotEmpty()
              .WithMessage("Name is required.")
              .MaximumLength(200)
              .WithMessage("Name must not exceed 200 characters.");

      RuleFor(x => x.Game.Description)
              .MaximumLength(1000)
              .WithMessage("Description must not exceed 1000 characters.");

      RuleFor(x => x.Game.Price)
              .GreaterThan(0)
              .WithMessage("Price must be greater than 0.");

      RuleFor(x => x.Game.Genre)
              .NotEmpty()
              .WithMessage("Genre is required.")
              .MaximumLength(100)
              .WithMessage("Genre must not exceed 100 characters.");

      RuleFor(x => x.Game.ImageUrl)
              .MaximumLength(500)
              .WithMessage("ImageUrl must not exceed 500 characters.")
              .Must(BeValidUrl)
              .When(x => !string.IsNullOrEmpty(x.Game.ImageUrl))
              .WithMessage("ImageUrl must be a valid URL.");

      RuleFor(x => x.Game.Developer)
              .NotEmpty()
              .WithMessage("Developer is required.")
              .MaximumLength(200)
              .WithMessage("Developer must not exceed 200 characters.");

      RuleFor(x => x.Game.ReleaseDate)
              .NotEmpty()
              .WithMessage("ReleaseDate is required.")
              .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddYears(10))
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

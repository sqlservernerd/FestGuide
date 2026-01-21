using FluentValidation;
using FestGuide.Application.Dtos;

namespace FestGuide.Application.Validators;

/// <summary>
/// Validator for artist creation requests.
/// </summary>
public class CreateArtistRequestValidator : AbstractValidator<CreateArtistRequest>
{
    public CreateArtistRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Artist name is required.")
            .MinimumLength(1).WithMessage("Artist name must be at least 1 character long.")
            .MaximumLength(200).WithMessage("Artist name must not exceed 200 characters.");

        RuleFor(x => x.Genre)
            .MaximumLength(100).WithMessage("Genre must not exceed 100 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be a valid URL.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500).WithMessage("Website URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid URL.");

        RuleFor(x => x.SpotifyUrl)
            .MaximumLength(500).WithMessage("Spotify URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.SpotifyUrl))
            .WithMessage("Spotify URL must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for artist update requests.
/// </summary>
public class UpdateArtistRequestValidator : AbstractValidator<UpdateArtistRequest>
{
    public UpdateArtistRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(1).WithMessage("Artist name must be at least 1 character long.")
            .MaximumLength(200).WithMessage("Artist name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Genre)
            .MaximumLength(100).WithMessage("Genre must not exceed 100 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be a valid URL.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500).WithMessage("Website URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid URL.");

        RuleFor(x => x.SpotifyUrl)
            .MaximumLength(500).WithMessage("Spotify URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.SpotifyUrl))
            .WithMessage("Spotify URL must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

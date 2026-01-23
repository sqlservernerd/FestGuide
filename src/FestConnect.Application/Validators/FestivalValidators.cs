using FluentValidation;
using FestConnect.Application.Dtos;

namespace FestConnect.Application.Validators;

/// <summary>
/// Validator for festival creation requests.
/// </summary>
public class CreateFestivalRequestValidator : AbstractValidator<CreateFestivalRequest>
{
    public CreateFestivalRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Festival name is required.")
            .MinimumLength(2).WithMessage("Festival name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Festival name must not exceed 200 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be a valid URL.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500).WithMessage("Website URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for festival update requests.
/// </summary>
public class UpdateFestivalRequestValidator : AbstractValidator<UpdateFestivalRequest>
{
    public UpdateFestivalRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Festival name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Festival name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must be a valid URL.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500).WithMessage("Website URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for ownership transfer requests.
/// </summary>
public class TransferOwnershipRequestValidator : AbstractValidator<TransferOwnershipRequest>
{
    public TransferOwnershipRequestValidator()
    {
        RuleFor(x => x.NewOwnerUserId)
            .NotEmpty().WithMessage("New owner user ID is required.");
    }
}

using FluentValidation;
using FestGuide.Application.Dtos;

namespace FestGuide.Application.Validators;

/// <summary>
/// Validator for edition creation requests.
/// </summary>
public class CreateEditionRequestValidator : AbstractValidator<CreateEditionRequest>
{
    public CreateEditionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Edition name is required.")
            .MinimumLength(2).WithMessage("Edition name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Edition name must not exceed 200 characters.");

        RuleFor(x => x.StartDateUtc)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDateUtc)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDateUtc).WithMessage("End date must be on or after start date.");

        RuleFor(x => x.TimezoneId)
            .NotEmpty().WithMessage("Timezone is required.")
            .MaximumLength(100).WithMessage("Timezone must not exceed 100 characters.");

        RuleFor(x => x.TicketUrl)
            .MaximumLength(500).WithMessage("Ticket URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.TicketUrl))
            .WithMessage("Ticket URL must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for edition update requests.
/// </summary>
public class UpdateEditionRequestValidator : AbstractValidator<UpdateEditionRequest>
{
    public UpdateEditionRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Edition name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Edition name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.EndDateUtc)
            .GreaterThanOrEqualTo(x => x.StartDateUtc!.Value)
            .When(x => x.StartDateUtc.HasValue && x.EndDateUtc.HasValue)
            .WithMessage("End date must be on or after start date.");

        RuleFor(x => x.TimezoneId)
            .MaximumLength(100).WithMessage("Timezone must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.TimezoneId));

        RuleFor(x => x.TicketUrl)
            .MaximumLength(500).WithMessage("Ticket URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.TicketUrl))
            .WithMessage("Ticket URL must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

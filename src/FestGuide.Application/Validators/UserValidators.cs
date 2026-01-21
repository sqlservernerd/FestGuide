using FluentValidation;
using FestGuide.Application.Dtos;
using FestGuide.Infrastructure.Timezone;

namespace FestGuide.Application.Validators;

/// <summary>
/// Validator for profile update requests.
/// </summary>
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator(ITimezoneService timezoneService)
    {
        RuleFor(x => x.DisplayName)
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.PreferredTimezoneId)
            .Must(tz => tz != null && timezoneService.IsValidTimezone(tz))
            .WithMessage("Invalid IANA timezone identifier. Use format like 'America/New_York' or 'Europe/London'.")
            .When(x => !string.IsNullOrEmpty(x.PreferredTimezoneId));
    }
}

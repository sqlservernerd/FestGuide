using FluentValidation;
using FestConnect.Application.Dtos;

namespace FestConnect.Application.Validators;

/// <summary>
/// Validator for device registration requests.
/// </summary>
public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    private static readonly string[] ValidPlatforms = ["ios", "android", "web"];

    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Device token is required.")
            .MaximumLength(256).WithMessage("Device token must not exceed 256 characters.");

        RuleFor(x => x.Platform)
            .NotEmpty().WithMessage("Platform is required.")
            .Must(p => !string.IsNullOrEmpty(p) && ValidPlatforms.Contains(p.ToLowerInvariant()))
            .WithMessage("Platform must be one of (case-insensitive): ios, android, web.");

        RuleFor(x => x.DeviceName)
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.DeviceName));
    }
}

/// <summary>
/// Validator for notification preference update requests.
/// </summary>
public class UpdateNotificationPreferenceRequestValidator : AbstractValidator<UpdateNotificationPreferenceRequest>
{
    public UpdateNotificationPreferenceRequestValidator()
    {
        RuleFor(x => x.ReminderMinutesBefore)
            .InclusiveBetween(5, 120)
            .WithMessage("Reminder time must be between 5 and 120 minutes.")
            .When(x => x.ReminderMinutesBefore.HasValue);

        RuleFor(x => x)
            .Must(x => !(x.QuietHoursStart.HasValue ^ x.QuietHoursEnd.HasValue))
            .WithMessage("Both quiet hours start and end must be provided, or neither.");

        RuleFor(x => x)
            .Must(x =>
                !x.QuietHoursStart.HasValue ||
                !x.QuietHoursEnd.HasValue ||
                x.QuietHoursStart.Value != x.QuietHoursEnd.Value)
            .WithMessage("Quiet hours start must not equal quiet hours end.");
    }
}

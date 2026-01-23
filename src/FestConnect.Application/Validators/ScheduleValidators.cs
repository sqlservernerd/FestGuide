using FluentValidation;
using FestConnect.Application.Dtos;

namespace FestConnect.Application.Validators;

/// <summary>
/// Validator for time slot creation requests.
/// </summary>
public class CreateTimeSlotRequestValidator : AbstractValidator<CreateTimeSlotRequest>
{
    public CreateTimeSlotRequestValidator()
    {
        RuleFor(x => x.EditionId)
            .NotEmpty().WithMessage("Edition ID is required.");

        RuleFor(x => x.StartTimeUtc)
            .NotEmpty().WithMessage("Start time is required.");

        RuleFor(x => x.EndTimeUtc)
            .NotEmpty().WithMessage("End time is required.")
            .GreaterThan(x => x.StartTimeUtc).WithMessage("End time must be after start time.");
    }
}

/// <summary>
/// Validator for time slot update requests.
/// </summary>
public class UpdateTimeSlotRequestValidator : AbstractValidator<UpdateTimeSlotRequest>
{
    public UpdateTimeSlotRequestValidator()
    {
        RuleFor(x => x.EndTimeUtc)
            .GreaterThan(x => x.StartTimeUtc!.Value)
            .When(x => x.StartTimeUtc.HasValue && x.EndTimeUtc.HasValue)
            .WithMessage("End time must be after start time.");
    }
}

/// <summary>
/// Validator for engagement creation requests.
/// </summary>
public class CreateEngagementRequestValidator : AbstractValidator<CreateEngagementRequest>
{
    public CreateEngagementRequestValidator()
    {
        RuleFor(x => x.ArtistId)
            .NotEmpty().WithMessage("Artist ID is required.");
    }
}

/// <summary>
/// Validator for engagement update requests.
/// </summary>
public class UpdateEngagementRequestValidator : AbstractValidator<UpdateEngagementRequest>
{
    public UpdateEngagementRequestValidator()
    {
        // All fields are optional for update
    }
}

using FluentValidation;
using FestGuide.Application.Dtos;

namespace FestGuide.Application.Validators;

/// <summary>
/// Validator for personal schedule creation requests.
/// </summary>
public class CreatePersonalScheduleRequestValidator : AbstractValidator<CreatePersonalScheduleRequest>
{
    public CreatePersonalScheduleRequestValidator()
    {
        RuleFor(x => x.EditionId)
            .NotEmpty().WithMessage("Edition ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Schedule name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));
    }
}

/// <summary>
/// Validator for personal schedule update requests.
/// </summary>
public class UpdatePersonalScheduleRequestValidator : AbstractValidator<UpdatePersonalScheduleRequest>
{
    public UpdatePersonalScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Schedule name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));
    }
}

/// <summary>
/// Validator for adding entries to a personal schedule.
/// </summary>
public class AddScheduleEntryRequestValidator : AbstractValidator<AddScheduleEntryRequest>
{
    public AddScheduleEntryRequestValidator()
    {
        RuleFor(x => x.EngagementId)
            .NotEmpty().WithMessage("Engagement ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

/// <summary>
/// Validator for updating schedule entries.
/// </summary>
public class UpdateScheduleEntryRequestValidator : AbstractValidator<UpdateScheduleEntryRequest>
{
    public UpdateScheduleEntryRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

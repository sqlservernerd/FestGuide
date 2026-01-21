using FluentValidation;
using FestGuide.Application.Dtos;

namespace FestGuide.Application.Validators;

/// <summary>
/// Validator for venue creation requests.
/// </summary>
public class CreateVenueRequestValidator : AbstractValidator<CreateVenueRequest>
{
    public CreateVenueRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Venue name is required.")
            .MinimumLength(2).WithMessage("Venue name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Venue name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}

/// <summary>
/// Validator for venue update requests.
/// </summary>
public class UpdateVenueRequestValidator : AbstractValidator<UpdateVenueRequest>
{
    public UpdateVenueRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Venue name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Venue name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}

/// <summary>
/// Validator for stage creation requests.
/// </summary>
public class CreateStageRequestValidator : AbstractValidator<CreateStageRequest>
{
    public CreateStageRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Stage name is required.")
            .MinimumLength(2).WithMessage("Stage name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Stage name must not exceed 200 characters.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}

/// <summary>
/// Validator for stage update requests.
/// </summary>
public class UpdateStageRequestValidator : AbstractValidator<UpdateStageRequest>
{
    public UpdateStageRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Stage name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Stage name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).When(x => x.SortOrder.HasValue)
            .WithMessage("Sort order must be non-negative.");
    }
}

using FluentValidation;
using FestGuide.Application.Dtos;
using FestGuide.Domain.Enums;

namespace FestGuide.Application.Validators;

/// <summary>
/// Validator for user invitation requests.
/// </summary>
public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role specified.")
            .NotEqual(FestivalRole.Owner).WithMessage("Cannot invite users as Owner. Use ownership transfer instead.");

        RuleFor(x => x.Scope)
            .IsInEnum().WithMessage("Invalid scope specified.");

        // Scope is only meaningful for Manager and Viewer roles
        RuleFor(x => x.Scope)
            .Equal(PermissionScope.All)
            .When(x => x.Role == FestivalRole.Administrator)
            .WithMessage("Administrators automatically have access to all scopes.");
    }
}

/// <summary>
/// Validator for permission update requests.
/// </summary>
public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role specified.")
            .NotEqual(FestivalRole.Owner).When(x => x.Role.HasValue)
            .WithMessage("Cannot change role to Owner. Use ownership transfer instead.");

        RuleFor(x => x.Scope)
            .IsInEnum().WithMessage("Invalid scope specified.")
            .When(x => x.Scope.HasValue);

        // At least one field must be provided
        RuleFor(x => x)
            .Must(x => x.Role.HasValue || x.Scope.HasValue)
            .WithMessage("At least one field (Role or Scope) must be provided.");
    }
}

/// <summary>
/// Validator for accepting invitation requests.
/// </summary>
public class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequest>
{
    public AcceptInvitationRequestValidator()
    {
        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("Permission ID is required.");
    }
}

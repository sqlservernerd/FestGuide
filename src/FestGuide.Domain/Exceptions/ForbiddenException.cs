namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user is not authorized to perform an action.
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string action, string resource)
        : base($"You do not have permission to {action} this {resource}.")
    {
        Action = action;
        Resource = resource;
    }

    public string? Action { get; }
    public string? Resource { get; }

    public static ForbiddenException CannotEditFestival(long festivalId) =>
        new("edit", "festival") { FestivalId = festivalId };

    public static ForbiddenException CannotDeleteFestival(long festivalId) =>
        new("delete", "festival") { FestivalId = festivalId };

    public static ForbiddenException CannotManagePermissions(long festivalId) =>
        new("manage permissions for", "festival") { FestivalId = festivalId };

    public long? FestivalId { get; init; }
}

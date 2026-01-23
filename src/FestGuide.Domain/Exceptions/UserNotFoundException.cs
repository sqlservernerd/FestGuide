namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user is not found.
/// </summary>
public class UserNotFoundException : DomainException
{
    public UserNotFoundException(long userId)
        : base($"User with ID '{userId}' was not found.")
    {
        UserId = userId;
    }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.")
    {
        Email = email;
    }

    public long? UserId { get; }
    public string? Email { get; }
}

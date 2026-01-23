namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when a duplicate resource is detected.
/// </summary>
public class DuplicateException : DomainException
{
    public DuplicateException(string resourceType, string field, string value)
        : base($"A {resourceType} with {field} '{value}' already exists.")
    {
        ResourceType = resourceType;
        Field = field;
        Value = value;
    }

    public string ResourceType { get; }
    public string Field { get; }
    public string Value { get; }

    public static DuplicateException UserEmail(string email) =>
        new("User", "email", email);
}

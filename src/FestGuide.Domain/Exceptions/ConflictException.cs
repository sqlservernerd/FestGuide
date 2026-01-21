namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a resource conflict occurs (e.g., duplicate, already exists).
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, string resourceType, object resourceId)
        : base(message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public string? ResourceType { get; }
    public object? ResourceId { get; }
}

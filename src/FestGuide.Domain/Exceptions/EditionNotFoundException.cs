namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a festival edition is not found.
/// </summary>
public class EditionNotFoundException : DomainException
{
    public EditionNotFoundException(Guid editionId)
        : base($"Edition with ID '{editionId}' was not found.")
    {
        EditionId = editionId;
    }

    public Guid EditionId { get; }
}

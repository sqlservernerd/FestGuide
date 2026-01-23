namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a festival edition is not found.
/// </summary>
public class EditionNotFoundException : DomainException
{
    public EditionNotFoundException(long editionId)
        : base($"Edition with ID '{editionId}' was not found.")
    {
        EditionId = editionId;
    }

    public long EditionId { get; }
}

namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a schedule is not found.
/// </summary>
public class ScheduleNotFoundException : DomainException
{
    public ScheduleNotFoundException(Guid editionId)
        : base($"Schedule for edition with ID '{editionId}' was not found.")
    {
        EditionId = editionId;
    }

    public Guid EditionId { get; }
}

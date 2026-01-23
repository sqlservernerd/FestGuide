namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when a schedule is not found.
/// </summary>
public class ScheduleNotFoundException : DomainException
{
    public ScheduleNotFoundException(long editionId)
        : base($"Schedule for edition with ID '{editionId}' was not found.")
    {
        EditionId = editionId;
    }

    public long EditionId { get; }
}

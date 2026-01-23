namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a time slot is not found.
/// </summary>
public class TimeSlotNotFoundException : DomainException
{
    public TimeSlotNotFoundException(long timeSlotId)
        : base($"Time slot with ID '{timeSlotId}' was not found.")
    {
        TimeSlotId = timeSlotId;
    }

    public long TimeSlotId { get; }
}

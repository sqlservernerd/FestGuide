namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when a personal schedule is not found.
/// </summary>
public class PersonalScheduleNotFoundException : DomainException
{
    public PersonalScheduleNotFoundException(long personalScheduleId)
        : base($"Personal schedule with ID '{personalScheduleId}' was not found.")
    {
        PersonalScheduleId = personalScheduleId;
    }

    public long PersonalScheduleId { get; }
}

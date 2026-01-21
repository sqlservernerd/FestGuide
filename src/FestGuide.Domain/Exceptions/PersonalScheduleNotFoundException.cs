namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a personal schedule is not found.
/// </summary>
public class PersonalScheduleNotFoundException : DomainException
{
    public PersonalScheduleNotFoundException(Guid personalScheduleId)
        : base($"Personal schedule with ID '{personalScheduleId}' was not found.")
    {
        PersonalScheduleId = personalScheduleId;
    }

    public Guid PersonalScheduleId { get; }
}

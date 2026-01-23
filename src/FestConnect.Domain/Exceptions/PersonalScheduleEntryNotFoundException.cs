namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when a personal schedule entry is not found.
/// </summary>
public class PersonalScheduleEntryNotFoundException : DomainException
{
    public PersonalScheduleEntryNotFoundException(long entryId)
        : base($"Personal schedule entry with ID '{entryId}' was not found.")
    {
        EntryId = entryId;
    }

    public long EntryId { get; }
}

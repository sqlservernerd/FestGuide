namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a festival is not found.
/// </summary>
public class FestivalNotFoundException : DomainException
{
    public FestivalNotFoundException(long festivalId)
        : base($"Festival with ID '{festivalId}' was not found.")
    {
        FestivalId = festivalId;
    }

    public long FestivalId { get; }
}

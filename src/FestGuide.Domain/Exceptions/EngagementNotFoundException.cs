namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when an engagement is not found.
/// </summary>
public class EngagementNotFoundException : DomainException
{
    public EngagementNotFoundException(Guid engagementId)
        : base($"Engagement with ID '{engagementId}' was not found.")
    {
        EngagementId = engagementId;
    }

    public Guid EngagementId { get; }
}

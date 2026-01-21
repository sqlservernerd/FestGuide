namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a stage is not found.
/// </summary>
public class StageNotFoundException : DomainException
{
    public StageNotFoundException(Guid stageId)
        : base($"Stage with ID '{stageId}' was not found.")
    {
        StageId = stageId;
    }

    public Guid StageId { get; }
}

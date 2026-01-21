namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a permission is not found.
/// </summary>
public class PermissionNotFoundException : DomainException
{
    public PermissionNotFoundException(Guid permissionId)
        : base($"Permission with ID '{permissionId}' was not found.")
    {
        PermissionId = permissionId;
    }

    public Guid PermissionId { get; }
}

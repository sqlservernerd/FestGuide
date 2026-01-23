namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when a permission is not found.
/// </summary>
public class PermissionNotFoundException : DomainException
{
    public PermissionNotFoundException(long permissionId)
        : base($"Permission with ID '{permissionId}' was not found.")
    {
        PermissionId = permissionId;
    }

    public long PermissionId { get; }
}

using FestGuide.Domain.Enums;

namespace FestGuide.Application.Authorization;

/// <summary>
/// Service interface for festival-level authorization checks.
/// </summary>
public interface IFestivalAuthorizationService
{
    /// <summary>
    /// Checks if a user can view a festival's details.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user can view the festival.</returns>
    Task<bool> CanViewFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can edit a festival's details.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user can edit the festival.</returns>
    Task<bool> CanEditFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can delete a festival.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user can delete the festival (owner only).</returns>
    Task<bool> CanDeleteFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has a specific permission scope for a festival.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="scope">The permission scope to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has the specified scope.</returns>
    Task<bool> HasScopeAsync(Guid userId, Guid festivalId, PermissionScope scope, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's role for a festival.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The role name, or null if no permission exists.</returns>
    Task<string?> GetRoleAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can manage permissions for a festival.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user can manage permissions (owner or admin).</returns>
    Task<bool> CanManagePermissionsAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can transfer ownership of a festival.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user can transfer ownership (owner only).</returns>
    Task<bool> CanTransferOwnershipAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can publish the schedule for an edition.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user can publish schedules.</returns>
    Task<bool> CanPublishScheduleAsync(Guid userId, Guid festivalId, CancellationToken ct = default);
}

using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Festival data access operations.
/// </summary>
public interface IFestivalRepository
{
    /// <summary>
    /// Gets a festival by its unique identifier.
    /// </summary>
    Task<Festival?> GetByIdAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple festivals by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<Festival>> GetByIdsAsync(IEnumerable<long> festivalIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all festivals owned by a user.
    /// </summary>
    Task<IReadOnlyList<Festival>> GetByOwnerAsync(long ownerUserId, CancellationToken ct = default);

    /// <summary>
    /// Gets all festivals a user has access to (through permissions).
    /// </summary>
    Task<IReadOnlyList<Festival>> GetByUserAccessAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Searches festivals by name.
    /// </summary>
    Task<IReadOnlyList<Festival>> SearchByNameAsync(string searchTerm, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Creates a new festival.
    /// </summary>
    Task<long> CreateAsync(Festival festival, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing festival.
    /// </summary>
    Task UpdateAsync(Festival festival, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a festival.
    /// </summary>
    Task DeleteAsync(long festivalId, long deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a festival exists.
    /// </summary>
    Task<bool> ExistsAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Transfers ownership of a festival to another user.
    /// </summary>
    Task TransferOwnershipAsync(long festivalId, long newOwnerUserId, long modifiedBy, CancellationToken ct = default);
}

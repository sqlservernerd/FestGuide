using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Schedule data access operations.
/// </summary>
public interface IScheduleRepository
{
    /// <summary>
    /// Gets a schedule by its unique identifier.
    /// </summary>
    Task<Schedule?> GetByIdAsync(long scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets the schedule for an edition.
    /// </summary>
    Task<Schedule?> GetByEditionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new schedule.
    /// </summary>
    Task<long> CreateAsync(Schedule schedule, CancellationToken ct = default);

    /// <summary>
    /// Publishes a schedule, updating version and publish timestamp.
    /// </summary>
    Task PublishAsync(long scheduleId, long publishedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a schedule exists for an edition.
    /// </summary>
    Task<bool> ExistsForEditionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets or creates a schedule for an edition.
    /// </summary>
    Task<Schedule> GetOrCreateAsync(long editionId, long createdBy, CancellationToken ct = default);
}

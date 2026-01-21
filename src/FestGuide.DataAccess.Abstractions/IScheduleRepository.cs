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
    Task<Schedule?> GetByIdAsync(Guid scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets the schedule for an edition.
    /// </summary>
    Task<Schedule?> GetByEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new schedule.
    /// </summary>
    Task<Guid> CreateAsync(Schedule schedule, CancellationToken ct = default);

    /// <summary>
    /// Publishes a schedule, updating version and publish timestamp.
    /// </summary>
    Task PublishAsync(Guid scheduleId, Guid publishedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a schedule exists for an edition.
    /// </summary>
    Task<bool> ExistsForEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets or creates a schedule for an edition.
    /// </summary>
    Task<Schedule> GetOrCreateAsync(Guid editionId, Guid createdBy, CancellationToken ct = default);
}

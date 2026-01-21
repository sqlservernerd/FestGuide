using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for festival operations.
/// </summary>
public interface IFestivalService
{
    /// <summary>
    /// Gets a festival by ID.
    /// </summary>
    Task<FestivalDto> GetByIdAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all festivals the user has access to.
    /// </summary>
    Task<IReadOnlyList<FestivalSummaryDto>> GetMyFestivalsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Searches festivals by name.
    /// </summary>
    Task<IReadOnlyList<FestivalSummaryDto>> SearchAsync(string searchTerm, Guid? userId = null, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Creates a new festival.
    /// </summary>
    Task<FestivalDto> CreateAsync(Guid userId, CreateFestivalRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing festival.
    /// </summary>
    Task<FestivalDto> UpdateAsync(Guid festivalId, Guid userId, UpdateFestivalRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a festival.
    /// </summary>
    Task DeleteAsync(Guid festivalId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Transfers festival ownership to another user.
    /// </summary>
    Task TransferOwnershipAsync(Guid festivalId, Guid currentUserId, TransferOwnershipRequest request, CancellationToken ct = default);
}

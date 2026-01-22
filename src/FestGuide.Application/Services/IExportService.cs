using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for export operations.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports edition data in the specified format.
    /// </summary>
    Task<ExportResultDto> ExportEditionDataAsync(Guid editionId, Guid organizerId, ExportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Exports the schedule as CSV.
    /// </summary>
    Task<ExportResultDto> ExportScheduleCsvAsync(Guid editionId, Guid organizerId, CancellationToken ct = default);

    /// <summary>
    /// Exports artist list as CSV.
    /// </summary>
    Task<ExportResultDto> ExportArtistsCsvAsync(Guid editionId, Guid organizerId, CancellationToken ct = default);

    /// <summary>
    /// Exports analytics summary as CSV.
    /// </summary>
    Task<ExportResultDto> ExportAnalyticsCsvAsync(Guid editionId, Guid organizerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    /// <summary>
    /// Exports personal schedule entries (attendee saves) as CSV.
    /// </summary>
    Task<ExportResultDto> ExportAttendeeSavesCsvAsync(Guid editionId, Guid organizerId, CancellationToken ct = default);
}

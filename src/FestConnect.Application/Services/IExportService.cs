using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for export operations.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports edition data in the specified format.
    /// </summary>
    Task<ExportResultDto> ExportEditionDataAsync(long editionId, long organizerId, ExportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Exports the schedule as CSV.
    /// </summary>
    Task<ExportResultDto> ExportScheduleCsvAsync(long editionId, long organizerId, CancellationToken ct = default);

    /// <summary>
    /// Exports artist list as CSV.
    /// </summary>
    Task<ExportResultDto> ExportArtistsCsvAsync(long editionId, long organizerId, CancellationToken ct = default);

    /// <summary>
    /// Exports analytics summary as CSV.
    /// </summary>
    Task<ExportResultDto> ExportAnalyticsCsvAsync(long editionId, long organizerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    /// <summary>
    /// Exports personal schedule entries (attendee saves) as CSV.
    /// </summary>
    Task<ExportResultDto> ExportAttendeeSavesCsvAsync(long editionId, long organizerId, CancellationToken ct = default);
}

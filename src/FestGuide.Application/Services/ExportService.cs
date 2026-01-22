using System.Text;
using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Export service implementation for generating CSV exports.
/// </summary>
public class ExportService : IExportService
{
    private readonly IEditionRepository _editionRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly IEngagementRepository _engagementRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IFestivalAuthorizationService _authService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IEditionRepository editionRepository,
        ITimeSlotRepository timeSlotRepository,
        IEngagementRepository engagementRepository,
        IArtistRepository artistRepository,
        IStageRepository stageRepository,
        IAnalyticsRepository analyticsRepository,
        IFestivalAuthorizationService authService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ExportService> logger)
    {
        _editionRepository = editionRepository ?? throw new ArgumentNullException(nameof(editionRepository));
        _timeSlotRepository = timeSlotRepository ?? throw new ArgumentNullException(nameof(timeSlotRepository));
        _engagementRepository = engagementRepository ?? throw new ArgumentNullException(nameof(engagementRepository));
        _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
        _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ExportResultDto> ExportEditionDataAsync(Guid editionId, Guid organizerId, ExportRequest request, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var sb = new StringBuilder();

        if (request.IncludeSchedule)
        {
            sb.AppendLine("=== SCHEDULE ===");
            var scheduleCsv = await BuildScheduleCsvAsync(editionId, ct);
            sb.AppendLine(scheduleCsv);
            sb.AppendLine();
        }

        if (request.IncludeArtists)
        {
            sb.AppendLine("=== ARTISTS ===");
            var artistsCsv = await BuildArtistsCsvAsync(editionId, ct);
            sb.AppendLine(artistsCsv);
            sb.AppendLine();
        }

        if (request.IncludeAnalytics)
        {
            sb.AppendLine("=== ANALYTICS ===");
            var analyticsCsv = await BuildAnalyticsCsvAsync(editionId, request.FromUtc, request.ToUtc, ct);
            sb.AppendLine(analyticsCsv);
        }

        var fileName = $"{edition.Name.Replace(" ", "_")}_export_{_dateTimeProvider.UtcNow:yyyyMMdd_HHmmss}.csv";
        var data = Encoding.UTF8.GetBytes(sb.ToString());

        _logger.LogInformation("Export generated for edition {EditionId} by user {OrganizerId}", editionId, organizerId);

        return new ExportResultDto(fileName, "text/csv", data);
    }

    /// <inheritdoc />
    public async Task<ExportResultDto> ExportScheduleCsvAsync(Guid editionId, Guid organizerId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var csv = await BuildScheduleCsvAsync(editionId, ct);
        var fileName = $"{edition.Name.Replace(" ", "_")}_schedule_{_dateTimeProvider.UtcNow:yyyyMMdd}.csv";
        var data = Encoding.UTF8.GetBytes(csv);

        return new ExportResultDto(fileName, "text/csv", data);
    }

    /// <inheritdoc />
    public async Task<ExportResultDto> ExportArtistsCsvAsync(Guid editionId, Guid organizerId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var csv = await BuildArtistsCsvAsync(editionId, ct);
        var fileName = $"{edition.Name.Replace(" ", "_")}_artists_{_dateTimeProvider.UtcNow:yyyyMMdd}.csv";
        var data = Encoding.UTF8.GetBytes(csv);

        return new ExportResultDto(fileName, "text/csv", data);
    }

    /// <inheritdoc />
    public async Task<ExportResultDto> ExportAnalyticsCsvAsync(Guid editionId, Guid organizerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var csv = await BuildAnalyticsCsvAsync(editionId, fromUtc, toUtc, ct);
        var fileName = $"{edition.Name.Replace(" ", "_")}_analytics_{_dateTimeProvider.UtcNow:yyyyMMdd}.csv";
        var data = Encoding.UTF8.GetBytes(csv);

        return new ExportResultDto(fileName, "text/csv", data);
    }

    /// <inheritdoc />
    public async Task<ExportResultDto> ExportAttendeeSavesCsvAsync(Guid editionId, Guid organizerId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var sb = new StringBuilder();
        sb.AppendLine("EngagementId,ArtistName,StageName,StartTimeUtc,EndTimeUtc,SaveCount");

        var topEngagements = await _analyticsRepository.GetTopSavedEngagementsAsync(editionId, 100, ct);

        var csvLinesTasks = topEngagements.Select(async item =>
        {
            var (engagementId, saveCount) = item;

            var engagement = await _engagementRepository.GetByIdAsync(engagementId, ct).ConfigureAwait(false);
            if (engagement == null) return null;

            var artistTask = _artistRepository.GetByIdAsync(engagement.ArtistId, ct);
            var timeSlotTask = _timeSlotRepository.GetByIdAsync(engagement.TimeSlotId, ct);

            var artist = await artistTask.ConfigureAwait(false);
            var timeSlot = await timeSlotTask.ConfigureAwait(false);

            var stage = timeSlot != null
                ? await _stageRepository.GetByIdAsync(timeSlot.StageId, ct).ConfigureAwait(false)
                : null;

            return $"{engagementId},{EscapeCsv(artist?.Name)},{EscapeCsv(stage?.Name)},{timeSlot?.StartTimeUtc:o},{timeSlot?.EndTimeUtc:o},{saveCount}";
        });

        var csvLines = await Task.WhenAll(csvLinesTasks).ConfigureAwait(false);

        foreach (var line in csvLines)
        {
            if (line != null) sb.AppendLine(line);
        }

        var fileName = $"{edition.Name.Replace(" ", "_")}_attendee_saves_{_dateTimeProvider.UtcNow:yyyyMMdd}.csv";
        var data = Encoding.UTF8.GetBytes(sb.ToString());

        return new ExportResultDto(fileName, "text/csv", data);
    }

    private async Task<string> BuildScheduleCsvAsync(Guid editionId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TimeSlotId,StageId,StageName,StartTimeUtc,EndTimeUtc,ArtistId,ArtistName");

        var timeSlots = await _timeSlotRepository.GetByEditionAsync(editionId, ct);

        var csvLinesTasks = timeSlots.OrderBy(t => t.StartTimeUtc).Select(async timeSlot =>
        {
            var stageTask = _stageRepository.GetByIdAsync(timeSlot.StageId, ct);
            var engagementTask = _engagementRepository.GetByTimeSlotAsync(timeSlot.TimeSlotId, ct);

            var stage = await stageTask.ConfigureAwait(false);
            var engagement = await engagementTask.ConfigureAwait(false);

            if (engagement == null)
            {
                return null;
            }

            var artist = await _artistRepository.GetByIdAsync(engagement.ArtistId, ct).ConfigureAwait(false);
            return $"{timeSlot.TimeSlotId},{timeSlot.StageId},{EscapeCsv(stage?.Name)},{timeSlot.StartTimeUtc:o},{timeSlot.EndTimeUtc:o},{engagement.ArtistId},{EscapeCsv(artist?.Name)}";
        });

        var csvLines = await Task.WhenAll(csvLinesTasks).ConfigureAwait(false);

        foreach (var line in csvLines)
        {
            if (line != null) sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private async Task<string> BuildArtistsCsvAsync(Guid editionId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ArtistId,Name,Genre,Bio,WebsiteUrl,ImageUrl");

        var edition = await _editionRepository.GetByIdAsync(editionId, ct);
        if (edition == null) return sb.ToString();

        var artists = await _artistRepository.GetByFestivalAsync(edition.FestivalId, ct);

        foreach (var artist in artists.OrderBy(a => a.Name))
        {
            sb.AppendLine($"{artist.ArtistId},{EscapeCsv(artist.Name)},{EscapeCsv(artist.Genre)},{EscapeCsv(artist.Bio)},{EscapeCsv(artist.WebsiteUrl)},{EscapeCsv(artist.ImageUrl)}");
        }

        return sb.ToString();
    }

    private async Task<string> BuildAnalyticsCsvAsync(Guid editionId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Summary metrics
        sb.AppendLine("Metric,Value");
        var scheduleViews = await _analyticsRepository.GetScheduleViewCountAsync(editionId, fromUtc, toUtc, ct);
        var uniqueViewers = await _analyticsRepository.GetUniqueViewerCountAsync(editionId, fromUtc, toUtc, ct);
        var personalSchedules = await _analyticsRepository.GetPersonalScheduleCountAsync(editionId, ct);
        var totalSaves = await _analyticsRepository.GetTotalEngagementSavesAsync(editionId, ct);

        sb.AppendLine($"Total Schedule Views,{scheduleViews}");
        sb.AppendLine($"Unique Viewers,{uniqueViewers}");
        sb.AppendLine($"Personal Schedules Created,{personalSchedules}");
        sb.AppendLine($"Total Engagement Saves,{totalSaves}");
        sb.AppendLine();

        // Platform distribution
        sb.AppendLine("Platform,Count");
        var platforms = await _analyticsRepository.GetPlatformDistributionAsync(editionId, ct);
        foreach (var (platform, count) in platforms)
        {
            sb.AppendLine($"{platform},{count}");
        }
        sb.AppendLine();

        // Top artists
        sb.AppendLine("Top Artists,SaveCount");
        var topArtists = await _analyticsRepository.GetTopArtistsAsync(editionId, 10, ct);
        foreach (var (artistId, artistName, saveCount) in topArtists)
        {
            sb.AppendLine($"{EscapeCsv(artistName)},{saveCount}");
        }

        return sb.ToString();
    }

    private async Task EnsureOrganizerAccessAsync(Guid festivalId, Guid organizerId, CancellationToken ct)
    {
        if (!await _authService.CanViewAnalyticsAsync(organizerId, festivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to export data for this festival.");
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

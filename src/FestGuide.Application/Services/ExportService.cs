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
    // Cache invalid filename characters as array for efficient IndexOfAny lookups
    private static readonly char[] InvalidFilenameChars;
    
    static ExportService()
    {
        // Build array of invalid filename characters including space
        var invalidChars = Path.GetInvalidFileNameChars();
        InvalidFilenameChars = new char[invalidChars.Length + 1];
        Array.Copy(invalidChars, InvalidFilenameChars, invalidChars.Length);
        InvalidFilenameChars[invalidChars.Length] = ' ';
    }
    
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
        ArgumentNullException.ThrowIfNull(request);
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

        var fileName = $"{SanitizeFilename(edition.Name, "_export")}.csv";
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
        var fileName = $"{SanitizeFilename(edition.Name, "_schedule")}.csv";
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
        var fileName = $"{SanitizeFilename(edition.Name, "_artists")}.csv";
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
        var fileName = $"{SanitizeFilename(edition.Name, "_analytics")}.csv";
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

        if (topEngagements.Count == 0)
        {
            var emptyFileName = $"{SanitizeFilename(edition.Name, "_attendee_saves")}.csv";
            var emptyData = Encoding.UTF8.GetBytes(sb.ToString());
            return new ExportResultDto(emptyFileName, "text/csv", emptyData);
        }

        // Batch fetch all engagements - fail fast if any are missing
        var engagementIds = topEngagements.Select(e => e.EngagementId).ToList();
        var engagementTasks = engagementIds.Select(id => _engagementRepository.GetByIdAsync(id, ct)).ToArray();
        
        Domain.Entities.Engagement?[] engagements;
        try
        {
            engagements = await Task.WhenAll(engagementTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch engagement data during export for edition {EditionId}",
                editionId);
            throw new InvalidOperationException(
                "Export failed due to missing engagement data. Please try again or contact support if the problem persists.",
                ex);
        }
        
        var engagementDictionary = new Dictionary<Guid, Domain.Entities.Engagement>();
        var missingEngagementIds = new List<Guid>();
        
        for (int i = 0; i < engagements.Length; i++)
        {
            var engagement = engagements[i];
            if (engagement is null)
            {
                missingEngagementIds.Add(engagementIds[i]);
            }
            else
            {
                engagementDictionary[engagement.EngagementId] = engagement;
            }
        }

        if (missingEngagementIds.Count > 0)
        {
            _logger.LogError(
                "Data integrity issue: Export for edition {EditionId} references {MissingCount} missing engagements. Missing IDs: {MissingIds}",
                editionId,
                missingEngagementIds.Count,
                string.Join(", ", missingEngagementIds));
            throw new InvalidOperationException(
                "Export failed due to missing engagement data. Please contact support.");
        }

        // Batch fetch all artists - fail fast if any are missing
        var artistIds = engagementDictionary.Values.Select(e => e.ArtistId).Distinct().ToList();
        var artistTasks = artistIds.Select(id => _artistRepository.GetByIdAsync(id, ct)).ToArray();
        
        Domain.Entities.Artist?[] artists;
        try
        {
            artists = await Task.WhenAll(artistTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch artist data during export for edition {EditionId}",
                editionId);
            throw new InvalidOperationException(
                "Export failed due to missing artist data. Please try again or contact support if the problem persists.",
                ex);
        }
        
        var artistDictionary = new Dictionary<Guid, Domain.Entities.Artist>();
        var missingArtistIds = new List<Guid>();
        
        for (int i = 0; i < artists.Length; i++)
        {
            var artist = artists[i];
            if (artist is null)
            {
                missingArtistIds.Add(artistIds[i]);
            }
            else
            {
                artistDictionary[artist.ArtistId] = artist;
            }
        }

        if (missingArtistIds.Count > 0)
        {
            _logger.LogError(
                "Data integrity issue: Export for edition {EditionId} references {MissingCount} missing artists. Missing IDs: {MissingIds}",
                editionId,
                missingArtistIds.Count,
                string.Join(", ", missingArtistIds));
            throw new InvalidOperationException(
                "Export failed due to missing artist data. Please contact support.");
        }

        // Batch fetch all time slots - fail fast if any are missing
        var timeSlotIds = engagementDictionary.Values.Select(e => e.TimeSlotId).Distinct().ToList();
        var timeSlotTasks = timeSlotIds.Select(id => _timeSlotRepository.GetByIdAsync(id, ct)).ToArray();
        
        Domain.Entities.TimeSlot?[] timeSlots;
        try
        {
            timeSlots = await Task.WhenAll(timeSlotTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch time slot data during export for edition {EditionId}",
                editionId);
            throw new InvalidOperationException(
                "Export failed due to missing time slot data. Please try again or contact support if the problem persists.",
                ex);
        }
        
        var timeSlotDictionary = new Dictionary<Guid, Domain.Entities.TimeSlot>();
        var missingTimeSlotIds = new List<Guid>();
        
        for (int i = 0; i < timeSlots.Length; i++)
        {
            var timeSlot = timeSlots[i];
            if (timeSlot is null)
            {
                missingTimeSlotIds.Add(timeSlotIds[i]);
            }
            else
            {
                timeSlotDictionary[timeSlot.TimeSlotId] = timeSlot;
            }
        }

        if (missingTimeSlotIds.Count > 0)
        {
            _logger.LogError(
                "Data integrity issue: Export for edition {EditionId} references {MissingCount} missing time slots. Missing IDs: {MissingIds}",
                editionId,
                missingTimeSlotIds.Count,
                string.Join(", ", missingTimeSlotIds));
            throw new InvalidOperationException(
                "Export failed due to missing time slot data. Please contact support.");
        }

        // Batch fetch all stages - fail fast if any are missing
        var stageIdsList = timeSlotDictionary.Values.Select(ts => ts.StageId).Distinct().ToList();
        
        var stageTasks = stageIdsList.Select(id => _stageRepository.GetByIdAsync(id, ct)).ToArray();
        
        Domain.Entities.Stage?[] stages;
        try
        {
            stages = await Task.WhenAll(stageTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch stage data during export for edition {EditionId}",
                editionId);
            throw new InvalidOperationException(
                "Export failed due to missing stage data. Please try again or contact support if the problem persists.",
                ex);
        }
        
        var stageDictionary = new Dictionary<Guid, Domain.Entities.Stage>();
        var missingStageIds = new List<Guid>();
        
        for (int i = 0; i < stages.Length; i++)
        {
            var stage = stages[i];
            if (stage is null)
            {
                missingStageIds.Add(stageIdsList[i]);
            }
            else
            {
                stageDictionary[stage.StageId] = stage;
            }
        }

        if (missingStageIds.Count > 0)
        {
            _logger.LogError(
                "Data integrity issue: Export for edition {EditionId} references {MissingCount} missing stages. Missing IDs: {MissingIds}",
                editionId,
                missingStageIds.Count,
                string.Join(", ", missingStageIds));
            throw new InvalidOperationException(
                "Export failed due to missing stage data. Please contact support.");
        }

        // Build CSV lines - all data is guaranteed to exist at this point
        foreach (var item in topEngagements)
        {
            var (engagementId, saveCount) = item;

            var engagement = engagementDictionary[engagementId];
            var artist = artistDictionary[engagement.ArtistId];
            var timeSlot = timeSlotDictionary[engagement.TimeSlotId];
            var stage = stageDictionary[timeSlot.StageId];

            var line =
                $"{engagementId},{EscapeCsv(artist.Name)},{EscapeCsv(stage.Name)},{timeSlot.StartTimeUtc:o},{timeSlot.EndTimeUtc:o},{saveCount}";

            sb.AppendLine(line);
        }

        var fileName = $"{SanitizeFilename(edition.Name, "_attendee_saves")}.csv";
        var data = Encoding.UTF8.GetBytes(sb.ToString());

        return new ExportResultDto(fileName, "text/csv", data);
    }

    private async Task<string> BuildScheduleCsvAsync(Guid editionId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TimeSlotId,StageId,StageName,StartTimeUtc,EndTimeUtc,ArtistId,ArtistName");

        var timeSlots = await _timeSlotRepository.GetByEditionAsync(editionId, ct);

        if (timeSlots.Count == 0)
        {
            return sb.ToString();
        }

        var orderedTimeSlots = timeSlots.OrderBy(t => t.StartTimeUtc).ToList();

        // Batch fetch all stages
        var stageIds = orderedTimeSlots.Select(t => t.StageId).Distinct().ToList();
        var stageTasks = stageIds.Select(id => _stageRepository.GetByIdAsync(id, ct)).ToArray();
        var stages = await Task.WhenAll(stageTasks);
        
        var stageDictionary = new Dictionary<Guid, Domain.Entities.Stage>();
        foreach (var stage in stages.OfType<Domain.Entities.Stage>())
        {
            stageDictionary[stage.StageId] = stage;
        }

        // Batch fetch all engagements
        var timeSlotIds = orderedTimeSlots.Select(t => t.TimeSlotId).ToList();
        var engagementTasks = timeSlotIds.Select(id => _engagementRepository.GetByTimeSlotAsync(id, ct)).ToArray();
        var engagements = await Task.WhenAll(engagementTasks);
        
        var engagementDictionary = new Dictionary<Guid, Domain.Entities.Engagement>();
        foreach (var engagement in engagements.Where(e => e != null))
        {
            engagementDictionary[engagement!.TimeSlotId] = engagement;
        }

        // Batch fetch all artists
        var artistIds = engagements
            .OfType<Domain.Entities.Engagement>()
            .Select(e => e.ArtistId)
            .ToHashSet();
        
        var artistTasks = artistIds.Select(id => _artistRepository.GetByIdAsync(id, ct)).ToArray();
        var artists = await Task.WhenAll(artistTasks);
        
        var artistDictionary = new Dictionary<Guid, Domain.Entities.Artist>();
        foreach (var artist in artists.Where(a => a != null))
        {
            artistDictionary[artist!.ArtistId] = artist;
        }

        // Build CSV lines
        foreach (var timeSlot in orderedTimeSlots)
        {
            stageDictionary.TryGetValue(timeSlot.StageId, out var stage);
            
            if (!engagementDictionary.TryGetValue(timeSlot.TimeSlotId, out var engagement))
            {
                continue;
            }

            artistDictionary.TryGetValue(engagement.ArtistId, out var artist);

            sb.AppendLine(
                $"{timeSlot.TimeSlotId},{timeSlot.StageId},{EscapeCsv(stage?.Name)},{timeSlot.StartTimeUtc:o},{timeSlot.EndTimeUtc:o},{engagement.ArtistId},{EscapeCsv(artist?.Name)}");
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

    /// <summary>
    /// Sanitizes a string for use in a filename by replacing invalid characters and appending a unique suffix.
    /// </summary>
    /// <param name="name">The string to sanitize.</param>
    /// <param name="suffix">Optional suffix to append (e.g., "_export", "_schedule").</param>
    /// <returns>A safe filename with invalid characters replaced and a unique timestamp suffix appended.</returns>
    private string SanitizeFilename(string name, string suffix = "")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return $"export{suffix}_{_dateTimeProvider.UtcNow:yyyyMMddHHmmss}";
        }

        // Use IndexOfAny for efficient character lookup
        var sanitized = string.Create(name.Length, name, (span, state) =>
        {
            state.AsSpan().CopyTo(span);
            
            // Replace invalid characters with underscore using optimized IndexOfAny
            int pos = 0;
            while (pos < span.Length)
            {
                var remaining = span.Slice(pos);
                int invalidIndex = remaining.IndexOfAny(InvalidFilenameChars);
                if (invalidIndex == -1)
                {
                    break;
                }
                span[pos + invalidIndex] = '_';
                pos += invalidIndex + 1;
            }
        });

        // Append suffix and timestamp to prevent filename collisions
        return $"{sanitized}{suffix}_{_dateTimeProvider.UtcNow:yyyyMMddHHmmss}";
    }
}

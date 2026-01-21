using NodaTime;
using NodaTime.TimeZones;

namespace FestGuide.Infrastructure.Timezone;

/// <summary>
/// NodaTime-based implementation of ITimezoneService for IANA timezone handling.
/// </summary>
public class NodaTimeTimezoneService : ITimezoneService
{
    private readonly IDateTimeZoneProvider _timezoneProvider;
    private readonly IClock _clock;
    private readonly HashSet<string> _validTimezoneIds;

    public NodaTimeTimezoneService()
        : this(DateTimeZoneProviders.Tzdb, SystemClock.Instance)
    {
    }

    public NodaTimeTimezoneService(IDateTimeZoneProvider timezoneProvider, IClock clock)
    {
        _timezoneProvider = timezoneProvider ?? throw new ArgumentNullException(nameof(timezoneProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _validTimezoneIds = new HashSet<string>(_timezoneProvider.Ids, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public DateTimeOffset ConvertFromUtc(DateTime utcDateTime, string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        var timezone = GetTimezone(timezoneId);
        var instant = Instant.FromDateTimeUtc(utcDateTime);
        var zonedDateTime = instant.InZone(timezone);

        return zonedDateTime.ToDateTimeOffset();
    }

    /// <inheritdoc />
    public DateTime ConvertToUtc(DateTime localDateTime, string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        var timezone = GetTimezone(timezoneId);
        var localDate = LocalDateTime.FromDateTime(localDateTime);

        // Use lenient resolver for ambiguous/skipped times
        var zonedDateTime = localDate.InZoneLeniently(timezone);

        return zonedDateTime.ToInstant().ToDateTimeUtc();
    }

    /// <inheritdoc />
    public bool IsValidTimezone(string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return false;
        }

        return _validTimezoneIds.Contains(timezoneId);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllTimezoneIds()
    {
        return _timezoneProvider.Ids.OrderBy(id => id);
    }

    /// <inheritdoc />
    public TimeSpan GetCurrentOffset(string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        var timezone = GetTimezone(timezoneId);
        var now = _clock.GetCurrentInstant();
        var offset = timezone.GetUtcOffset(now);

        return offset.ToTimeSpan();
    }

    /// <inheritdoc />
    public string FormatInTimezone(DateTime utcDateTime, string timezoneId, string format = "yyyy-MM-dd HH:mm zzz")
    {
        var dateTimeOffset = ConvertFromUtc(utcDateTime, timezoneId);
        return dateTimeOffset.ToString(format);
    }

    private DateTimeZone GetTimezone(string timezoneId)
    {
        var timezone = _timezoneProvider.GetZoneOrNull(timezoneId);

        if (timezone == null)
        {
            throw new ArgumentException($"Invalid timezone identifier: {timezoneId}", nameof(timezoneId));
        }

        return timezone;
    }
}

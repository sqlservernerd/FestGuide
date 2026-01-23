namespace FestConnect.Infrastructure.Timezone;

/// <summary>
/// Service interface for timezone operations using IANA timezone identifiers.
/// </summary>
public interface ITimezoneService
{
    /// <summary>
    /// Converts a UTC DateTime to a specific timezone.
    /// </summary>
    /// <param name="utcDateTime">The UTC datetime to convert.</param>
    /// <param name="timezoneId">The IANA timezone identifier (e.g., "America/New_York").</param>
    /// <returns>The converted DateTimeOffset in the specified timezone.</returns>
    DateTimeOffset ConvertFromUtc(DateTime utcDateTime, string timezoneId);

    /// <summary>
    /// Converts a local datetime in a specific timezone to UTC.
    /// </summary>
    /// <param name="localDateTime">The local datetime to convert.</param>
    /// <param name="timezoneId">The IANA timezone identifier.</param>
    /// <returns>The UTC DateTime.</returns>
    DateTime ConvertToUtc(DateTime localDateTime, string timezoneId);

    /// <summary>
    /// Validates whether a timezone identifier is valid (IANA format).
    /// </summary>
    /// <param name="timezoneId">The timezone identifier to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidTimezone(string timezoneId);

    /// <summary>
    /// Gets all available IANA timezone identifiers.
    /// </summary>
    /// <returns>Collection of valid IANA timezone identifiers.</returns>
    IEnumerable<string> GetAllTimezoneIds();

    /// <summary>
    /// Gets the current UTC offset for a timezone.
    /// </summary>
    /// <param name="timezoneId">The IANA timezone identifier.</param>
    /// <returns>The current UTC offset.</returns>
    TimeSpan GetCurrentOffset(string timezoneId);

    /// <summary>
    /// Formats a UTC datetime for display in a specific timezone.
    /// </summary>
    /// <param name="utcDateTime">The UTC datetime.</param>
    /// <param name="timezoneId">The IANA timezone identifier.</param>
    /// <param name="format">The format string (default: "yyyy-MM-dd HH:mm zzz").</param>
    /// <returns>Formatted datetime string.</returns>
    string FormatInTimezone(DateTime utcDateTime, string timezoneId, string format = "yyyy-MM-dd HH:mm zzz");
}

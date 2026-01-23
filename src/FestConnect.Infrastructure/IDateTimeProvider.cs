namespace FestConnect.Infrastructure;

/// <summary>
/// Provides the current date and time. Abstracted for testability.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }
}

/// <summary>
/// System clock implementation of IDateTimeProvider.
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}

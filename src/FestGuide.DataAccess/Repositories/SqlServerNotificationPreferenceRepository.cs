using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of INotificationPreferenceRepository using Dapper.
/// </summary>
public class SqlServerNotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly IDbConnection _connection;

    public SqlServerNotificationPreferenceRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<NotificationPreference?> GetByUserAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                NotificationPreferenceId, UserId, PushEnabled, EmailEnabled,
                ScheduleChangesEnabled, RemindersEnabled, ReminderMinutesBefore,
                AnnouncementsEnabled, QuietHoursStart, QuietHoursEnd, TimeZoneId,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.NotificationPreference
            WHERE UserId = @UserId
            """;

        return await _connection.QuerySingleOrDefaultAsync<NotificationPreference>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<long> UpsertAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        const string sql = """
            MERGE notifications.NotificationPreference AS target
            USING (SELECT @UserId AS UserId) AS source
            ON target.UserId = source.UserId
            WHEN MATCHED THEN
                UPDATE SET
                    PushEnabled = @PushEnabled,
                    EmailEnabled = @EmailEnabled,
                    ScheduleChangesEnabled = @ScheduleChangesEnabled,
                    RemindersEnabled = @RemindersEnabled,
                    ReminderMinutesBefore = @ReminderMinutesBefore,
                    AnnouncementsEnabled = @AnnouncementsEnabled,
                    QuietHoursStart = @QuietHoursStart,
                    QuietHoursEnd = @QuietHoursEnd,
                    TimeZoneId = @TimeZoneId,
                    ModifiedAtUtc = @ModifiedAtUtc,
                    ModifiedBy = @ModifiedBy
            WHEN NOT MATCHED THEN
                INSERT (
                    NotificationPreferenceId, UserId, PushEnabled, EmailEnabled,
                    ScheduleChangesEnabled, RemindersEnabled, ReminderMinutesBefore,
                    AnnouncementsEnabled, QuietHoursStart, QuietHoursEnd, TimeZoneId,
                    CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
                ) VALUES (
                    @NotificationPreferenceId, @UserId, @PushEnabled, @EmailEnabled,
                    @ScheduleChangesEnabled, @RemindersEnabled, @ReminderMinutesBefore,
                    @AnnouncementsEnabled, @QuietHoursStart, @QuietHoursEnd, @TimeZoneId,
                    @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
                );
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, preference, cancellationToken: ct)).ConfigureAwait(false);

        return preference.NotificationPreferenceId;
    }
}

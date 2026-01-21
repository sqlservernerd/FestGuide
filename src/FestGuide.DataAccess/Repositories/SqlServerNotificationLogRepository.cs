using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of INotificationLogRepository using Dapper.
/// </summary>
public class SqlServerNotificationLogRepository : INotificationLogRepository
{
    private readonly IDbConnection _connection;

    public SqlServerNotificationLogRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<NotificationLog?> GetByIdAsync(Guid notificationLogId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                NotificationLogId, UserId, DeviceTokenId, NotificationType,
                Title, Body, DataPayload, RelatedEntityType, RelatedEntityId,
                SentAtUtc, IsDelivered, ErrorMessage, ReadAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.NotificationLog
            WHERE NotificationLogId = @NotificationLogId
            """;

        return await _connection.QuerySingleOrDefaultAsync<NotificationLog>(
            new CommandDefinition(sql, new { NotificationLogId = notificationLogId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationLog>> GetByUserAsync(Guid userId, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                NotificationLogId, UserId, DeviceTokenId, NotificationType,
                Title, Body, DataPayload, RelatedEntityType, RelatedEntityId,
                SentAtUtc, IsDelivered, ErrorMessage, ReadAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.NotificationLog
            WHERE UserId = @UserId
            ORDER BY SentAtUtc DESC
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY
            """;

        var result = await _connection.QueryAsync<NotificationLog>(
            new CommandDefinition(sql, new { UserId = userId, Limit = limit, Offset = offset }, cancellationToken: ct)).ConfigureAwait(false);

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationLog>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                NotificationLogId, UserId, DeviceTokenId, NotificationType,
                Title, Body, DataPayload, RelatedEntityType, RelatedEntityId,
                SentAtUtc, IsDelivered, ErrorMessage, ReadAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.NotificationLog
            WHERE UserId = @UserId AND ReadAtUtc IS NULL
            ORDER BY SentAtUtc DESC
            """;

        var result = await _connection.QueryAsync<NotificationLog>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM notifications.NotificationLog
            WHERE UserId = @UserId AND ReadAtUtc IS NULL
            """;

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(NotificationLog notificationLog, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO notifications.NotificationLog (
                NotificationLogId, UserId, DeviceTokenId, NotificationType,
                Title, Body, DataPayload, RelatedEntityType, RelatedEntityId,
                SentAtUtc, IsDelivered, ErrorMessage, ReadAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @NotificationLogId, @UserId, @DeviceTokenId, @NotificationType,
                @Title, @Body, @DataPayload, @RelatedEntityType, @RelatedEntityId,
                @SentAtUtc, @IsDelivered, @ErrorMessage, @ReadAtUtc,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, notificationLog, cancellationToken: ct)).ConfigureAwait(false);

        return notificationLog.NotificationLogId;
    }

    /// <inheritdoc />
    public async Task CreateBatchAsync(IEnumerable<NotificationLog> notificationLogs, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO notifications.NotificationLog (
                NotificationLogId, UserId, DeviceTokenId, NotificationType,
                Title, Body, DataPayload, RelatedEntityType, RelatedEntityId,
                SentAtUtc, IsDelivered, ErrorMessage, ReadAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @NotificationLogId, @UserId, @DeviceTokenId, @NotificationType,
                @Title, @Body, @DataPayload, @RelatedEntityType, @RelatedEntityId,
                @SentAtUtc, @IsDelivered, @ErrorMessage, @ReadAtUtc,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, notificationLogs, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid notificationLogId, Guid modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.NotificationLog SET
                ReadAtUtc = GETUTCDATE(),
                ModifiedAtUtc = GETUTCDATE(),
                ModifiedBy = @ModifiedBy
            WHERE NotificationLogId = @NotificationLogId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { NotificationLogId = notificationLogId, ModifiedBy = modifiedBy }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid userId, Guid modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.NotificationLog SET
                ReadAtUtc = GETUTCDATE(),
                ModifiedAtUtc = GETUTCDATE(),
                ModifiedBy = @ModifiedBy
            WHERE UserId = @UserId AND ReadAtUtc IS NULL
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, ModifiedBy = modifiedBy }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateDeliveryStatusAsync(Guid notificationLogId, bool isDelivered, string? errorMessage, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.NotificationLog SET
                IsDelivered = @IsDelivered,
                ErrorMessage = @ErrorMessage,
                ModifiedAtUtc = GETUTCDATE()
            WHERE NotificationLogId = @NotificationLogId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { NotificationLogId = notificationLogId, IsDelivered = isDelivered, ErrorMessage = errorMessage }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CleanupOldLogsAsync(int daysToKeep, CancellationToken ct = default)
    {
        const string sql = """
            DELETE FROM notifications.NotificationLog
            WHERE SentAtUtc < DATEADD(DAY, -@DaysToKeep, GETUTCDATE())
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { DaysToKeep = daysToKeep }, cancellationToken: ct)).ConfigureAwait(false);
    }
}

using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IDeviceTokenRepository using Dapper.
/// </summary>
public class SqlServerDeviceTokenRepository : IDeviceTokenRepository
{
    // SQL Server has a hard limit of 2100 parameters per statement. We cap at 2000 to leave
    // headroom for additional parameters (e.g., non-batched WHERE clauses, framework/Dapper
    // parameters, or future query changes) and avoid hitting the hard limit.
    private const int MaxSqlParameterCount = 2000;

    private readonly IDbConnection _connection;

    public SqlServerDeviceTokenRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<DeviceToken?> GetByIdAsync(Guid deviceTokenId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                DeviceTokenId, UserId, Token, Platform, DeviceName,
                IsActive, LastUsedAtUtc, ExpiresAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.DeviceToken
            WHERE DeviceTokenId = @DeviceTokenId AND IsActive = 1
            """;

        return await _connection.QuerySingleOrDefaultAsync<DeviceToken>(
            new CommandDefinition(sql, new { DeviceTokenId = deviceTokenId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                DeviceTokenId, UserId, Token, Platform, DeviceName,
                IsActive, LastUsedAtUtc, ExpiresAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.DeviceToken
            WHERE Token = @Token AND IsActive = 1
            """;

        return await _connection.QuerySingleOrDefaultAsync<DeviceToken>(
            new CommandDefinition(sql, new { Token = token }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceToken>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                DeviceTokenId, UserId, Token, Platform, DeviceName,
                IsActive, LastUsedAtUtc, ExpiresAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM notifications.DeviceToken
            WHERE UserId = @UserId AND IsActive = 1
            ORDER BY LastUsedAtUtc DESC
            """;

        var result = await _connection.QueryAsync<DeviceToken>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)).ConfigureAwait(false);

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceToken>> GetByUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var userIdArray = userIds.ToArray();
        if (userIdArray.Length == 0)
        {
            return Array.Empty<DeviceToken>();
        }

        // Handle large user sets by batching to avoid SQL Server parameter limits
        var results = new List<DeviceToken>();

        for (int i = 0; i < userIdArray.Length; i += MaxSqlParameterCount)
        {
            var batch = userIdArray.Skip(i).Take(MaxSqlParameterCount).ToArray();

            const string sql = """
                SELECT 
                    DeviceTokenId, UserId, Token, Platform, DeviceName,
                    IsActive, LastUsedAtUtc, ExpiresAtUtc,
                    CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
                FROM notifications.DeviceToken
                WHERE UserId IN @UserIds AND IsActive = 1
                """;

            var batchResult = await _connection.QueryAsync<DeviceToken>(
                new CommandDefinition(sql, new { UserIds = batch }, cancellationToken: ct)).ConfigureAwait(false);

            results.AddRange(batchResult);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<Guid> UpsertAsync(DeviceToken deviceToken, CancellationToken ct = default)
    {
        const string sql = """
            MERGE notifications.DeviceToken AS target
            USING (SELECT @Token AS Token) AS source
            ON target.Token = source.Token
            WHEN MATCHED THEN
                UPDATE SET
                    UserId = @UserId,
                    Platform = @Platform,
                    DeviceName = @DeviceName,
                    IsActive = 1,
                    LastUsedAtUtc = @LastUsedAtUtc,
                    ExpiresAtUtc = @ExpiresAtUtc,
                    ModifiedAtUtc = @ModifiedAtUtc,
                    ModifiedBy = @ModifiedBy
            WHEN NOT MATCHED THEN
                INSERT (
                    DeviceTokenId, UserId, Token, Platform, DeviceName,
                    IsActive, LastUsedAtUtc, ExpiresAtUtc,
                    CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
                ) VALUES (
                    @DeviceTokenId, @UserId, @Token, @Platform, @DeviceName,
                    @IsActive, @LastUsedAtUtc, @ExpiresAtUtc,
                    @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
                );
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, deviceToken, cancellationToken: ct)).ConfigureAwait(false);

        return deviceToken.DeviceTokenId;
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(Guid deviceTokenId, Guid modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.DeviceToken SET
                IsActive = 0,
                ModifiedAtUtc = GETUTCDATE(),
                ModifiedBy = @ModifiedBy
            WHERE DeviceTokenId = @DeviceTokenId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { DeviceTokenId = deviceTokenId, ModifiedBy = modifiedBy }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeactivateByTokenAsync(string token, Guid modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.DeviceToken SET
                IsActive = 0,
                ModifiedAtUtc = GETUTCDATE(),
                ModifiedBy = @ModifiedBy
            WHERE Token = @Token
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Token = token, ModifiedBy = modifiedBy }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeactivateAllForUserAsync(Guid userId, Guid modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.DeviceToken SET
                IsActive = 0,
                ModifiedAtUtc = GETUTCDATE(),
                ModifiedBy = @ModifiedBy
            WHERE UserId = @UserId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, ModifiedBy = modifiedBy }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateLastUsedAsync(Guid deviceTokenId, DateTime lastUsedAtUtc, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.DeviceToken SET
                LastUsedAtUtc = @LastUsedAtUtc
            WHERE DeviceTokenId = @DeviceTokenId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { DeviceTokenId = deviceTokenId, LastUsedAtUtc = lastUsedAtUtc }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        const string sql = """
            UPDATE notifications.DeviceToken SET
                IsActive = 0,
                ModifiedAtUtc = GETUTCDATE()
            WHERE ExpiresAtUtc < GETUTCDATE() AND IsActive = 1
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
    }
}

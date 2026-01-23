using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IPasswordResetTokenRepository using Dapper.
/// </summary>
public class SqlServerPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IDbConnection _connection;

    public SqlServerPasswordResetTokenRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO identity.PasswordResetToken (
                UserId, TokenHash, ExpiresAtUtc, IsUsed,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @UserId, @TokenHash, @ExpiresAtUtc, @IsUsed,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, token, cancellationToken: ct));

        return token.TokenId;
    }

    /// <inheritdoc />
    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                TokenId, UserId, TokenHash, ExpiresAtUtc, IsUsed,
                UsedAtUtc, CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM identity.PasswordResetToken
            WHERE TokenHash = @TokenHash
            """;

        return await _connection.QuerySingleOrDefaultAsync<PasswordResetToken>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<PasswordResetToken?> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                TokenId, UserId, TokenHash, ExpiresAtUtc, IsUsed,
                UsedAtUtc, CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM identity.PasswordResetToken
            WHERE UserId = @UserId 
              AND IsUsed = 0 
              AND ExpiresAtUtc > @Now
            ORDER BY CreatedAtUtc DESC
            """;

        return await _connection.QuerySingleOrDefaultAsync<PasswordResetToken>(
            new CommandDefinition(sql, new { UserId = userId, Now = DateTime.UtcNow }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task MarkAsUsedAsync(long tokenId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.PasswordResetToken
            SET IsUsed = 1,
                UsedAtUtc = @UsedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE TokenId = @TokenId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { TokenId = tokenId, UsedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.PasswordResetToken
            SET IsUsed = 1,
                UsedAtUtc = @UsedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE UserId = @UserId AND IsUsed = 0
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, UsedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteExpiredAsync(DateTime olderThan, CancellationToken ct = default)
    {
        const string sql = """
            DELETE FROM identity.PasswordResetToken
            WHERE ExpiresAtUtc < @OlderThan
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { OlderThan = olderThan },
            cancellationToken: ct));
    }
}

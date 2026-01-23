using System.Data;
using Dapper;
using FestConnect.DataAccess.Abstractions;
using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IEmailVerificationTokenRepository using Dapper.
/// </summary>
public class SqlServerEmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly IDbConnection _connection;

    public SqlServerEmailVerificationTokenRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(EmailVerificationToken token, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO identity.EmailVerificationToken (
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
    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                TokenId, UserId, TokenHash, ExpiresAtUtc, IsUsed,
                UsedAtUtc, CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM identity.EmailVerificationToken
            WHERE TokenHash = @TokenHash
            """;

        return await _connection.QuerySingleOrDefaultAsync<EmailVerificationToken>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<EmailVerificationToken?> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                TokenId, UserId, TokenHash, ExpiresAtUtc, IsUsed,
                UsedAtUtc, CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM identity.EmailVerificationToken
            WHERE UserId = @UserId 
              AND IsUsed = 0 
              AND ExpiresAtUtc > @Now
            ORDER BY CreatedAtUtc DESC
            """;

        return await _connection.QuerySingleOrDefaultAsync<EmailVerificationToken>(
            new CommandDefinition(sql, new { UserId = userId, Now = DateTime.UtcNow }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task MarkAsUsedAsync(long tokenId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.EmailVerificationToken
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
            UPDATE identity.EmailVerificationToken
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
            DELETE FROM identity.EmailVerificationToken
            WHERE ExpiresAtUtc < @OlderThan
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { OlderThan = olderThan },
            cancellationToken: ct));
    }
}

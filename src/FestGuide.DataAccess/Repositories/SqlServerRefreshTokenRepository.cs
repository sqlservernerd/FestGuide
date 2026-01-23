using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IRefreshTokenRepository using Dapper.
/// </summary>
public class SqlServerRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnection _connection;

    public SqlServerRefreshTokenRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                RefreshTokenId, UserId, TokenHash, ExpiresAtUtc, IsRevoked,
                RevokedAtUtc, ReplacedByTokenId, CreatedAtUtc, CreatedByIp
            FROM identity.RefreshToken
            WHERE TokenHash = @TokenHash
            """;

        return await _connection.QuerySingleOrDefaultAsync<RefreshToken>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                RefreshTokenId, UserId, TokenHash, ExpiresAtUtc, IsRevoked,
                RevokedAtUtc, ReplacedByTokenId, CreatedAtUtc, CreatedByIp
            FROM identity.RefreshToken
            WHERE UserId = @UserId 
              AND IsRevoked = 0 
              AND ExpiresAtUtc > @Now
            """;

        return await _connection.QueryAsync<RefreshToken>(
            new CommandDefinition(sql, new { UserId = userId, Now = DateTime.UtcNow }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(RefreshToken token, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO identity.RefreshToken (
                RefreshTokenId, UserId, TokenHash, ExpiresAtUtc, IsRevoked,
                CreatedAtUtc, CreatedByIp
            ) VALUES (
                @RefreshTokenId, @UserId, @TokenHash, @ExpiresAtUtc, @IsRevoked,
                @CreatedAtUtc, @CreatedByIp
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, token, cancellationToken: ct));

        return token.RefreshTokenId;
    }

    /// <inheritdoc />
    public async Task RevokeAsync(long tokenId, long? replacedByTokenId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.RefreshToken
            SET IsRevoked = 1,
                RevokedAtUtc = @RevokedAtUtc,
                ReplacedByTokenId = @ReplacedByTokenId
            WHERE RefreshTokenId = @TokenId
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { TokenId = tokenId, RevokedAtUtc = DateTime.UtcNow, ReplacedByTokenId = replacedByTokenId }, 
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.RefreshToken
            SET IsRevoked = 1,
                RevokedAtUtc = @RevokedAtUtc
            WHERE UserId = @UserId AND IsRevoked = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { UserId = userId, RevokedAtUtc = DateTime.UtcNow }, 
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RemoveExpiredAsync(CancellationToken ct = default)
    {
        const string sql = """
            DELETE FROM identity.RefreshToken
            WHERE ExpiresAtUtc < @Now
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { Now = DateTime.UtcNow }, 
            cancellationToken: ct));
    }
}

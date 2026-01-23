using System.Data;
using Dapper;
using FestConnect.DataAccess.Abstractions;
using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IUserRepository using Dapper.
/// </summary>
public class SqlServerUserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public SqlServerUserRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                UserId, Email, EmailNormalized, EmailVerified, PasswordHash,
                DisplayName, UserType, PreferredTimezoneId, IsDeleted, DeletedAtUtc,
                FailedLoginAttempts, LockoutEndUtc, CreatedAtUtc, CreatedBy,
                ModifiedAtUtc, ModifiedBy
            FROM identity.[User]
            WHERE UserId = @UserId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                UserId, Email, EmailNormalized, EmailVerified, PasswordHash,
                DisplayName, UserType, PreferredTimezoneId, IsDeleted, DeletedAtUtc,
                FailedLoginAttempts, LockoutEndUtc, CreatedAtUtc, CreatedBy,
                ModifiedAtUtc, ModifiedBy
            FROM identity.[User]
            WHERE EmailNormalized = @EmailNormalized AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { EmailNormalized = email.ToLowerInvariant() }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<long> userIds, CancellationToken ct = default)
    {
        var userIdsList = userIds?.ToList();
        if (userIdsList == null || !userIdsList.Any())
        {
            return Array.Empty<User>();
        }

        const string sql = """
            SELECT 
                UserId, Email, EmailNormalized, EmailVerified, PasswordHash,
                DisplayName, UserType, PreferredTimezoneId, IsDeleted, DeletedAtUtc,
                FailedLoginAttempts, LockoutEndUtc, CreatedAtUtc, CreatedBy,
                ModifiedAtUtc, ModifiedBy
            FROM identity.[User]
            WHERE UserId IN @UserIds AND IsDeleted = 0
            """;

        var users = await _connection.QueryAsync<User>(
            new CommandDefinition(sql, new { UserIds = userIdsList }, cancellationToken: ct));

        return users.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM identity.[User]
            WHERE EmailNormalized = @EmailNormalized AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EmailNormalized = email.ToLowerInvariant() }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(User user, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO identity.[User] (
                Email, EmailNormalized, EmailVerified, PasswordHash,
                DisplayName, UserType, PreferredTimezoneId, IsDeleted,
                FailedLoginAttempts, CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @Email, @EmailNormalized, @EmailVerified, @PasswordHash,
                @DisplayName, @UserType, @PreferredTimezoneId, @IsDeleted,
                @FailedLoginAttempts, @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, user, cancellationToken: ct));

        return user.UserId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.[User]
            SET DisplayName = @DisplayName,
                PreferredTimezoneId = @PreferredTimezoneId,
                EmailVerified = @EmailVerified,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE UserId = @UserId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, user, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.[User]
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE UserId = @UserId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { UserId = userId, DeletedAtUtc = now, ModifiedAtUtc = now }, 
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateLoginAttemptsAsync(long userId, int failedAttempts, DateTime? lockoutEndUtc, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.[User]
            SET FailedLoginAttempts = @FailedAttempts,
                LockoutEndUtc = @LockoutEndUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE UserId = @UserId
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { UserId = userId, FailedAttempts = failedAttempts, LockoutEndUtc = lockoutEndUtc, ModifiedAtUtc = DateTime.UtcNow }, 
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task ResetLoginAttemptsAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE identity.[User]
            SET FailedLoginAttempts = 0,
                LockoutEndUtc = NULL,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE UserId = @UserId
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { UserId = userId, ModifiedAtUtc = DateTime.UtcNow }, 
            cancellationToken: ct));
    }
}

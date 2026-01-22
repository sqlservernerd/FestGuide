using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IFestivalPermissionRepository using Dapper.
/// </summary>
public class SqlServerFestivalPermissionRepository : IFestivalPermissionRepository
{
    private readonly IDbConnection _connection;

    public SqlServerFestivalPermissionRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<FestivalPermission?> GetByIdAsync(Guid permissionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked, RevokedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM permissions.FestivalPermission
            WHERE FestivalPermissionId = @PermissionId
            """;

        return await _connection.QuerySingleOrDefaultAsync<FestivalPermission>(
            new CommandDefinition(sql, new { PermissionId = permissionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<FestivalPermission?> GetByUserAndFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked, RevokedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM permissions.FestivalPermission
            WHERE UserId = @UserId AND FestivalId = @FestivalId AND IsRevoked = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<FestivalPermission>(
            new CommandDefinition(sql, new { UserId = userId, FestivalId = festivalId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FestivalPermission>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked, RevokedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM permissions.FestivalPermission
            WHERE FestivalId = @FestivalId
            ORDER BY Role DESC, CreatedAtUtc
            """;

        return await _connection.QueryAsync<FestivalPermission>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FestivalPermission>> GetActiveByFestivalAsync(Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked, RevokedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM permissions.FestivalPermission
            WHERE FestivalId = @FestivalId 
              AND IsRevoked = 0 
              AND IsPending = 0
            ORDER BY Role DESC, CreatedAtUtc
            """;

        return await _connection.QueryAsync<FestivalPermission>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FestivalPermission>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked, RevokedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM permissions.FestivalPermission
            WHERE UserId = @UserId AND IsRevoked = 0
            ORDER BY CreatedAtUtc DESC
            """;

        return await _connection.QueryAsync<FestivalPermission>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<FestivalPermission?> GetOwnerAsync(Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked, RevokedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM permissions.FestivalPermission
            WHERE FestivalId = @FestivalId AND Role = @OwnerRole AND IsRevoked = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<FestivalPermission>(
            new CommandDefinition(sql, new { FestivalId = festivalId, OwnerRole = (int)FestivalRole.Owner }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyPermissionAsync(Guid userId, Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM permissions.FestivalPermission
            WHERE UserId = @UserId 
              AND FestivalId = @FestivalId 
              AND IsRevoked = 0 
              AND IsPending = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId, FestivalId = festivalId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<bool> HasRoleOrHigherAsync(Guid userId, Guid festivalId, FestivalRole minimumRole, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM permissions.FestivalPermission
            WHERE UserId = @UserId 
              AND FestivalId = @FestivalId 
              AND Role >= @MinimumRole
              AND IsRevoked = 0 
              AND IsPending = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId, FestivalId = festivalId, MinimumRole = (int)minimumRole }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<bool> HasScopeAsync(Guid userId, Guid festivalId, PermissionScope scope, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM permissions.FestivalPermission
            WHERE UserId = @UserId 
              AND FestivalId = @FestivalId 
              AND (Role >= @AdminRole OR Scope = @AllScope OR Scope = @RequestedScope)
              AND IsRevoked = 0 
              AND IsPending = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new 
            { 
                UserId = userId, 
                FestivalId = festivalId, 
                AdminRole = (int)FestivalRole.Administrator,
                AllScope = (int)PermissionScope.All,
                RequestedScope = (int)scope 
            }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(FestivalPermission permission, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO permissions.FestivalPermission (
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @FestivalPermissionId, @FestivalId, @UserId, @Role, @Scope,
                @InvitedByUserId, @AcceptedAtUtc, @IsPending, @IsRevoked,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, permission, cancellationToken: ct));

        return permission.FestivalPermissionId;
    }
    
    /// <inheritdoc />
    public async Task<Guid> CreateAsync(FestivalPermission permission, ITransactionScope transactionScope, CancellationToken ct = default)
    {
        if (transactionScope == null)
        {
            throw new ArgumentNullException(nameof(transactionScope));
        }
        
        const string sql = """
            INSERT INTO permissions.FestivalPermission (
                FestivalPermissionId, FestivalId, UserId, Role, Scope,
                InvitedByUserId, AcceptedAtUtc, IsPending, IsRevoked,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @FestivalPermissionId, @FestivalId, @UserId, @Role, @Scope,
                @InvitedByUserId, @AcceptedAtUtc, @IsPending, @IsRevoked,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, permission, transaction: transactionScope.Transaction, cancellationToken: ct));

        return permission.FestivalPermissionId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FestivalPermission permission, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE permissions.FestivalPermission
            SET Role = @Role,
                Scope = @Scope,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE FestivalPermissionId = @FestivalPermissionId
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, permission, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RevokeAsync(Guid permissionId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE permissions.FestivalPermission
            SET IsRevoked = 1,
                RevokedAtUtc = @RevokedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE FestivalPermissionId = @PermissionId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { PermissionId = permissionId, RevokedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task TransferOwnershipAsync(Guid festivalId, Guid fromUserId, Guid toUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Demote current owner to Administrator
        const string demoteSql = """
            UPDATE permissions.FestivalPermission
            SET Role = @AdminRole,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE FestivalId = @FestivalId 
              AND UserId = @FromUserId 
              AND Role = @OwnerRole
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            demoteSql,
            new { FestivalId = festivalId, FromUserId = fromUserId, OwnerRole = (int)FestivalRole.Owner, AdminRole = (int)FestivalRole.Administrator, ModifiedAtUtc = now },
            cancellationToken: ct));

        // Promote new owner
        const string promoteSql = """
            UPDATE permissions.FestivalPermission
            SET Role = @OwnerRole,
                Scope = @AllScope,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE FestivalId = @FestivalId 
              AND UserId = @ToUserId
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            promoteSql,
            new { FestivalId = festivalId, ToUserId = toUserId, OwnerRole = (int)FestivalRole.Owner, AllScope = (int)PermissionScope.All, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task AcceptInvitationAsync(Guid permissionId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE permissions.FestivalPermission
            SET IsPending = 0,
                AcceptedAtUtc = @AcceptedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE FestivalPermissionId = @PermissionId AND IsPending = 1
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { PermissionId = permissionId, AcceptedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeclineInvitationAsync(Guid permissionId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE permissions.FestivalPermission
            SET IsRevoked = 1,
                RevokedAtUtc = @RevokedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc
            WHERE FestivalPermissionId = @PermissionId AND IsPending = 1
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { PermissionId = permissionId, RevokedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }
}

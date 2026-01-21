using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IFestivalRepository using Dapper.
/// </summary>
public class SqlServerFestivalRepository : IFestivalRepository
{
    private readonly IDbConnection _connection;

    public SqlServerFestivalRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Festival?> GetByIdAsync(Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalId, Name, Description, ImageUrl, WebsiteUrl,
                OwnerUserId, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Festival
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Festival>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Festival>> GetByIdsAsync(IEnumerable<Guid> festivalIds, CancellationToken ct = default)
    {
        var festivalIdsList = festivalIds?.ToList();
        if (festivalIdsList == null || !festivalIdsList.Any())
        {
            return Array.Empty<Festival>();
        }

        const string sql = """
            SELECT 
                FestivalId, Name, Description, ImageUrl, WebsiteUrl,
                OwnerUserId, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Festival
            WHERE FestivalId IN @FestivalIds AND IsDeleted = 0
            """;

        var festivals = await _connection.QueryAsync<Festival>(
            new CommandDefinition(sql, new { FestivalIds = festivalIdsList }, cancellationToken: ct));

        return festivals.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Festival>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                FestivalId, Name, Description, ImageUrl, WebsiteUrl,
                OwnerUserId, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Festival
            WHERE OwnerUserId = @OwnerUserId AND IsDeleted = 0
            ORDER BY Name
            """;

        var result = await _connection.QueryAsync<Festival>(
            new CommandDefinition(sql, new { OwnerUserId = ownerUserId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Festival>> GetByUserAccessAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT f.FestivalId, f.Name, f.Description, f.ImageUrl, f.WebsiteUrl,
                f.OwnerUserId, f.IsDeleted, f.DeletedAtUtc,
                f.CreatedAtUtc, f.CreatedBy, f.ModifiedAtUtc, f.ModifiedBy
            FROM core.Festival f
            LEFT JOIN permissions.FestivalPermission fp ON f.FestivalId = fp.FestivalId
            WHERE f.IsDeleted = 0 
                AND (f.OwnerUserId = @UserId OR (fp.UserId = @UserId AND fp.IsRevoked = 0))
            ORDER BY f.Name
            """;

        var result = await _connection.QueryAsync<Festival>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Festival>> SearchByNameAsync(string searchTerm, int limit = 20, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Limit)
                FestivalId, Name, Description, ImageUrl, WebsiteUrl,
                OwnerUserId, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Festival
            WHERE IsDeleted = 0 AND Name LIKE @SearchTerm
            ORDER BY Name
            """;

        var result = await _connection.QueryAsync<Festival>(
            new CommandDefinition(sql, new { SearchTerm = $"%{searchTerm}%", Limit = limit }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(Festival festival, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO core.Festival (
                FestivalId, Name, Description, ImageUrl, WebsiteUrl,
                OwnerUserId, IsDeleted, CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @FestivalId, @Name, @Description, @ImageUrl, @WebsiteUrl,
                @OwnerUserId, @IsDeleted, @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, festival, cancellationToken: ct));

        return festival.FestivalId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Festival festival, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.Festival
            SET Name = @Name,
                Description = @Description,
                ImageUrl = @ImageUrl,
                WebsiteUrl = @WebsiteUrl,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, festival, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid festivalId, Guid deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.Festival
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE FestivalId = @FestivalId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { FestivalId = festivalId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM core.Festival
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task TransferOwnershipAsync(Guid festivalId, Guid newOwnerUserId, Guid modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.Festival
            SET OwnerUserId = @NewOwnerUserId,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { FestivalId = festivalId, NewOwnerUserId = newOwnerUserId, ModifiedBy = modifiedBy, ModifiedAtUtc = DateTime.UtcNow },
            cancellationToken: ct));
    }
}

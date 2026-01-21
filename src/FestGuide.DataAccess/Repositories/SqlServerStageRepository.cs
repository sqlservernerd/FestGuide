using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IStageRepository using Dapper.
/// </summary>
public class SqlServerStageRepository : IStageRepository
{
    private readonly IDbConnection _connection;

    public SqlServerStageRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Stage?> GetByIdAsync(Guid stageId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                StageId, VenueId, Name, Description, SortOrder,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.Stage
            WHERE StageId = @StageId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Stage>(
            new CommandDefinition(sql, new { StageId = stageId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Stage>> GetByVenueAsync(Guid venueId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                StageId, VenueId, Name, Description, SortOrder,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.Stage
            WHERE VenueId = @VenueId AND IsDeleted = 0
            ORDER BY SortOrder, Name
            """;

        var result = await _connection.QueryAsync<Stage>(
            new CommandDefinition(sql, new { VenueId = venueId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Stage>> GetByEditionAsync(Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                s.StageId, s.VenueId, s.Name, s.Description, s.SortOrder,
                s.IsDeleted, s.DeletedAtUtc,
                s.CreatedAtUtc, s.CreatedBy, s.ModifiedAtUtc, s.ModifiedBy
            FROM venue.Stage s
            INNER JOIN venue.Venue v ON s.VenueId = v.VenueId
            INNER JOIN venue.EditionVenue ev ON v.VenueId = ev.VenueId
            WHERE ev.EditionId = @EditionId AND s.IsDeleted = 0 AND v.IsDeleted = 0
            ORDER BY v.Name, s.SortOrder, s.Name
            """;

        var result = await _connection.QueryAsync<Stage>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(Stage stage, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO venue.Stage (
                StageId, VenueId, Name, Description, SortOrder, IsDeleted,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @StageId, @VenueId, @Name, @Description, @SortOrder, @IsDeleted,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, stage, cancellationToken: ct));

        return stage.StageId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Stage stage, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE venue.Stage
            SET Name = @Name,
                Description = @Description,
                SortOrder = @SortOrder,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE StageId = @StageId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, stage, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid stageId, Guid deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE venue.Stage
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE StageId = @StageId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { StageId = stageId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid stageId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM venue.Stage
            WHERE StageId = @StageId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StageId = stageId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<Guid?> GetVenueIdAsync(Guid stageId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT VenueId FROM venue.Stage
            WHERE StageId = @StageId AND IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { StageId = stageId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<Guid?> GetFestivalIdAsync(Guid stageId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT v.FestivalId 
            FROM venue.Stage s
            INNER JOIN venue.Venue v ON s.VenueId = v.VenueId
            WHERE s.StageId = @StageId AND s.IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { StageId = stageId }, cancellationToken: ct));
    }
}

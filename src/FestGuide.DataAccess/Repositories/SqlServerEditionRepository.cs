using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IEditionRepository using Dapper.
/// </summary>
public class SqlServerEditionRepository : IEditionRepository
{
    private readonly IDbConnection _connection;

    public SqlServerEditionRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<FestivalEdition?> GetByIdAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EditionId, FestivalId, Name, StartDateUtc, EndDateUtc,
                TimezoneId, TicketUrl, Status, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.FestivalEdition
            WHERE EditionId = @EditionId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<FestivalEdition>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FestivalEdition>> GetByIdsAsync(IEnumerable<long> editionIds, CancellationToken ct = default)
    {
        var editionIdsList = editionIds?.ToList();
        if (editionIdsList == null || !editionIdsList.Any())
        {
            return Array.Empty<FestivalEdition>();
        }

        const string sql = """
            SELECT 
                EditionId, FestivalId, Name, StartDateUtc, EndDateUtc,
                TimezoneId, TicketUrl, Status, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.FestivalEdition
            WHERE EditionId IN @EditionIds AND IsDeleted = 0
            """;

        var result = await _connection.QueryAsync<FestivalEdition>(
            new CommandDefinition(sql, new { EditionIds = editionIdsList }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FestivalEdition>> GetByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EditionId, FestivalId, Name, StartDateUtc, EndDateUtc,
                TimezoneId, TicketUrl, Status, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.FestivalEdition
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            ORDER BY StartDateUtc DESC
            """;

        var result = await _connection.QueryAsync<FestivalEdition>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FestivalEdition>> GetPublishedByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EditionId, FestivalId, Name, StartDateUtc, EndDateUtc,
                TimezoneId, TicketUrl, Status, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.FestivalEdition
            WHERE FestivalId = @FestivalId AND IsDeleted = 0 AND Status = @Status
            ORDER BY StartDateUtc DESC
            """;

        var result = await _connection.QueryAsync<FestivalEdition>(
            new CommandDefinition(sql, new { FestivalId = festivalId, Status = EditionStatus.Published.ToString().ToLowerInvariant() }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FestivalEdition>> GetCurrentAndRecentAsync(long festivalId, int archiveMonths = 3, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EditionId, FestivalId, Name, StartDateUtc, EndDateUtc,
                TimezoneId, TicketUrl, Status, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.FestivalEdition
            WHERE FestivalId = @FestivalId 
                AND IsDeleted = 0 
                AND Status IN ('published', 'archived')
                AND EndDateUtc >= @CutoffDate
            ORDER BY StartDateUtc DESC
            """;

        var cutoffDate = DateTime.UtcNow.AddMonths(-archiveMonths);
        var result = await _connection.QueryAsync<FestivalEdition>(
            new CommandDefinition(sql, new { FestivalId = festivalId, CutoffDate = cutoffDate }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(FestivalEdition edition, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO core.FestivalEdition (
                FestivalId, Name, StartDateUtc, EndDateUtc,
                TimezoneId, TicketUrl, Status, IsDeleted,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @FestivalId, @Name, @StartDateUtc, @EndDateUtc,
                @TimezoneId, @TicketUrl, @Status, @IsDeleted,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            edition.FestivalId,
            edition.Name,
            edition.StartDateUtc,
            edition.EndDateUtc,
            edition.TimezoneId,
            edition.TicketUrl,
            Status = edition.Status.ToString().ToLowerInvariant(),
            edition.IsDeleted,
            edition.CreatedAtUtc,
            edition.CreatedBy,
            edition.ModifiedAtUtc,
            edition.ModifiedBy
        }, cancellationToken: ct));

        return edition.EditionId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FestivalEdition edition, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.FestivalEdition
            SET Name = @Name,
                StartDateUtc = @StartDateUtc,
                EndDateUtc = @EndDateUtc,
                TimezoneId = @TimezoneId,
                TicketUrl = @TicketUrl,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE EditionId = @EditionId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, edition, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(long editionId, EditionStatus status, long modifiedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.FestivalEdition
            SET Status = @Status,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE EditionId = @EditionId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EditionId = editionId, Status = status.ToString().ToLowerInvariant(), ModifiedBy = modifiedBy, ModifiedAtUtc = DateTime.UtcNow },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long editionId, long deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.FestivalEdition
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE EditionId = @EditionId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EditionId = editionId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM core.FestivalEdition
            WHERE EditionId = @EditionId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<long?> GetFestivalIdAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT FestivalId FROM core.FestivalEdition
            WHERE EditionId = @EditionId AND IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));
    }
}

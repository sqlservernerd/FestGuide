using System.Data;
using Dapper;
using FestConnect.DataAccess.Abstractions;
using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IEngagementRepository using Dapper.
/// </summary>
public class SqlServerEngagementRepository : IEngagementRepository
{
    private readonly IDbConnection _connection;

    public SqlServerEngagementRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Engagement?> GetByIdAsync(long engagementId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EngagementId, TimeSlotId, ArtistId, Notes,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM schedule.Engagement
            WHERE EngagementId = @EngagementId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Engagement>(
            new CommandDefinition(sql, new { EngagementId = engagementId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Engagement>> GetByIdsAsync(IEnumerable<long> engagementIds, CancellationToken ct = default)
    {
        var engagementIdsList = engagementIds?.ToList();
        if (engagementIdsList == null || !engagementIdsList.Any())
        {
            return Array.Empty<Engagement>();
        }

        const string sql = """
            SELECT 
                EngagementId, TimeSlotId, ArtistId, Notes,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM schedule.Engagement
            WHERE EngagementId IN @EngagementIds AND IsDeleted = 0
            """;

        var result = await _connection.QueryAsync<Engagement>(
            new CommandDefinition(sql, new { EngagementIds = engagementIdsList }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<Engagement?> GetByTimeSlotAsync(long timeSlotId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EngagementId, TimeSlotId, ArtistId, Notes,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM schedule.Engagement
            WHERE TimeSlotId = @TimeSlotId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Engagement>(
            new CommandDefinition(sql, new { TimeSlotId = timeSlotId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Engagement>> GetByEditionAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                e.EngagementId, e.TimeSlotId, e.ArtistId, e.Notes,
                e.IsDeleted, e.DeletedAtUtc,
                e.CreatedAtUtc, e.CreatedBy, e.ModifiedAtUtc, e.ModifiedBy
            FROM schedule.Engagement e
            INNER JOIN venue.TimeSlot ts ON e.TimeSlotId = ts.TimeSlotId
            WHERE ts.EditionId = @EditionId AND e.IsDeleted = 0 AND ts.IsDeleted = 0
            ORDER BY ts.StartTimeUtc
            """;

        var result = await _connection.QueryAsync<Engagement>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Engagement>> GetByArtistAsync(long artistId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                EngagementId, TimeSlotId, ArtistId, Notes,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM schedule.Engagement
            WHERE ArtistId = @ArtistId AND IsDeleted = 0
            """;

        var result = await _connection.QueryAsync<Engagement>(
            new CommandDefinition(sql, new { ArtistId = artistId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(Engagement engagement, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO schedule.Engagement (
                TimeSlotId, ArtistId, Notes, IsDeleted,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @TimeSlotId, @ArtistId, @Notes, @IsDeleted,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, engagement, cancellationToken: ct));

        return engagement.EngagementId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Engagement engagement, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE schedule.Engagement
            SET ArtistId = @ArtistId,
                Notes = @Notes,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE EngagementId = @EngagementId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, engagement, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long engagementId, long deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE schedule.Engagement
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE EngagementId = @EngagementId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EngagementId = engagementId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long engagementId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM schedule.Engagement
            WHERE EngagementId = @EngagementId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EngagementId = engagementId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<bool> TimeSlotHasEngagementAsync(long timeSlotId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM schedule.Engagement
            WHERE TimeSlotId = @TimeSlotId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { TimeSlotId = timeSlotId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<long?> GetFestivalIdAsync(long engagementId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT f.FestivalId 
            FROM schedule.Engagement e
            INNER JOIN venue.TimeSlot ts ON e.TimeSlotId = ts.TimeSlotId
            INNER JOIN core.FestivalEdition fe ON ts.EditionId = fe.EditionId
            INNER JOIN core.Festival f ON fe.FestivalId = f.FestivalId
            WHERE e.EngagementId = @EngagementId AND e.IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(sql, new { EngagementId = engagementId }, cancellationToken: ct));
    }
}

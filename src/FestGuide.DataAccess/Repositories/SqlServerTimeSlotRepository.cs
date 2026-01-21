using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of ITimeSlotRepository using Dapper.
/// </summary>
public class SqlServerTimeSlotRepository : ITimeSlotRepository
{
    private readonly IDbConnection _connection;

    public SqlServerTimeSlotRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<TimeSlot?> GetByIdAsync(Guid timeSlotId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                TimeSlotId, StageId, EditionId, StartTimeUtc, EndTimeUtc,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.TimeSlot
            WHERE TimeSlotId = @TimeSlotId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<TimeSlot>(
            new CommandDefinition(sql, new { TimeSlotId = timeSlotId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimeSlot>> GetByIdsAsync(IEnumerable<Guid> timeSlotIds, CancellationToken ct = default)
    {
        var timeSlotIdsList = timeSlotIds?.ToList();
        if (timeSlotIdsList == null || !timeSlotIdsList.Any())
        {
            return Array.Empty<TimeSlot>();
        }

        const string sql = """
            SELECT 
                TimeSlotId, StageId, EditionId, StartTimeUtc, EndTimeUtc,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.TimeSlot
            WHERE TimeSlotId IN @TimeSlotIds AND IsDeleted = 0
            """;

        var result = await _connection.QueryAsync<TimeSlot>(
            new CommandDefinition(sql, new { TimeSlotIds = timeSlotIdsList }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimeSlot>> GetByStageAndEditionAsync(Guid stageId, Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                TimeSlotId, StageId, EditionId, StartTimeUtc, EndTimeUtc,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.TimeSlot
            WHERE StageId = @StageId AND EditionId = @EditionId AND IsDeleted = 0
            ORDER BY StartTimeUtc
            """;

        var result = await _connection.QueryAsync<TimeSlot>(
            new CommandDefinition(sql, new { StageId = stageId, EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimeSlot>> GetByEditionAsync(Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                TimeSlotId, StageId, EditionId, StartTimeUtc, EndTimeUtc,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.TimeSlot
            WHERE EditionId = @EditionId AND IsDeleted = 0
            ORDER BY StartTimeUtc
            """;

        var result = await _connection.QueryAsync<TimeSlot>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(TimeSlot timeSlot, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO venue.TimeSlot (
                TimeSlotId, StageId, EditionId, StartTimeUtc, EndTimeUtc, IsDeleted,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @TimeSlotId, @StageId, @EditionId, @StartTimeUtc, @EndTimeUtc, @IsDeleted,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, timeSlot, cancellationToken: ct));

        return timeSlot.TimeSlotId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TimeSlot timeSlot, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE venue.TimeSlot
            SET StartTimeUtc = @StartTimeUtc,
                EndTimeUtc = @EndTimeUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE TimeSlotId = @TimeSlotId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, timeSlot, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid timeSlotId, Guid deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE venue.TimeSlot
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE TimeSlotId = @TimeSlotId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { TimeSlotId = timeSlotId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid timeSlotId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM venue.TimeSlot
            WHERE TimeSlotId = @TimeSlotId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { TimeSlotId = timeSlotId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<Guid?> GetEditionIdAsync(Guid timeSlotId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EditionId FROM venue.TimeSlot
            WHERE TimeSlotId = @TimeSlotId AND IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { TimeSlotId = timeSlotId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<Guid?> GetFestivalIdAsync(Guid timeSlotId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT f.FestivalId 
            FROM venue.TimeSlot ts
            INNER JOIN core.FestivalEdition fe ON ts.EditionId = fe.EditionId
            INNER JOIN core.Festival f ON fe.FestivalId = f.FestivalId
            WHERE ts.TimeSlotId = @TimeSlotId AND ts.IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { TimeSlotId = timeSlotId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> HasOverlapAsync(Guid stageId, Guid editionId, DateTime startTimeUtc, DateTime endTimeUtc, Guid? excludeTimeSlotId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM venue.TimeSlot
            WHERE StageId = @StageId 
                AND EditionId = @EditionId 
                AND IsDeleted = 0
                AND (@ExcludeTimeSlotId IS NULL OR TimeSlotId != @ExcludeTimeSlotId)
                AND StartTimeUtc < @EndTimeUtc 
                AND EndTimeUtc > @StartTimeUtc
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StageId = stageId, EditionId = editionId, StartTimeUtc = startTimeUtc, EndTimeUtc = endTimeUtc, ExcludeTimeSlotId = excludeTimeSlotId }, cancellationToken: ct));

        return count > 0;
    }
}

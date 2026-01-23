using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IScheduleRepository using Dapper.
/// </summary>
public class SqlServerScheduleRepository : IScheduleRepository
{
    private readonly IDbConnection _connection;

    public SqlServerScheduleRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Schedule?> GetByIdAsync(long scheduleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                ScheduleId, EditionId, Version, PublishedAtUtc, PublishedBy,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM schedule.Schedule
            WHERE ScheduleId = @ScheduleId
            """;

        return await _connection.QuerySingleOrDefaultAsync<Schedule>(
            new CommandDefinition(sql, new { ScheduleId = scheduleId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<Schedule?> GetByEditionAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                ScheduleId, EditionId, Version, PublishedAtUtc, PublishedBy,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM schedule.Schedule
            WHERE EditionId = @EditionId
            """;

        return await _connection.QuerySingleOrDefaultAsync<Schedule>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(Schedule schedule, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO schedule.Schedule (
                ScheduleId, EditionId, Version, PublishedAtUtc, PublishedBy,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @ScheduleId, @EditionId, @Version, @PublishedAtUtc, @PublishedBy,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, schedule, cancellationToken: ct));

        return schedule.ScheduleId;
    }

    /// <inheritdoc />
    public async Task PublishAsync(long scheduleId, long publishedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE schedule.Schedule
            SET Version = Version + 1,
                PublishedAtUtc = @PublishedAtUtc,
                PublishedBy = @PublishedBy,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @PublishedBy
            WHERE ScheduleId = @ScheduleId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ScheduleId = scheduleId, PublishedBy = publishedBy, PublishedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForEditionAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM schedule.Schedule
            WHERE EditionId = @EditionId
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<Schedule> GetOrCreateAsync(long editionId, long createdBy, CancellationToken ct = default)
    {
        var existing = await GetByEditionAsync(editionId, ct);
        if (existing != null)
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var schedule = new Schedule
        {
            EditionId = editionId,
            Version = 1,
            CreatedAtUtc = now,
            CreatedBy = createdBy,
            ModifiedAtUtc = now,
            ModifiedBy = createdBy
        };

        await CreateAsync(schedule, ct);
        return schedule;
    }
}

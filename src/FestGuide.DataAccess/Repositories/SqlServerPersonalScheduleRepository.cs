using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IPersonalScheduleRepository using Dapper.
/// </summary>
public class SqlServerPersonalScheduleRepository : IPersonalScheduleRepository
{
    private readonly IDbConnection _connection;

    public SqlServerPersonalScheduleRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<PersonalSchedule?> GetByIdAsync(Guid personalScheduleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                PersonalScheduleId, UserId, EditionId, Name, IsDefault,
                LastSyncedAtUtc, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalSchedule
            WHERE PersonalScheduleId = @PersonalScheduleId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<PersonalSchedule>(
            new CommandDefinition(sql, new { PersonalScheduleId = personalScheduleId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonalSchedule>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                PersonalScheduleId, UserId, EditionId, Name, IsDefault,
                LastSyncedAtUtc, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalSchedule
            WHERE UserId = @UserId AND IsDeleted = 0
            ORDER BY CreatedAtUtc DESC
            """;

        var result = await _connection.QueryAsync<PersonalSchedule>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonalSchedule>> GetByUserAndEditionAsync(Guid userId, Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                PersonalScheduleId, UserId, EditionId, Name, IsDefault,
                LastSyncedAtUtc, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalSchedule
            WHERE UserId = @UserId AND EditionId = @EditionId AND IsDeleted = 0
            ORDER BY IsDefault DESC, CreatedAtUtc DESC
            """;

        var result = await _connection.QueryAsync<PersonalSchedule>(
            new CommandDefinition(sql, new { UserId = userId, EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<PersonalSchedule?> GetDefaultAsync(Guid userId, Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                PersonalScheduleId, UserId, EditionId, Name, IsDefault,
                LastSyncedAtUtc, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalSchedule
            WHERE UserId = @UserId AND EditionId = @EditionId AND IsDeleted = 0
            ORDER BY IsDefault DESC, CreatedAtUtc ASC
            """;

        return await _connection.QuerySingleOrDefaultAsync<PersonalSchedule>(
            new CommandDefinition(sql, new { UserId = userId, EditionId = editionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(PersonalSchedule personalSchedule, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO attendee.PersonalSchedule (
                PersonalScheduleId, UserId, EditionId, Name, IsDefault,
                LastSyncedAtUtc, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @PersonalScheduleId, @UserId, @EditionId, @Name, @IsDefault,
                @LastSyncedAtUtc, @IsDeleted, @DeletedAtUtc,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, personalSchedule, cancellationToken: ct));

        return personalSchedule.PersonalScheduleId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PersonalSchedule personalSchedule, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE attendee.PersonalSchedule SET
                Name = @Name,
                IsDefault = @IsDefault,
                LastSyncedAtUtc = @LastSyncedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE PersonalScheduleId = @PersonalScheduleId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, personalSchedule, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid personalScheduleId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE attendee.PersonalSchedule SET
                IsDeleted = 1,
                DeletedAtUtc = GETUTCDATE()
            WHERE PersonalScheduleId = @PersonalScheduleId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { PersonalScheduleId = personalScheduleId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid personalScheduleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM attendee.PersonalSchedule
                WHERE PersonalScheduleId = @PersonalScheduleId AND IsDeleted = 0
            ) THEN 1 ELSE 0 END
            """;

        return await _connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { PersonalScheduleId = personalScheduleId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonalScheduleEntry>> GetEntriesAsync(Guid personalScheduleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                PersonalScheduleEntryId, PersonalScheduleId, EngagementId, Notes,
                NotificationsEnabled, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalScheduleEntry
            WHERE PersonalScheduleId = @PersonalScheduleId AND IsDeleted = 0
            ORDER BY CreatedAtUtc
            """;

        var result = await _connection.QueryAsync<PersonalScheduleEntry>(
            new CommandDefinition(sql, new { PersonalScheduleId = personalScheduleId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PersonalScheduleEntry>>> GetEntriesByScheduleIdsAsync(IEnumerable<Guid> personalScheduleIds, CancellationToken ct = default)
    {
        var scheduleIdsList = personalScheduleIds.ToList();
        if (!scheduleIdsList.Any())
        {
            return new Dictionary<Guid, IReadOnlyList<PersonalScheduleEntry>>();
        }

        const string sql = """
            SELECT 
                PersonalScheduleEntryId, PersonalScheduleId, EngagementId, Notes,
                NotificationsEnabled, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalScheduleEntry
            WHERE PersonalScheduleId IN @PersonalScheduleIds AND IsDeleted = 0
            ORDER BY PersonalScheduleId, CreatedAtUtc
            """;

        var result = await _connection.QueryAsync<PersonalScheduleEntry>(
            new CommandDefinition(sql, new { PersonalScheduleIds = scheduleIdsList }, cancellationToken: ct));

        // Group entries by schedule ID
        return result
            .GroupBy(e => e.PersonalScheduleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PersonalScheduleEntry>)g.ToList());
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                PersonalScheduleEntryId, PersonalScheduleId, EngagementId, Notes,
                NotificationsEnabled, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalScheduleEntry
            WHERE PersonalScheduleEntryId = @EntryId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<PersonalScheduleEntry>(
            new CommandDefinition(sql, new { EntryId = entryId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<Guid> AddEntryAsync(PersonalScheduleEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO attendee.PersonalScheduleEntry (
                PersonalScheduleEntryId, PersonalScheduleId, EngagementId, Notes,
                NotificationsEnabled, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @PersonalScheduleEntryId, @PersonalScheduleId, @EngagementId, @Notes,
                @NotificationsEnabled, @IsDeleted, @DeletedAtUtc,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, entry, cancellationToken: ct));

        return entry.PersonalScheduleEntryId;
    }

    /// <inheritdoc />
    public async Task UpdateEntryAsync(PersonalScheduleEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE attendee.PersonalScheduleEntry SET
                Notes = @Notes,
                NotificationsEnabled = @NotificationsEnabled,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE PersonalScheduleEntryId = @PersonalScheduleEntryId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, entry, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RemoveEntryAsync(Guid entryId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE attendee.PersonalScheduleEntry SET
                IsDeleted = 1,
                DeletedAtUtc = GETUTCDATE()
            WHERE PersonalScheduleEntryId = @EntryId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { EntryId = entryId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> HasEngagementAsync(Guid personalScheduleId, Guid engagementId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM attendee.PersonalScheduleEntry
                WHERE PersonalScheduleId = @PersonalScheduleId 
                  AND EngagementId = @EngagementId 
                  AND IsDeleted = 0
            ) THEN 1 ELSE 0 END
            """;

        return await _connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { PersonalScheduleId = personalScheduleId, EngagementId = engagementId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<Guid?> GetScheduleIdForEntryAsync(Guid entryId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT PersonalScheduleId 
            FROM attendee.PersonalScheduleEntry
            WHERE PersonalScheduleEntryId = @EntryId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { EntryId = entryId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateLastSyncedAsync(Guid personalScheduleId, DateTime syncedAtUtc, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE attendee.PersonalSchedule SET
                LastSyncedAtUtc = @SyncedAtUtc
            WHERE PersonalScheduleId = @PersonalScheduleId
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { PersonalScheduleId = personalScheduleId, SyncedAtUtc = syncedAtUtc }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonalSchedule>> GetByEditionAsync(Guid editionId, int limit = 1000, int offset = 0, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                PersonalScheduleId, UserId, EditionId, Name, IsDefault,
                LastSyncedAtUtc, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM attendee.PersonalSchedule
            WHERE EditionId = @EditionId AND IsDeleted = 0
            ORDER BY CreatedAtUtc
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY
            """;

        var result = await _connection.QueryAsync<PersonalSchedule>(
            new CommandDefinition(sql, new { EditionId = editionId, Limit = limit, Offset = offset }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetUserIdsWithEngagementAsync(Guid engagementId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT ps.UserId
            FROM attendee.PersonalSchedule ps
            INNER JOIN attendee.PersonalScheduleEntry pse ON ps.PersonalScheduleId = pse.PersonalScheduleId
            WHERE pse.EngagementId = @EngagementId 
              AND pse.IsDeleted = 0 
              AND ps.IsDeleted = 0
              AND pse.NotificationsEnabled = 1
            """;

        var result = await _connection.QueryAsync<Guid>(
            new CommandDefinition(sql, new { EngagementId = engagementId }, cancellationToken: ct));

        return result.ToList();
    }
}

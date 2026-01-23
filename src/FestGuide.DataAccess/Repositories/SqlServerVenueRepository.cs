using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IVenueRepository using Dapper.
/// </summary>
public class SqlServerVenueRepository : IVenueRepository
{
    private readonly IDbConnection _connection;

    public SqlServerVenueRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Venue?> GetByIdAsync(long venueId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                VenueId, FestivalId, Name, Description, Address,
                Latitude, Longitude, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.Venue
            WHERE VenueId = @VenueId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Venue>(
            new CommandDefinition(sql, new { VenueId = venueId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Venue>> GetByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                VenueId, FestivalId, Name, Description, Address,
                Latitude, Longitude, IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM venue.Venue
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            ORDER BY Name
            """;

        var result = await _connection.QueryAsync<Venue>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Venue>> GetByEditionAsync(long editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                v.VenueId, v.FestivalId, v.Name, v.Description, v.Address,
                v.Latitude, v.Longitude, v.IsDeleted, v.DeletedAtUtc,
                v.CreatedAtUtc, v.CreatedBy, v.ModifiedAtUtc, v.ModifiedBy
            FROM venue.Venue v
            INNER JOIN venue.EditionVenue ev ON v.VenueId = ev.VenueId
            WHERE ev.EditionId = @EditionId AND v.IsDeleted = 0
            ORDER BY v.Name
            """;

        var result = await _connection.QueryAsync<Venue>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(Venue venue, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO venue.Venue (
                FestivalId, Name, Description, Address,
                Latitude, Longitude, IsDeleted,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @FestivalId, @Name, @Description, @Address,
                @Latitude, @Longitude, @IsDeleted,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, venue, cancellationToken: ct));

        return venue.VenueId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Venue venue, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE venue.Venue
            SET Name = @Name,
                Description = @Description,
                Address = @Address,
                Latitude = @Latitude,
                Longitude = @Longitude,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE VenueId = @VenueId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, venue, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long venueId, long deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE venue.Venue
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE VenueId = @VenueId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { VenueId = venueId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long venueId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM venue.Venue
            WHERE VenueId = @VenueId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { VenueId = venueId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<long?> GetFestivalIdAsync(long venueId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT FestivalId FROM venue.Venue
            WHERE VenueId = @VenueId AND IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(sql, new { VenueId = venueId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task AddToEditionAsync(long editionId, long venueId, long createdBy, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO venue.EditionVenue (EditionId, VenueId, CreatedAtUtc, CreatedBy)
            VALUES (@EditionId, @VenueId, @CreatedAtUtc, @CreatedBy)
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EditionId = editionId, VenueId = venueId, CreatedAtUtc = DateTime.UtcNow, CreatedBy = createdBy },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RemoveFromEditionAsync(long editionId, long venueId, CancellationToken ct = default)
    {
        const string sql = """
            DELETE FROM venue.EditionVenue
            WHERE EditionId = @EditionId AND VenueId = @VenueId
            """;

        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EditionId = editionId, VenueId = venueId },
            cancellationToken: ct));
    }
}

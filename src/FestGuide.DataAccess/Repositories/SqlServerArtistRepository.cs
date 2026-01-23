using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IArtistRepository using Dapper.
/// </summary>
public class SqlServerArtistRepository : IArtistRepository
{
    private readonly IDbConnection _connection;

    public SqlServerArtistRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Artist?> GetByIdAsync(long artistId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                ArtistId, FestivalId, Name, Genre, Bio,
                ImageUrl, WebsiteUrl, SpotifyUrl,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Artist
            WHERE ArtistId = @ArtistId AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Artist>(
            new CommandDefinition(sql, new { ArtistId = artistId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artist>> GetByIdsAsync(IEnumerable<long> artistIds, CancellationToken ct = default)
    {
        var artistIdsList = artistIds?.ToList();
        if (artistIdsList == null || !artistIdsList.Any())
        {
            return Array.Empty<Artist>();
        }

        const string sql = """
            SELECT 
                ArtistId, FestivalId, Name, Genre, Bio,
                ImageUrl, WebsiteUrl, SpotifyUrl,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Artist
            WHERE ArtistId IN @ArtistIds AND IsDeleted = 0
            """;

        var artists = await _connection.QueryAsync<Artist>(
            new CommandDefinition(sql, new { ArtistIds = artistIdsList }, cancellationToken: ct));

        return artists.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artist>> GetByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                ArtistId, FestivalId, Name, Genre, Bio,
                ImageUrl, WebsiteUrl, SpotifyUrl,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Artist
            WHERE FestivalId = @FestivalId AND IsDeleted = 0
            ORDER BY Name
            """;

        var result = await _connection.QueryAsync<Artist>(
            new CommandDefinition(sql, new { FestivalId = festivalId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artist>> SearchByNameAsync(long festivalId, string searchTerm, int limit = 20, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Limit)
                ArtistId, FestivalId, Name, Genre, Bio,
                ImageUrl, WebsiteUrl, SpotifyUrl,
                IsDeleted, DeletedAtUtc,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            FROM core.Artist
            WHERE FestivalId = @FestivalId AND IsDeleted = 0 AND Name LIKE @SearchTerm
            ORDER BY Name
            """;

        var result = await _connection.QueryAsync<Artist>(
            new CommandDefinition(sql, new { FestivalId = festivalId, SearchTerm = $"%{searchTerm}%", Limit = limit }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(Artist artist, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO core.Artist (
                FestivalId, Name, Genre, Bio,
                ImageUrl, WebsiteUrl, SpotifyUrl, IsDeleted,
                CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
            ) VALUES (
                @FestivalId, @Name, @Genre, @Bio,
                @ImageUrl, @WebsiteUrl, @SpotifyUrl, @IsDeleted,
                @CreatedAtUtc, @CreatedBy, @ModifiedAtUtc, @ModifiedBy
            )
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, artist, cancellationToken: ct));

        return artist.ArtistId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Artist artist, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.Artist
            SET Name = @Name,
                Genre = @Genre,
                Bio = @Bio,
                ImageUrl = @ImageUrl,
                WebsiteUrl = @WebsiteUrl,
                SpotifyUrl = @SpotifyUrl,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @ModifiedBy
            WHERE ArtistId = @ArtistId AND IsDeleted = 0
            """;

        await _connection.ExecuteAsync(new CommandDefinition(sql, artist, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long artistId, long deletedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE core.Artist
            SET IsDeleted = 1,
                DeletedAtUtc = @DeletedAtUtc,
                ModifiedAtUtc = @ModifiedAtUtc,
                ModifiedBy = @DeletedBy
            WHERE ArtistId = @ArtistId
            """;

        var now = DateTime.UtcNow;
        await _connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ArtistId = artistId, DeletedBy = deletedBy, DeletedAtUtc = now, ModifiedAtUtc = now },
            cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long artistId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM core.Artist
            WHERE ArtistId = @ArtistId AND IsDeleted = 0
            """;

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { ArtistId = artistId }, cancellationToken: ct));

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<long?> GetFestivalIdAsync(long artistId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT FestivalId FROM core.Artist
            WHERE ArtistId = @ArtistId AND IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(sql, new { ArtistId = artistId }, cancellationToken: ct));
    }
}

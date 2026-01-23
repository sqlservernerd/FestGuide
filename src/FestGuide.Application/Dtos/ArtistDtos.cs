using FestGuide.Domain.Entities;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Response DTO for artist.
/// </summary>
public sealed record ArtistDto(
    long ArtistId,
    long FestivalId,
    string Name,
    string? Genre,
    string? Bio,
    string? ImageUrl,
    string? WebsiteUrl,
    string? SpotifyUrl,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static ArtistDto FromEntity(Artist artist) =>
        new(
            artist.ArtistId,
            artist.FestivalId,
            artist.Name,
            artist.Genre,
            artist.Bio,
            artist.ImageUrl,
            artist.WebsiteUrl,
            artist.SpotifyUrl,
            artist.CreatedAtUtc,
            artist.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating an artist.
/// </summary>
public sealed record CreateArtistRequest(
    string Name,
    string? Genre,
    string? Bio,
    string? ImageUrl,
    string? WebsiteUrl,
    string? SpotifyUrl);

/// <summary>
/// Request DTO for updating an artist.
/// </summary>
public sealed record UpdateArtistRequest(
    string? Name,
    string? Genre,
    string? Bio,
    string? ImageUrl,
    string? WebsiteUrl,
    string? SpotifyUrl);

/// <summary>
/// Summary DTO for artist list items.
/// </summary>
public sealed record ArtistSummaryDto(
    long ArtistId,
    string Name,
    string? Genre,
    string? ImageUrl)
{
    public static ArtistSummaryDto FromEntity(Artist artist) =>
        new(
            artist.ArtistId,
            artist.Name,
            artist.Genre,
            artist.ImageUrl);
}

using FestConnect.Domain.Entities;

namespace FestConnect.Application.Dtos;

/// <summary>
/// Response DTO for venue.
/// </summary>
public sealed record VenueDto(
    long VenueId,
    long FestivalId,
    string Name,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static VenueDto FromEntity(Venue venue) =>
        new(
            venue.VenueId,
            venue.FestivalId,
            venue.Name,
            venue.Description,
            venue.Address,
            venue.Latitude,
            venue.Longitude,
            venue.CreatedAtUtc,
            venue.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating a venue.
/// </summary>
public sealed record CreateVenueRequest(
    string Name,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>
/// Request DTO for updating a venue.
/// </summary>
public sealed record UpdateVenueRequest(
    string? Name,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>
/// Summary DTO for venue list items.
/// </summary>
public sealed record VenueSummaryDto(
    long VenueId,
    string Name,
    string? Address)
{
    public static VenueSummaryDto FromEntity(Venue venue) =>
        new(
            venue.VenueId,
            venue.Name,
            venue.Address);
}

/// <summary>
/// Response DTO for stage.
/// </summary>
public sealed record StageDto(
    long StageId,
    long VenueId,
    string Name,
    string? Description,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static StageDto FromEntity(Stage stage) =>
        new(
            stage.StageId,
            stage.VenueId,
            stage.Name,
            stage.Description,
            stage.SortOrder,
            stage.CreatedAtUtc,
            stage.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating a stage.
/// </summary>
public sealed record CreateStageRequest(
    string Name,
    string? Description,
    int SortOrder = 0);

/// <summary>
/// Request DTO for updating a stage.
/// </summary>
public sealed record UpdateStageRequest(
    string? Name,
    string? Description,
    int? SortOrder);

/// <summary>
/// Summary DTO for stage list items.
/// </summary>
public sealed record StageSummaryDto(
    long StageId,
    string Name,
    int SortOrder)
{
    public static StageSummaryDto FromEntity(Stage stage) =>
        new(
            stage.StageId,
            stage.Name,
            stage.SortOrder);
}

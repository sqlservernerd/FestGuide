using FestGuide.Domain.Entities;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Response DTO for festival.
/// </summary>
public sealed record FestivalDto(
    Guid FestivalId,
    string Name,
    string? Description,
    string? ImageUrl,
    string? WebsiteUrl,
    Guid OwnerUserId,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static FestivalDto FromEntity(Festival festival) =>
        new(
            festival.FestivalId,
            festival.Name,
            festival.Description,
            festival.ImageUrl,
            festival.WebsiteUrl,
            festival.OwnerUserId,
            festival.CreatedAtUtc,
            festival.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating a festival.
/// </summary>
public sealed record CreateFestivalRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    string? WebsiteUrl);

/// <summary>
/// Request DTO for updating a festival.
/// </summary>
public sealed record UpdateFestivalRequest(
    string? Name,
    string? Description,
    string? ImageUrl,
    string? WebsiteUrl);

/// <summary>
/// Request DTO for transferring festival ownership.
/// </summary>
public sealed record TransferOwnershipRequest(
    Guid NewOwnerUserId);

/// <summary>
/// Summary DTO for festival list items.
/// </summary>
public sealed record FestivalSummaryDto(
    Guid FestivalId,
    string Name,
    string? ImageUrl,
    bool IsOwner)
{
    public static FestivalSummaryDto FromEntity(Festival festival, Guid currentUserId) =>
        new(
            festival.FestivalId,
            festival.Name,
            festival.ImageUrl,
            festival.OwnerUserId == currentUserId);
}

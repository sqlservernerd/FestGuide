using FestConnect.Domain.Entities;
using FestConnect.Domain.Enums;

namespace FestConnect.Application.Dtos;

/// <summary>
/// Response DTO for festival edition.
/// </summary>
public sealed record EditionDto(
    long EditionId,
    long FestivalId,
    string Name,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string TimezoneId,
    string? TicketUrl,
    EditionStatus Status,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static EditionDto FromEntity(FestivalEdition edition) =>
        new(
            edition.EditionId,
            edition.FestivalId,
            edition.Name,
            edition.StartDateUtc,
            edition.EndDateUtc,
            edition.TimezoneId,
            edition.TicketUrl,
            edition.Status,
            edition.CreatedAtUtc,
            edition.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating an edition.
/// </summary>
public sealed record CreateEditionRequest(
    string Name,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string TimezoneId,
    string? TicketUrl);

/// <summary>
/// Request DTO for updating an edition.
/// </summary>
public sealed record UpdateEditionRequest(
    string? Name,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string? TimezoneId,
    string? TicketUrl);

/// <summary>
/// Summary DTO for edition list items.
/// </summary>
public sealed record EditionSummaryDto(
    long EditionId,
    string Name,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    EditionStatus Status)
{
    public static EditionSummaryDto FromEntity(FestivalEdition edition) =>
        new(
            edition.EditionId,
            edition.Name,
            edition.StartDateUtc,
            edition.EndDateUtc,
            edition.Status);
}

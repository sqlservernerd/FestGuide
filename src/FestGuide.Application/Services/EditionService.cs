using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Edition service implementation.
/// </summary>
public class EditionService : IEditionService
{
    private readonly IEditionRepository _editionRepository;
    private readonly IFestivalRepository _festivalRepository;
    private readonly IFestivalAuthorizationService _authorizationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<EditionService> _logger;

    public EditionService(
        IEditionRepository editionRepository,
        IFestivalRepository festivalRepository,
        IFestivalAuthorizationService authorizationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<EditionService> logger)
    {
        _editionRepository = editionRepository ?? throw new ArgumentNullException(nameof(editionRepository));
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EditionDto> GetByIdAsync(long editionId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        return EditionDto.FromEntity(edition);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EditionSummaryDto>> GetByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        var editions = await _editionRepository.GetByFestivalAsync(festivalId, ct);
        return editions.Select(EditionSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EditionSummaryDto>> GetPublishedByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        var editions = await _editionRepository.GetPublishedByFestivalAsync(festivalId, ct);
        return editions.Select(EditionSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<EditionDto> CreateAsync(long festivalId, long userId, CreateEditionRequest request, CancellationToken ct = default)
    {
        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Editions, ct))
        {
            throw new ForbiddenException("You do not have permission to create editions for this festival.");
        }

        if (!await _festivalRepository.ExistsAsync(festivalId, ct))
        {
            throw new FestivalNotFoundException(festivalId);
        }

        var now = _dateTimeProvider.UtcNow;
        var edition = new FestivalEdition
        {
            FestivalId = festivalId,
            Name = request.Name,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            TimezoneId = request.TimezoneId,
            TicketUrl = request.TicketUrl,
            Status = EditionStatus.Draft,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _editionRepository.CreateAsync(edition, ct);

        _logger.LogInformation("Edition {EditionId} created for festival {FestivalId} by user {UserId}",
            edition.EditionId, festivalId, userId);

        return EditionDto.FromEntity(edition);
    }

    /// <inheritdoc />
    public async Task<EditionDto> UpdateAsync(long editionId, long userId, UpdateEditionRequest request, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        if (!await _authorizationService.HasScopeAsync(userId, edition.FestivalId, PermissionScope.Editions, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this edition.");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            edition.Name = request.Name;
        }

        if (request.StartDateUtc.HasValue)
        {
            edition.StartDateUtc = request.StartDateUtc.Value;
        }

        if (request.EndDateUtc.HasValue)
        {
            edition.EndDateUtc = request.EndDateUtc.Value;
        }

        if (!string.IsNullOrEmpty(request.TimezoneId))
        {
            edition.TimezoneId = request.TimezoneId;
        }

        if (request.TicketUrl != null)
        {
            edition.TicketUrl = request.TicketUrl;
        }

        edition.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        edition.ModifiedBy = userId;

        await _editionRepository.UpdateAsync(edition, ct);

        _logger.LogInformation("Edition {EditionId} updated by user {UserId}", editionId, userId);

        return EditionDto.FromEntity(edition);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long editionId, long userId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        if (!await _authorizationService.HasScopeAsync(userId, edition.FestivalId, PermissionScope.Editions, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this edition.");
        }

        await _editionRepository.DeleteAsync(editionId, userId, ct);

        _logger.LogInformation("Edition {EditionId} deleted by user {UserId}", editionId, userId);
    }
}

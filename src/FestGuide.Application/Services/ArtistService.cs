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
/// Artist service implementation.
/// </summary>
public class ArtistService : IArtistService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IFestivalRepository _festivalRepository;
    private readonly IFestivalAuthorizationService _authorizationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ArtistService> _logger;

    public ArtistService(
        IArtistRepository artistRepository,
        IFestivalRepository festivalRepository,
        IFestivalAuthorizationService authorizationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ArtistService> logger)
    {
        _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ArtistDto> GetByIdAsync(long artistId, CancellationToken ct = default)
    {
        var artist = await _artistRepository.GetByIdAsync(artistId, ct)
            ?? throw new ArtistNotFoundException(artistId);

        return ArtistDto.FromEntity(artist);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtistSummaryDto>> GetByFestivalAsync(long festivalId, CancellationToken ct = default)
    {
        var artists = await _artistRepository.GetByFestivalAsync(festivalId, ct);
        return artists.Select(ArtistSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtistSummaryDto>> SearchAsync(long festivalId, string searchTerm, int limit = 20, CancellationToken ct = default)
    {
        var artists = await _artistRepository.SearchByNameAsync(festivalId, searchTerm, limit, ct);
        return artists.Select(ArtistSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<ArtistDto> CreateAsync(long festivalId, long userId, CreateArtistRequest request, CancellationToken ct = default)
    {
        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Artists, ct))
        {
            throw new ForbiddenException("You do not have permission to create artists for this festival.");
        }

        if (!await _festivalRepository.ExistsAsync(festivalId, ct))
        {
            throw new FestivalNotFoundException(festivalId);
        }

        var now = _dateTimeProvider.UtcNow;
        var artist = new Artist
        {
            ArtistId = 0,
            FestivalId = festivalId,
            Name = request.Name,
            Genre = request.Genre,
            Bio = request.Bio,
            ImageUrl = request.ImageUrl,
            WebsiteUrl = request.WebsiteUrl,
            SpotifyUrl = request.SpotifyUrl,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _artistRepository.CreateAsync(artist, ct);

        _logger.LogInformation("Artist {ArtistId} created for festival {FestivalId} by user {UserId}",
            artist.ArtistId, festivalId, userId);

        return ArtistDto.FromEntity(artist);
    }

    /// <inheritdoc />
    public async Task<ArtistDto> UpdateAsync(long artistId, long userId, UpdateArtistRequest request, CancellationToken ct = default)
    {
        var artist = await _artistRepository.GetByIdAsync(artistId, ct)
            ?? throw new ArtistNotFoundException(artistId);

        if (!await _authorizationService.HasScopeAsync(userId, artist.FestivalId, PermissionScope.Artists, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this artist.");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            artist.Name = request.Name;
        }

        if (request.Genre != null)
        {
            artist.Genre = request.Genre;
        }

        if (request.Bio != null)
        {
            artist.Bio = request.Bio;
        }

        if (request.ImageUrl != null)
        {
            artist.ImageUrl = request.ImageUrl;
        }

        if (request.WebsiteUrl != null)
        {
            artist.WebsiteUrl = request.WebsiteUrl;
        }

        if (request.SpotifyUrl != null)
        {
            artist.SpotifyUrl = request.SpotifyUrl;
        }

        artist.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        artist.ModifiedBy = userId;

        await _artistRepository.UpdateAsync(artist, ct);

        _logger.LogInformation("Artist {ArtistId} updated by user {UserId}", artistId, userId);

        return ArtistDto.FromEntity(artist);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long artistId, long userId, CancellationToken ct = default)
    {
        var artist = await _artistRepository.GetByIdAsync(artistId, ct)
            ?? throw new ArtistNotFoundException(artistId);

        if (!await _authorizationService.HasScopeAsync(userId, artist.FestivalId, PermissionScope.Artists, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this artist.");
        }

        await _artistRepository.DeleteAsync(artistId, userId, ct);

        _logger.LogInformation("Artist {ArtistId} deleted by user {UserId}", artistId, userId);
    }
}

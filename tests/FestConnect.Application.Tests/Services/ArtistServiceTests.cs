using FluentAssertions;
using Moq;
using FestConnect.Application.Authorization;
using FestConnect.Application.Dtos;
using FestConnect.Application.Services;
using FestConnect.DataAccess.Abstractions;
using FestConnect.Domain.Entities;
using FestConnect.Domain.Enums;
using FestConnect.Domain.Exceptions;
using FestConnect.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestConnect.Application.Tests.Services;

public class ArtistServiceTests
{
    private readonly Mock<IArtistRepository> _mockArtistRepo;
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<ArtistService>> _mockLogger;
    private readonly ArtistService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public ArtistServiceTests()
    {
        _mockArtistRepo = new Mock<IArtistRepository>();
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<ArtistService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new ArtistService(
            _mockArtistRepo.Object,
            _mockFestivalRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsArtist()
    {
        // Arrange
        var artistId = 1L;
        var artist = CreateTestArtist(artistId);

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);

        // Act
        var result = await _sut.GetByIdAsync(artistId);

        // Assert
        result.Should().NotBeNull();
        result.ArtistId.Should().Be(artistId);
        result.Name.Should().Be(artist.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsArtistNotFoundException()
    {
        // Arrange
        var artistId = 2L;

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        // Act
        var act = () => _sut.GetByIdAsync(artistId);

        // Assert
        await act.Should().ThrowAsync<ArtistNotFoundException>();
    }

    #endregion

    #region GetByFestivalAsync Tests

    [Fact]
    public async Task GetByFestivalAsync_WithValidFestivalId_ReturnsArtists()
    {
        // Arrange
        var festivalId = 3L;
        var artists = new List<Artist>
        {
            CreateTestArtist(festivalId: festivalId, name: "Artist One"),
            CreateTestArtist(festivalId: festivalId, name: "Artist Two"),
            CreateTestArtist(festivalId: festivalId, name: "Artist Three")
        };

        _mockArtistRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Act
        var result = await _sut.GetByFestivalAsync(festivalId);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByFestivalAsync_WithNoArtists_ReturnsEmptyList()
    {
        // Arrange
        var festivalId = 4L;

        _mockArtistRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());

        // Act
        var result = await _sut.GetByFestivalAsync(festivalId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithMatchingTerm_ReturnsArtists()
    {
        // Arrange
        var festivalId = 5L;
        var searchTerm = "Rock";
        var artists = new List<Artist>
        {
            CreateTestArtist(festivalId: festivalId, name: "Rock Band"),
            CreateTestArtist(festivalId: festivalId, name: "The Rockers")
        };

        _mockArtistRepo.Setup(r => r.SearchByNameAsync(festivalId, searchTerm, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Act
        var result = await _sut.SearchAsync(festivalId, searchTerm);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var festivalId = 6L;
        var searchTerm = "NonExistent";

        _mockArtistRepo.Setup(r => r.SearchByNameAsync(festivalId, searchTerm, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());

        // Act
        var result = await _sut.SearchAsync(festivalId, searchTerm);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesArtist()
    {
        // Arrange
        var festivalId = 7L;
        var userId = 8L;
        var request = new CreateArtistRequest(
            Name: "The Headliners",
            Genre: "Rock",
            Bio: "An amazing rock band",
            ImageUrl: "https://example.com/image.jpg",
            WebsiteUrl: "https://theheadliners.com",
            SpotifyUrl: "https://spotify.com/artist/headliners");

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(r => r.CreateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101L);

        // Act
        var result = await _sut.CreateAsync(festivalId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.Genre.Should().Be(request.Genre);
        result.FestivalId.Should().Be(festivalId);

        _mockArtistRepo.Verify(r => r.CreateAsync(
            It.Is<Artist>(a => a.Name == request.Name && a.FestivalId == festivalId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = 9L;
        var userId = 10L;
        var request = new CreateArtistRequest(Name: "Test", Genre: null, Bio: null, ImageUrl: null, WebsiteUrl: null, SpotifyUrl: null);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateAsync(festivalId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentFestival_ThrowsFestivalNotFoundException()
    {
        // Arrange
        var festivalId = 11L;
        var userId = 12L;
        var request = new CreateArtistRequest(Name: "Test", Genre: null, Bio: null, ImageUrl: null, WebsiteUrl: null, SpotifyUrl: null);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateAsync(festivalId, userId, request);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesArtist()
    {
        // Arrange
        var artistId = 13L;
        var festivalId = 14L;
        var userId = 15L;
        var artist = CreateTestArtist(artistId, festivalId);
        var request = new UpdateArtistRequest(
            Name: "Updated Artist Name",
            Genre: "Electronic",
            Bio: null,
            ImageUrl: null,
            WebsiteUrl: null,
            SpotifyUrl: null);

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateAsync(artistId, userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockArtistRepo.Verify(r => r.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentArtist_ThrowsArtistNotFoundException()
    {
        // Arrange
        var artistId = 16L;
        var userId = 17L;
        var request = new UpdateArtistRequest(Name: "Updated", Genre: null, Bio: null, ImageUrl: null, WebsiteUrl: null, SpotifyUrl: null);

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        // Act
        var act = () => _sut.UpdateAsync(artistId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ArtistNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var artistId = 18L;
        var festivalId = 19L;
        var userId = 20L;
        var artist = CreateTestArtist(artistId, festivalId);
        var request = new UpdateArtistRequest(Name: "Updated", Genre: null, Bio: null, ImageUrl: null, WebsiteUrl: null, SpotifyUrl: null);

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.UpdateAsync(artistId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPermission_DeletesArtist()
    {
        // Arrange
        var artistId = 21L;
        var festivalId = 22L;
        var userId = 23L;
        var artist = CreateTestArtist(artistId, festivalId);

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(artistId, userId);

        // Assert
        _mockArtistRepo.Verify(r => r.DeleteAsync(artistId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentArtist_ThrowsArtistNotFoundException()
    {
        // Arrange
        var artistId = 24L;
        var userId = 25L;

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        // Act
        var act = () => _sut.DeleteAsync(artistId, userId);

        // Assert
        await act.Should().ThrowAsync<ArtistNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var artistId = 26L;
        var festivalId = 27L;
        var userId = 28L;
        var artist = CreateTestArtist(artistId, festivalId);

        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Artists, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.DeleteAsync(artistId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Helper Methods

    private Artist CreateTestArtist(long? artistId = null, long? festivalId = null, string? name = null)
    {
        return new Artist
        {
            ArtistId = artistId ?? 0L,
            FestivalId = festivalId ?? 0L,
            Name = name ?? "Test Artist",
            Genre = "Rock",
            Bio = "Test bio for the artist",
            ImageUrl = "https://example.com/artist.jpg",
            WebsiteUrl = "https://artist.com",
            SpotifyUrl = "https://spotify.com/artist/test",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    #endregion
}

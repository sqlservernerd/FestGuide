using FluentAssertions;
using Moq;
using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

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
        var artistId = Guid.NewGuid();
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
        var artistId = Guid.NewGuid();

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
        var festivalId = Guid.NewGuid();
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
        var festivalId = Guid.NewGuid();

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
        var festivalId = Guid.NewGuid();
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
        var festivalId = Guid.NewGuid();
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
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
            .ReturnsAsync(Guid.NewGuid());

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
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var artistId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var artistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var artistId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var artistId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var artistId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
        var artistId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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

    private Artist CreateTestArtist(Guid? artistId = null, Guid? festivalId = null, string? name = null)
    {
        return new Artist
        {
            ArtistId = artistId ?? Guid.NewGuid(),
            FestivalId = festivalId ?? Guid.NewGuid(),
            Name = name ?? "Test Artist",
            Genre = "Rock",
            Bio = "Test bio for the artist",
            ImageUrl = "https://example.com/artist.jpg",
            WebsiteUrl = "https://artist.com",
            SpotifyUrl = "https://spotify.com/artist/test",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = Guid.NewGuid()
        };
    }

    #endregion
}

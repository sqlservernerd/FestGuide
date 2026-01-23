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

public class VenueServiceTests
{
    private readonly Mock<IVenueRepository> _mockVenueRepo;
    private readonly Mock<IStageRepository> _mockStageRepo;
    private readonly Mock<IEditionRepository> _mockEditionRepo;
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<VenueService>> _mockLogger;
    private readonly VenueService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public VenueServiceTests()
    {
        _mockVenueRepo = new Mock<IVenueRepository>();
        _mockStageRepo = new Mock<IStageRepository>();
        _mockEditionRepo = new Mock<IEditionRepository>();
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<VenueService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new VenueService(
            _mockVenueRepo.Object,
            _mockStageRepo.Object,
            _mockEditionRepo.Object,
            _mockFestivalRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region Venue GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsVenue()
    {
        // Arrange
        var venueId = 1L;
        var venue = CreateTestVenue(venueId);

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);

        // Act
        var result = await _sut.GetByIdAsync(venueId);

        // Assert
        result.Should().NotBeNull();
        result.VenueId.Should().Be(venueId);
        result.Name.Should().Be(venue.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsVenueNotFoundException()
    {
        // Arrange
        var venueId = 2L;

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Venue?)null);

        // Act
        var act = () => _sut.GetByIdAsync(venueId);

        // Assert
        await act.Should().ThrowAsync<VenueNotFoundException>();
    }

    #endregion

    #region Venue GetByFestivalAsync Tests

    [Fact]
    public async Task GetByFestivalAsync_WithValidFestivalId_ReturnsVenues()
    {
        // Arrange
        var festivalId = 3L;
        var venues = new List<Venue>
        {
            CreateTestVenue(festivalId: festivalId, name: "Main Grounds"),
            CreateTestVenue(festivalId: festivalId, name: "VIP Area")
        };

        _mockVenueRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venues);

        // Act
        var result = await _sut.GetByFestivalAsync(festivalId);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Venue CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesVenue()
    {
        // Arrange
        var festivalId = 4L;
        var userId = 5L;
        var request = new CreateVenueRequest(
            Name: "Main Stage Area",
            Description: "The main festival grounds",
            Address: "123 Festival Lane",
            Latitude: 34.0522m,
            Longitude: -118.2437m);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockVenueRepo.Setup(r => r.CreateAsync(It.IsAny<Venue>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101L);

        // Act
        var result = await _sut.CreateAsync(festivalId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.FestivalId.Should().Be(festivalId);

        _mockVenueRepo.Verify(r => r.CreateAsync(
            It.Is<Venue>(v => v.Name == request.Name && v.FestivalId == festivalId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = 6L;
        var userId = 7L;
        var request = new CreateVenueRequest(Name: "Test", Description: null, Address: null, Latitude: null, Longitude: null);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
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
        var festivalId = 8L;
        var userId = 9L;
        var request = new CreateVenueRequest(Name: "Test", Description: null, Address: null, Latitude: null, Longitude: null);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateAsync(festivalId, userId, request);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>();
    }

    #endregion

    #region Venue UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesVenue()
    {
        // Arrange
        var venueId = 10L;
        var festivalId = 11L;
        var userId = 12L;
        var venue = CreateTestVenue(venueId, festivalId);
        var request = new UpdateVenueRequest(
            Name: "Updated Venue Name",
            Description: null,
            Address: null,
            Latitude: null,
            Longitude: null);

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateAsync(venueId, userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockVenueRepo.Verify(r => r.UpdateAsync(It.IsAny<Venue>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentVenue_ThrowsVenueNotFoundException()
    {
        // Arrange
        var venueId = 13L;
        var userId = 14L;
        var request = new UpdateVenueRequest(Name: "Updated", Description: null, Address: null, Latitude: null, Longitude: null);

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Venue?)null);

        // Act
        var act = () => _sut.UpdateAsync(venueId, userId, request);

        // Assert
        await act.Should().ThrowAsync<VenueNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var venueId = 15L;
        var festivalId = 16L;
        var userId = 17L;
        var venue = CreateTestVenue(venueId, festivalId);
        var request = new UpdateVenueRequest(Name: "Updated", Description: null, Address: null, Latitude: null, Longitude: null);

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.UpdateAsync(venueId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Venue DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPermission_DeletesVenue()
    {
        // Arrange
        var venueId = 18L;
        var festivalId = 19L;
        var userId = 20L;
        var venue = CreateTestVenue(venueId, festivalId);

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(venueId, userId);

        // Assert
        _mockVenueRepo.Verify(r => r.DeleteAsync(venueId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentVenue_ThrowsVenueNotFoundException()
    {
        // Arrange
        var venueId = 21L;
        var userId = 22L;

        _mockVenueRepo.Setup(r => r.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Venue?)null);

        // Act
        var act = () => _sut.DeleteAsync(venueId, userId);

        // Assert
        await act.Should().ThrowAsync<VenueNotFoundException>();
    }

    #endregion

    #region Stage Tests

    [Fact]
    public async Task GetStageByIdAsync_WithValidId_ReturnsStage()
    {
        // Arrange
        var stageId = 23L;
        var stage = CreateTestStage(stageId);

        _mockStageRepo.Setup(r => r.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);

        // Act
        var result = await _sut.GetStageByIdAsync(stageId);

        // Assert
        result.Should().NotBeNull();
        result.StageId.Should().Be(stageId);
    }

    [Fact]
    public async Task GetStageByIdAsync_WithInvalidId_ThrowsStageNotFoundException()
    {
        // Arrange
        var stageId = 24L;

        _mockStageRepo.Setup(r => r.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stage?)null);

        // Act
        var act = () => _sut.GetStageByIdAsync(stageId);

        // Assert
        await act.Should().ThrowAsync<StageNotFoundException>();
    }

    [Fact]
    public async Task GetStagesByVenueAsync_WithValidVenueId_ReturnsStages()
    {
        // Arrange
        var venueId = 25L;
        var stages = new List<Stage>
        {
            CreateTestStage(venueId: venueId, name: "Main Stage"),
            CreateTestStage(venueId: venueId, name: "Second Stage")
        };

        _mockStageRepo.Setup(r => r.GetByVenueAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stages);

        // Act
        var result = await _sut.GetStagesByVenueAsync(venueId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateStageAsync_WithValidRequest_CreatesStage()
    {
        // Arrange
        var venueId = 26L;
        var festivalId = 27L;
        var userId = 28L;
        var request = new CreateStageRequest(
            Name: "Main Stage",
            Description: "The biggest stage");

        _mockVenueRepo.Setup(r => r.GetFestivalIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockStageRepo.Setup(r => r.CreateAsync(It.IsAny<Stage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(102L);

        // Act
        var result = await _sut.CreateStageAsync(venueId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);

        _mockStageRepo.Verify(r => r.CreateAsync(
            It.Is<Stage>(s => s.Name == request.Name && s.VenueId == venueId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStageAsync_WithNonExistentVenue_ThrowsVenueNotFoundException()
    {
        // Arrange
        var venueId = 29L;
        var userId = 30L;
        var request = new CreateStageRequest(Name: "Test Stage", Description: null);

        _mockVenueRepo.Setup(r => r.GetFestivalIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        // Act
        var act = () => _sut.CreateStageAsync(venueId, userId, request);

        // Assert
        await act.Should().ThrowAsync<VenueNotFoundException>();
    }

    [Fact]
    public async Task CreateStageAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var venueId = 31L;
        var festivalId = 32L;
        var userId = 33L;
        var request = new CreateStageRequest(Name: "Test Stage", Description: null);

        _mockVenueRepo.Setup(r => r.GetFestivalIdAsync(venueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Venues, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateStageAsync(venueId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Helper Methods

    private Venue CreateTestVenue(long? venueId = null, long? festivalId = null, string? name = null)
    {
        return new Venue
        {
            VenueId = venueId ?? 0L,
            FestivalId = festivalId ?? 0L,
            Name = name ?? "Test Venue",
            Description = "Test Description",
            Address = "123 Test Lane",
            Latitude = 34.0522m,
            Longitude = -118.2437m,
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    private Stage CreateTestStage(long? stageId = null, long? venueId = null, string? name = null)
    {
        return new Stage
        {
            StageId = stageId ?? 0L,
            VenueId = venueId ?? 0L,
            Name = name ?? "Test Stage",
            Description = "Test Stage Description",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    #endregion
}

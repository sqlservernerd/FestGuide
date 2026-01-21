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

public class FestivalServiceTests
{
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IFestivalPermissionRepository> _mockPermissionRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<FestivalService>> _mockLogger;
    private readonly FestivalService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public FestivalServiceTests()
    {
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockPermissionRepo = new Mock<IFestivalPermissionRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<FestivalService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new FestivalService(
            _mockFestivalRepo.Object,
            _mockPermissionRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsFestival()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var festival = CreateTestFestival(festivalId);

        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);

        // Act
        var result = await _sut.GetByIdAsync(festivalId);

        // Assert
        result.Should().NotBeNull();
        result.FestivalId.Should().Be(festivalId);
        result.Name.Should().Be(festival.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsFestivalNotFoundException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();

        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Festival?)null);

        // Act
        var act = () => _sut.GetByIdAsync(festivalId);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>();
    }

    #endregion

    #region GetMyFestivalsAsync Tests

    [Fact]
    public async Task GetMyFestivalsAsync_WithValidUserId_ReturnsFestivals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var festivals = new List<Festival>
        {
            CreateTestFestival(Guid.NewGuid(), userId),
            CreateTestFestival(Guid.NewGuid(), userId)
        };

        _mockFestivalRepo.Setup(r => r.GetByUserAccessAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivals);

        // Act
        var result = await _sut.GetMyFestivalsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(f => f.IsOwner.Should().BeTrue());
    }

    [Fact]
    public async Task GetMyFestivalsAsync_WithNoFestivals_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockFestivalRepo.Setup(r => r.GetByUserAccessAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Festival>());

        // Act
        var result = await _sut.GetMyFestivalsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithMatchingTerm_ReturnsFestivals()
    {
        // Arrange
        var searchTerm = "Summer";
        var festivals = new List<Festival>
        {
            CreateTestFestival(Guid.NewGuid(), name: "Summer Fest"),
            CreateTestFestival(Guid.NewGuid(), name: "Summer Music Festival")
        };

        _mockFestivalRepo.Setup(r => r.SearchByNameAsync(searchTerm, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivals);

        // Act
        var result = await _sut.SearchAsync(searchTerm);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesFestivalAndOwnerPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateFestivalRequest(
            Name: "New Festival",
            Description: "A great festival",
            ImageUrl: "https://example.com/image.jpg",
            WebsiteUrl: "https://example.com");

        _mockFestivalRepo.Setup(r => r.CreateAsync(It.IsAny<Festival>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockPermissionRepo.Setup(r => r.CreateAsync(It.IsAny<FestivalPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.OwnerUserId.Should().Be(userId);

        _mockFestivalRepo.Verify(r => r.CreateAsync(
            It.Is<Festival>(f => f.Name == request.Name && f.OwnerUserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockPermissionRepo.Verify(r => r.CreateAsync(
            It.Is<FestivalPermission>(p => p.UserId == userId && p.Role == FestivalRole.Owner),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesFestival()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var festival = CreateTestFestival(festivalId, userId);
        var request = new UpdateFestivalRequest(
            Name: "Updated Name",
            Description: "Updated Description",
            ImageUrl: null,
            WebsiteUrl: null);

        _mockAuthService.Setup(a => a.CanEditFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);

        // Act
        var result = await _sut.UpdateAsync(festivalId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);

        _mockFestivalRepo.Verify(r => r.UpdateAsync(
            It.Is<Festival>(f => f.Name == request.Name),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateFestivalRequest(Name: "Updated", Description: null, ImageUrl: null, WebsiteUrl: null);

        _mockAuthService.Setup(a => a.CanEditFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.UpdateAsync(festivalId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentFestival_ThrowsFestivalNotFoundException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateFestivalRequest(Name: "Updated", Description: null, ImageUrl: null, WebsiteUrl: null);

        _mockAuthService.Setup(a => a.CanEditFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Festival?)null);

        // Act
        var act = () => _sut.UpdateAsync(festivalId, userId, request);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPermission_DeletesFestival()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockAuthService.Setup(a => a.CanDeleteFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(festivalId, userId);

        // Assert
        _mockFestivalRepo.Verify(r => r.DeleteAsync(festivalId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockAuthService.Setup(a => a.CanDeleteFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.DeleteAsync(festivalId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentFestival_ThrowsFestivalNotFoundException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockAuthService.Setup(a => a.CanDeleteFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.DeleteAsync(festivalId, userId);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>();
    }

    #endregion

    #region TransferOwnershipAsync Tests

    [Fact]
    public async Task TransferOwnershipAsync_AsOwner_TransfersOwnership()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var currentOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var festival = CreateTestFestival(festivalId, currentOwnerId);
        var request = new TransferOwnershipRequest(newOwnerId);

        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);

        // Act
        await _sut.TransferOwnershipAsync(festivalId, currentOwnerId, request);

        // Assert
        _mockFestivalRepo.Verify(r => r.TransferOwnershipAsync(festivalId, newOwnerId, currentOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockPermissionRepo.Verify(r => r.TransferOwnershipAsync(festivalId, currentOwnerId, newOwnerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferOwnershipAsync_NotAsOwner_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var currentOwnerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var festival = CreateTestFestival(festivalId, currentOwnerId);
        var request = new TransferOwnershipRequest(newOwnerId);

        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);

        // Act
        var act = () => _sut.TransferOwnershipAsync(festivalId, differentUserId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*owner*");
    }

    [Fact]
    public async Task TransferOwnershipAsync_WithNonExistentFestival_ThrowsFestivalNotFoundException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new TransferOwnershipRequest(Guid.NewGuid());

        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Festival?)null);

        // Act
        var act = () => _sut.TransferOwnershipAsync(festivalId, userId, request);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>();
    }

    #endregion

    #region Helper Methods

    private Festival CreateTestFestival(Guid? festivalId = null, Guid? ownerId = null, string? name = null)
    {
        return new Festival
        {
            FestivalId = festivalId ?? Guid.NewGuid(),
            Name = name ?? "Test Festival",
            Description = "Test Description",
            ImageUrl = "https://example.com/image.jpg",
            WebsiteUrl = "https://example.com",
            OwnerUserId = ownerId ?? Guid.NewGuid(),
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = ownerId ?? Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = ownerId ?? Guid.NewGuid()
        };
    }

    #endregion
}

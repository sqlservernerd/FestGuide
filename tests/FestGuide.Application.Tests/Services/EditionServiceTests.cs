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

public class EditionServiceTests
{
    private readonly Mock<IEditionRepository> _mockEditionRepo;
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<EditionService>> _mockLogger;
    private readonly EditionService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public EditionServiceTests()
    {
        _mockEditionRepo = new Mock<IEditionRepository>();
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<EditionService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new EditionService(
            _mockEditionRepo.Object,
            _mockFestivalRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEdition()
    {
        // Arrange
        var editionId = 1L;
        var edition = CreateTestEdition(editionId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);

        // Act
        var result = await _sut.GetByIdAsync(editionId);

        // Assert
        result.Should().NotBeNull();
        result.EditionId.Should().Be(editionId);
        result.Name.Should().Be(edition.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = 2L;

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalEdition?)null);

        // Act
        var act = () => _sut.GetByIdAsync(editionId);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    #endregion

    #region GetByFestivalAsync Tests

    [Fact]
    public async Task GetByFestivalAsync_WithValidFestivalId_ReturnsEditions()
    {
        // Arrange
        var festivalId = 3L;
        var editions = new List<FestivalEdition>
        {
            CreateTestEdition(festivalId: festivalId, name: "2025 Edition"),
            CreateTestEdition(festivalId: festivalId, name: "2026 Edition")
        };

        _mockEditionRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(editions);

        // Act
        var result = await _sut.GetByFestivalAsync(festivalId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByFestivalAsync_WithNoEditions_ReturnsEmptyList()
    {
        // Arrange
        var festivalId = 4L;

        _mockEditionRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FestivalEdition>());

        // Act
        var result = await _sut.GetByFestivalAsync(festivalId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPublishedByFestivalAsync Tests

    [Fact]
    public async Task GetPublishedByFestivalAsync_ReturnsOnlyPublishedEditions()
    {
        // Arrange
        var festivalId = 5L;
        var editions = new List<FestivalEdition>
        {
            CreateTestEdition(festivalId: festivalId, status: EditionStatus.Published)
        };

        _mockEditionRepo.Setup(r => r.GetPublishedByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(editions);

        // Act
        var result = await _sut.GetPublishedByFestivalAsync(festivalId);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesEdition()
    {
        // Arrange
        var festivalId = 6L;
        var userId = 7L;
        var request = new CreateEditionRequest(
            Name: "Summer Festival 2026",
            StartDateUtc: _now.AddMonths(6),
            EndDateUtc: _now.AddMonths(6).AddDays(3),
            TimezoneId: "America/Los_Angeles",
            TicketUrl: "https://tickets.example.com");

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEditionRepo.Setup(r => r.CreateAsync(It.IsAny<FestivalEdition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101L);

        // Act
        var result = await _sut.CreateAsync(festivalId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.FestivalId.Should().Be(festivalId);
        result.TimezoneId.Should().Be(request.TimezoneId);

        _mockEditionRepo.Verify(r => r.CreateAsync(
            It.Is<FestivalEdition>(e => e.Name == request.Name && e.FestivalId == festivalId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = 8L;
        var userId = 9L;
        var request = new CreateEditionRequest(
            Name: "Test", StartDateUtc: _now, EndDateUtc: _now.AddDays(1),
            TimezoneId: "UTC", TicketUrl: null);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
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
        var festivalId = 10L;
        var userId = 11L;
        var request = new CreateEditionRequest(
            Name: "Test", StartDateUtc: _now, EndDateUtc: _now.AddDays(1),
            TimezoneId: "UTC", TicketUrl: null);

        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
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
    public async Task UpdateAsync_WithValidRequest_UpdatesEdition()
    {
        // Arrange
        var editionId = 12L;
        var festivalId = 13L;
        var userId = 14L;
        var edition = CreateTestEdition(editionId, festivalId);
        var request = new UpdateEditionRequest(
            Name: "Updated Edition Name",
            StartDateUtc: null,
            EndDateUtc: null,
            TimezoneId: null,
            TicketUrl: null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateAsync(editionId, userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockEditionRepo.Verify(r => r.UpdateAsync(It.IsAny<FestivalEdition>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = 15L;
        var userId = 16L;
        var request = new UpdateEditionRequest(Name: "Updated", StartDateUtc: null, EndDateUtc: null, TimezoneId: null, TicketUrl: null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalEdition?)null);

        // Act
        var act = () => _sut.UpdateAsync(editionId, userId, request);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = 17L;
        var festivalId = 18L;
        var userId = 19L;
        var edition = CreateTestEdition(editionId, festivalId);
        var request = new UpdateEditionRequest(Name: "Updated", StartDateUtc: null, EndDateUtc: null, TimezoneId: null, TicketUrl: null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.UpdateAsync(editionId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPermission_DeletesEdition()
    {
        // Arrange
        var editionId = 20L;
        var festivalId = 21L;
        var userId = 22L;
        var edition = CreateTestEdition(editionId, festivalId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(editionId, userId);

        // Assert
        _mockEditionRepo.Verify(r => r.DeleteAsync(editionId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = 23L;
        var userId = 24L;

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalEdition?)null);

        // Act
        var act = () => _sut.DeleteAsync(editionId, userId);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = 25L;
        var festivalId = 26L;
        var userId = 27L;
        var edition = CreateTestEdition(editionId, festivalId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.HasScopeAsync(userId, festivalId, PermissionScope.Editions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.DeleteAsync(editionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Helper Methods

    private FestivalEdition CreateTestEdition(
        long? editionId = null,
        long? festivalId = null,
        string? name = null,
        EditionStatus status = EditionStatus.Draft)
    {
        return new FestivalEdition
        {
            EditionId = editionId ?? 0L,
            FestivalId = festivalId ?? 0L,
            Name = name ?? "Test Edition 2026",
            StartDateUtc = _now.AddMonths(6),
            EndDateUtc = _now.AddMonths(6).AddDays(3),
            TimezoneId = "America/Los_Angeles",
            TicketUrl = "https://tickets.example.com",
            Status = status,
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    #endregion
}

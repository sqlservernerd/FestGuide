using FluentAssertions;
using Moq;
using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IAnalyticsRepository> _mockAnalyticsRepo;
    private readonly Mock<IEditionRepository> _mockEditionRepo;
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IEngagementRepository> _mockEngagementRepo;
    private readonly Mock<IArtistRepository> _mockArtistRepo;
    private readonly Mock<ITimeSlotRepository> _mockTimeSlotRepo;
    private readonly Mock<IStageRepository> _mockStageRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<AnalyticsService>> _mockLogger;
    private readonly AnalyticsService _sut;
    private readonly DateTime _now = DateTime.UtcNow;

    public AnalyticsServiceTests()
    {
        _mockAnalyticsRepo = new Mock<IAnalyticsRepository>();
        _mockEditionRepo = new Mock<IEditionRepository>();
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockEngagementRepo = new Mock<IEngagementRepository>();
        _mockArtistRepo = new Mock<IArtistRepository>();
        _mockTimeSlotRepo = new Mock<ITimeSlotRepository>();
        _mockStageRepo = new Mock<IStageRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<AnalyticsService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new AnalyticsService(
            _mockAnalyticsRepo.Object,
            _mockEditionRepo.Object,
            _mockFestivalRepo.Object,
            _mockEngagementRepo.Object,
            _mockArtistRepo.Object,
            _mockTimeSlotRepo.Object,
            _mockStageRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region TrackEventAsync Tests

    [Fact]
    public async Task TrackEventAsync_WithValidRequest_RecordsEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var request = new TrackEventRequest(
            EventType: "schedule_view",
            EditionId: editionId,
            EntityType: "Schedule",
            EntityId: null,
            Platform: "ios",
            SessionId: "session-123",
            Metadata: null);

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAnalyticsRepo.Setup(r => r.RecordEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.TrackEventAsync(userId, request);

        // Assert
        _mockAnalyticsRepo.Verify(r => r.RecordEventAsync(
            It.Is<AnalyticsEvent>(e =>
                e.UserId == userId &&
                e.EditionId == editionId &&
                e.FestivalId == festivalId &&
                e.EventType == "schedule_view" &&
                e.Platform == "ios"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackScheduleViewAsync_RecordsScheduleViewEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAnalyticsRepo.Setup(r => r.RecordEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.TrackScheduleViewAsync(userId, editionId, "android", "session-456");

        // Assert
        _mockAnalyticsRepo.Verify(r => r.RecordEventAsync(
            It.Is<AnalyticsEvent>(e => e.EventType == "schedule_view" && e.EntityType == "Schedule"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackEngagementSaveAsync_RecordsEngagementSaveEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var engagementId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAnalyticsRepo.Setup(r => r.RecordEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.TrackEngagementSaveAsync(userId, editionId, engagementId);

        // Assert
        _mockAnalyticsRepo.Verify(r => r.RecordEventAsync(
            It.Is<AnalyticsEvent>(e =>
                e.EventType == "engagement_save" &&
                e.EntityType == "Engagement" &&
                e.EntityId == engagementId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetEditionDashboardAsync Tests

    [Fact]
    public async Task GetEditionDashboardAsync_WithValidPermission_ReturnsDashboard()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId, Name = "2026 Edition" };
        var festival = new Festival { FestivalId = festivalId, Name = "Summer Festival" };

        SetupEditionAndFestival(edition, festival);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupEmptyAnalyticsData(editionId);

        // Act
        var result = await _sut.GetEditionDashboardAsync(editionId, organizerId);

        // Assert
        result.Should().NotBeNull();
        result.EditionId.Should().Be(editionId);
        result.EditionName.Should().Be("2026 Edition");
        result.FestivalName.Should().Be("Summer Festival");
    }

    [Fact]
    public async Task GetEditionDashboardAsync_WithNoPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.GetEditionDashboardAsync(editionId, organizerId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetEditionDashboardAsync_WithNonExistentEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalEdition?)null);

        // Act
        var act = () => _sut.GetEditionDashboardAsync(editionId, organizerId);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    [Fact]
    public async Task GetEditionDashboardAsync_WithAnalyticsData_ReturnsPopulatedDashboard()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId, Name = "2026" };
        var festival = new Festival { FestivalId = festivalId, Name = "Festival" };

        SetupEditionAndFestival(edition, festival);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAnalyticsRepo.Setup(r => r.GetScheduleViewCountAsync(editionId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1500);
        _mockAnalyticsRepo.Setup(r => r.GetUniqueViewerCountAsync(editionId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);
        _mockAnalyticsRepo.Setup(r => r.GetPersonalScheduleCountAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(200);
        _mockAnalyticsRepo.Setup(r => r.GetTotalEngagementSavesAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(800);
        _mockAnalyticsRepo.Setup(r => r.GetTopArtistsAsync(editionId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string, int)>());
        _mockAnalyticsRepo.Setup(r => r.GetTopSavedEngagementsAsync(editionId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, int)>());
        _mockAnalyticsRepo.Setup(r => r.GetPlatformDistributionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, int)> { ("ios", 300), ("android", 200) });

        // Act
        var result = await _sut.GetEditionDashboardAsync(editionId, organizerId);

        // Assert
        result.TotalScheduleViews.Should().Be(1500);
        result.UniqueViewers.Should().Be(500);
        result.PersonalSchedulesCreated.Should().Be(200);
        result.TotalEngagementSaves.Should().Be(800);
        result.PlatformDistribution.Should().HaveCount(2);
    }

    #endregion

    #region GetFestivalSummaryAsync Tests

    [Fact]
    public async Task GetFestivalSummaryAsync_WithValidPermission_ReturnsSummary()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var festival = new Festival { FestivalId = festivalId, Name = "Test Festival" };

        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);
        _mockEditionRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FestivalEdition>());

        // Act
        var result = await _sut.GetFestivalSummaryAsync(festivalId, organizerId);

        // Assert
        result.Should().NotBeNull();
        result.FestivalId.Should().Be(festivalId);
        result.FestivalName.Should().Be("Test Festival");
    }

    [Fact]
    public async Task GetFestivalSummaryAsync_WithEditions_AggregatesMetrics()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition1Id = Guid.NewGuid();
        var edition2Id = Guid.NewGuid();

        var festival = new Festival { FestivalId = festivalId, Name = "Festival" };
        var editions = new List<FestivalEdition>
        {
            new() { EditionId = edition1Id, FestivalId = festivalId, Name = "2025" },
            new() { EditionId = edition2Id, FestivalId = festivalId, Name = "2026" }
        };

        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);
        _mockEditionRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(editions);

        _mockAnalyticsRepo.Setup(r => r.GetScheduleViewCountAsync(edition1Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        _mockAnalyticsRepo.Setup(r => r.GetScheduleViewCountAsync(edition2Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(200);
        _mockAnalyticsRepo.Setup(r => r.GetPersonalScheduleCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);
        _mockAnalyticsRepo.Setup(r => r.GetTotalEngagementSavesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        // Act
        var result = await _sut.GetFestivalSummaryAsync(festivalId, organizerId);

        // Assert
        result.TotalEditions.Should().Be(2);
        result.TotalScheduleViews.Should().Be(300); // 100 + 200
        result.EditionMetrics.Should().HaveCount(2);
    }

    #endregion

    #region GetTopArtistsAsync Tests

    [Fact]
    public async Task GetTopArtistsAsync_WithValidPermission_ReturnsTopArtists()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var artistId = Guid.NewGuid();

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId };
        var artist = new Artist { ArtistId = artistId, Name = "Top Artist", ImageUrl = "http://img.com/artist.jpg" };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockAnalyticsRepo.Setup(r => r.GetTopArtistsAsync(editionId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string, int)> { (artistId, "Top Artist", 150) });
        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);

        // Act
        var result = await _sut.GetTopArtistsAsync(editionId, organizerId);

        // Assert
        result.Should().HaveCount(1);
        result[0].ArtistName.Should().Be("Top Artist");
        result[0].SaveCount.Should().Be(150);
        result[0].Rank.Should().Be(1);
    }

    #endregion

    #region GetPlatformDistributionAsync Tests

    [Fact]
    public async Task GetPlatformDistributionAsync_CalculatesPercentages()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockAnalyticsRepo.Setup(r => r.GetPlatformDistributionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, int)> { ("ios", 60), ("android", 40) });

        // Act
        var result = await _sut.GetPlatformDistributionAsync(editionId, organizerId);

        // Assert
        result.Should().HaveCount(2);
        result.First(p => p.Platform == "ios").Percentage.Should().Be(60);
        result.First(p => p.Platform == "android").Percentage.Should().Be(40);
    }

    #endregion

    #region Helper Methods

    private void SetupEditionAndFestival(FestivalEdition edition, Festival festival)
    {
        _mockEditionRepo.Setup(r => r.GetByIdAsync(edition.EditionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festival.FestivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);
    }

    private void SetupEmptyAnalyticsData(Guid editionId)
    {
        _mockAnalyticsRepo.Setup(r => r.GetScheduleViewCountAsync(editionId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockAnalyticsRepo.Setup(r => r.GetUniqueViewerCountAsync(editionId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockAnalyticsRepo.Setup(r => r.GetPersonalScheduleCountAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockAnalyticsRepo.Setup(r => r.GetTotalEngagementSavesAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockAnalyticsRepo.Setup(r => r.GetTopArtistsAsync(editionId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string, int)>());
        _mockAnalyticsRepo.Setup(r => r.GetTopSavedEngagementsAsync(editionId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, int)>());
        _mockAnalyticsRepo.Setup(r => r.GetPlatformDistributionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, int)>());
    }

    #endregion
}

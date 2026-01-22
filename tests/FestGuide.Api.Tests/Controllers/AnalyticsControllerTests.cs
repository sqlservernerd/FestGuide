using System.Security.Claims;
using FluentAssertions;
using Moq;
using FestGuide.Api.Controllers;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FestGuide.Api.Tests.Controllers;

public class AnalyticsControllerTests
{
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly Mock<ILogger<AnalyticsController>> _mockLogger;
    private readonly AnalyticsController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AnalyticsControllerTests()
    {
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _mockLogger = new Mock<ILogger<AnalyticsController>>();

        _sut = new AnalyticsController(
            _mockAnalyticsService.Object,
            _mockLogger.Object);

        SetupUserContext();
    }

    #region TrackEvent Tests

    [Fact]
    public async Task TrackEvent_WithValidRequest_Returns204NoContent()
    {
        // Arrange
        var request = new TrackEventRequest(
            "schedule_view",
            Guid.NewGuid(),
            null,
            null,
            "iOS",
            Guid.NewGuid().ToString(),
            null);

        // Act
        var result = await _sut.TrackEvent(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockAnalyticsService.Verify(s => s.TrackEventAsync(
            It.IsAny<Guid?>(),
            request,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackEvent_WithoutAuthentication_AllowsAnonymous()
    {
        // Arrange
        var request = new TrackEventRequest(
            "schedule_view",
            Guid.NewGuid(),
            null,
            null,
            "iOS",
            Guid.NewGuid().ToString(),
            null);

        // Act
        var result = await _sut.TrackEvent(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region GetEditionDashboard Tests

    [Fact]
    public async Task GetEditionDashboard_WithValidEditionId_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var dashboard = new EditionDashboardDto(
            editionId,
            "Summer Festival 2026",
            "Summer Music Fest",
            100,
            50,
            25,
            75,
            new List<TopArtistDto>(),
            new List<TopEngagementDto>(),
            new List<PlatformDistributionDto>());

        _mockAnalyticsService.Setup(s => s.GetEditionDashboardAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _sut.GetEditionDashboard(editionId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<EditionDashboardDto>>().Subject;
        response.Data.EditionId.Should().Be(editionId);
    }

    [Fact]
    public async Task GetEditionDashboard_WithNonExistentEdition_Returns404NotFound()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockAnalyticsService.Setup(s => s.GetEditionDashboardAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EditionNotFoundException(editionId));

        // Act
        var result = await _sut.GetEditionDashboard(editionId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("EDITION_NOT_FOUND");
    }

    [Fact]
    public async Task GetEditionDashboard_WithoutPermission_Returns403Forbidden()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockAnalyticsService.Setup(s => s.GetEditionDashboardAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Access denied"));

        // Act
        var result = await _sut.GetEditionDashboard(editionId, CancellationToken.None);

        // Assert
        var forbiddenResult = result.Should().BeOfType<ObjectResult>().Subject;
        forbiddenResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var error = forbiddenResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task GetEditionDashboard_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var controller = new AnalyticsController(_mockAnalyticsService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.GetEditionDashboard(editionId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region GetFestivalSummary Tests

    [Fact]
    public async Task GetFestivalSummary_WithValidFestivalId_Returns200Ok()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var summary = new FestivalAnalyticsSummaryDto(
            festivalId,
            "Summer Music Fest",
            3,
            500,
            100,
            200,
            new List<EditionMetricsSummaryDto>());

        _mockAnalyticsService.Setup(s => s.GetFestivalSummaryAsync(festivalId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _sut.GetFestivalSummary(festivalId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<FestivalAnalyticsSummaryDto>>().Subject;
        response.Data.FestivalId.Should().Be(festivalId);
    }

    [Fact]
    public async Task GetFestivalSummary_WithNonExistentFestival_Returns404NotFound()
    {
        // Arrange
        var festivalId = Guid.NewGuid();

        _mockAnalyticsService.Setup(s => s.GetFestivalSummaryAsync(festivalId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FestivalNotFoundException(festivalId));

        // Act
        var result = await _sut.GetFestivalSummary(festivalId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("FESTIVAL_NOT_FOUND");
    }

    #endregion

    #region GetTopArtists Tests

    [Fact]
    public async Task GetTopArtists_WithValidEditionId_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var artists = new List<ArtistAnalyticsDto>
        {
            new(Guid.NewGuid(), "Artist One", "image1.jpg", 100, 1, 0.5m),
            new(Guid.NewGuid(), "Artist Two", "image2.jpg", 75, 2, 0.3m)
        };

        _mockAnalyticsService.Setup(s => s.GetTopArtistsAsync(editionId, _userId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Act
        var result = await _sut.GetTopArtists(editionId, 10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<ArtistAnalyticsDto>>>().Subject;
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTopArtists_WithoutPermission_Returns403Forbidden()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockAnalyticsService.Setup(s => s.GetTopArtistsAsync(editionId, _userId, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Access denied"));

        // Act
        var result = await _sut.GetTopArtists(editionId, 10, CancellationToken.None);

        // Assert
        var forbiddenResult = result.Should().BeOfType<ObjectResult>().Subject;
        forbiddenResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    #endregion

    #region GetTopEngagements Tests

    [Fact]
    public async Task GetTopEngagements_WithValidEditionId_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var engagements = new List<EngagementAnalyticsDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Artist One", Guid.NewGuid(), "Main Stage", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 50, 1)
        };

        _mockAnalyticsService.Setup(s => s.GetTopEngagementsAsync(editionId, _userId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagements);

        // Act
        var result = await _sut.GetTopEngagements(editionId, 10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<EngagementAnalyticsDto>>>().Subject;
        response.Data.Should().HaveCount(1);
    }

    #endregion

    #region GetEventTimeline Tests

    [Fact]
    public async Task GetEventTimeline_WithValidParameters_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var fromUtc = DateTime.UtcNow.AddDays(-7);
        var toUtc = DateTime.UtcNow;
        var timeline = new List<TimelineDataPointDto>
        {
            new(DateTime.UtcNow.AddDays(-1), 10),
            new(DateTime.UtcNow, 15)
        };

        _mockAnalyticsService.Setup(s => s.GetEventTimelineAsync(
            editionId,
            _userId,
            It.IsAny<TimelineRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeline);

        // Act
        var result = await _sut.GetEventTimeline(editionId, fromUtc, toUtc, "schedule_view", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<TimelineDataPointDto>>>().Subject;
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventTimeline_WithoutEventType_UsesNull()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var fromUtc = DateTime.UtcNow.AddDays(-7);
        var toUtc = DateTime.UtcNow;

        _mockAnalyticsService.Setup(s => s.GetEventTimelineAsync(
            editionId,
            _userId,
            It.Is<TimelineRequest>(r => r.EventType == null),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimelineDataPointDto>());

        // Act
        var result = await _sut.GetEventTimeline(editionId, fromUtc, toUtc, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetDailyActiveUsers Tests

    [Fact]
    public async Task GetDailyActiveUsers_WithDateRange_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var fromUtc = DateTime.UtcNow.AddDays(-7);
        var toUtc = DateTime.UtcNow;
        var dau = new List<DailyActiveUsersDto>
        {
            new(DateTime.UtcNow.AddDays(-1).Date, 20),
            new(DateTime.UtcNow.Date, 25)
        };

        _mockAnalyticsService.Setup(s => s.GetDailyActiveUsersAsync(editionId, _userId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dau);

        // Act
        var result = await _sut.GetDailyActiveUsers(editionId, fromUtc, toUtc, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<DailyActiveUsersDto>>>().Subject;
        response.Data.Should().HaveCount(2);
    }

    #endregion

    #region GetPlatformDistribution Tests

    [Fact]
    public async Task GetPlatformDistribution_WithValidEditionId_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var platforms = new List<PlatformDistributionDto>
        {
            new("iOS", 100, 0.5m),
            new("Android", 80, 0.4m),
            new("Web", 20, 0.1m)
        };

        _mockAnalyticsService.Setup(s => s.GetPlatformDistributionAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platforms);

        // Act
        var result = await _sut.GetPlatformDistribution(editionId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<PlatformDistributionDto>>>().Subject;
        response.Data.Should().HaveCount(3);
    }

    #endregion

    #region GetEventTypeDistribution Tests

    [Fact]
    public async Task GetEventTypeDistribution_WithValidEditionId_Returns200Ok()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var distribution = new List<EventTypeDistributionDto>
        {
            new("schedule_view", 100, 0.5m),
            new("engagement_save", 80, 0.4m)
        };

        _mockAnalyticsService.Setup(s => s.GetEventTypeDistributionAsync(
            editionId,
            _userId,
            null,
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(distribution);

        // Act
        var result = await _sut.GetEventTypeDistribution(editionId, null, null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<EventTypeDistributionDto>>>().Subject;
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventTypeDistribution_WithDateRange_PassesParameters()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var fromUtc = DateTime.UtcNow.AddDays(-7);
        var toUtc = DateTime.UtcNow;

        _mockAnalyticsService.Setup(s => s.GetEventTypeDistributionAsync(
            editionId,
            _userId,
            fromUtc,
            toUtc,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventTypeDistributionDto>());

        // Act
        var result = await _sut.GetEventTypeDistribution(editionId, fromUtc, toUtc, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.GetEventTypeDistributionAsync(
            editionId,
            _userId,
            fromUtc,
            toUtc,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupUserContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}

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

public class ExportServiceTests
{
    private readonly Mock<IEditionRepository> _mockEditionRepo;
    private readonly Mock<ITimeSlotRepository> _mockTimeSlotRepo;
    private readonly Mock<IEngagementRepository> _mockEngagementRepo;
    private readonly Mock<IArtistRepository> _mockArtistRepo;
    private readonly Mock<IStageRepository> _mockStageRepo;
    private readonly Mock<IAnalyticsRepository> _mockAnalyticsRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<ExportService>> _mockLogger;
    private readonly ExportService _sut;
    private readonly DateTime _now = DateTime.UtcNow;

    public ExportServiceTests()
    {
        _mockEditionRepo = new Mock<IEditionRepository>();
        _mockTimeSlotRepo = new Mock<ITimeSlotRepository>();
        _mockEngagementRepo = new Mock<IEngagementRepository>();
        _mockArtistRepo = new Mock<IArtistRepository>();
        _mockStageRepo = new Mock<IStageRepository>();
        _mockAnalyticsRepo = new Mock<IAnalyticsRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<ExportService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new ExportService(
            _mockEditionRepo.Object,
            _mockTimeSlotRepo.Object,
            _mockEngagementRepo.Object,
            _mockArtistRepo.Object,
            _mockStageRepo.Object,
            _mockAnalyticsRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullEditionRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ExportService(
            null!,
            _mockTimeSlotRepo.Object,
            _mockEngagementRepo.Object,
            _mockArtistRepo.Object,
            _mockStageRepo.Object,
            _mockAnalyticsRepo.Object,
            _mockAuthService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editionRepository");
    }

    #endregion

    #region ExportEditionDataAsync Tests

    [Fact]
    public async Task ExportEditionDataAsync_WithValidEdition_ReturnsExportResult()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var request = new ExportRequest("csv", true, true, true, null, null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(r => r.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot>());
        _mockArtistRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        SetupAnalyticsMocks(editionId);

        // Act
        var result = await _sut.ExportEditionDataAsync(editionId, organizerId, request);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Contain(edition.Name.Replace(" ", "_"));
        result.FileName.Should().EndWith(".csv");
        result.ContentType.Should().Be("text/csv");
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportEditionDataAsync_WithNonExistentEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var request = new ExportRequest("csv", true, true, true, null, null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalEdition?)null);

        // Act
        var act = () => _sut.ExportEditionDataAsync(editionId, organizerId, request);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    [Fact]
    public async Task ExportEditionDataAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var request = new ExportRequest("csv", true, true, true, null, null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.ExportEditionDataAsync(editionId, organizerId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ExportEditionDataAsync_WithOnlySchedule_ExportsOnlySchedule()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var request = new ExportRequest("csv", false, true, false, null, null);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(r => r.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot>());

        // Act
        var result = await _sut.ExportEditionDataAsync(editionId, organizerId, request);

        // Assert
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("=== SCHEDULE ===");
        csvContent.Should().NotContain("=== ARTISTS ===");
        csvContent.Should().NotContain("=== ANALYTICS ===");
    }

    #endregion

    #region ExportScheduleCsvAsync Tests

    [Fact]
    public async Task ExportScheduleCsvAsync_WithValidEdition_ReturnsCsvData()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(r => r.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlot>());

        // Act
        var result = await _sut.ExportScheduleCsvAsync(editionId, organizerId);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Contain("schedule");
        result.ContentType.Should().Be("text/csv");
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("TimeSlotId,StageId,StageName,StartTimeUtc,EndTimeUtc,ArtistId,ArtistName");
    }

    [Fact]
    public async Task ExportScheduleCsvAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.ExportScheduleCsvAsync(editionId, organizerId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region ExportArtistsCsvAsync Tests

    [Fact]
    public async Task ExportArtistsCsvAsync_WithValidEdition_ReturnsCsvData()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var artists = new List<Artist>
        {
            CreateTestArtist(festivalId, "Artist One"),
            CreateTestArtist(festivalId, "Artist Two")
        };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Act
        var result = await _sut.ExportArtistsCsvAsync(editionId, organizerId);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Contain("artists");
        result.ContentType.Should().Be("text/csv");
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("ArtistId,Name,Genre,Bio,WebsiteUrl,ImageUrl");
        csvContent.Should().Contain("Artist One");
        csvContent.Should().Contain("Artist Two");
    }

    [Fact]
    public async Task ExportArtistsCsvAsync_WithSpecialCharactersInData_EscapesCsv()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var artists = new List<Artist>
        {
            CreateTestArtist(festivalId, "Artist, with comma")
        };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(r => r.GetByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Act
        var result = await _sut.ExportArtistsCsvAsync(editionId, organizerId);

        // Assert
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("\"Artist, with comma\"");
    }

    #endregion

    #region ExportAnalyticsCsvAsync Tests

    [Fact]
    public async Task ExportAnalyticsCsvAsync_WithValidEdition_ReturnsCsvData()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupAnalyticsMocks(editionId);

        // Act
        var result = await _sut.ExportAnalyticsCsvAsync(editionId, organizerId, null, null);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Contain("analytics");
        result.ContentType.Should().Be("text/csv");
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("Metric,Value");
        csvContent.Should().Contain("Total Schedule Views");
        csvContent.Should().Contain("Platform,Count");
        csvContent.Should().Contain("Top Artists,SaveCount");
    }

    [Fact]
    public async Task ExportAnalyticsCsvAsync_WithDateRange_PassesDateRangeToRepository()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupAnalyticsMocks(editionId);

        // Act
        await _sut.ExportAnalyticsCsvAsync(editionId, organizerId, fromUtc, toUtc);

        // Assert
        _mockAnalyticsRepo.Verify(r => r.GetScheduleViewCountAsync(editionId, fromUtc, toUtc, It.IsAny<CancellationToken>()), Times.Once);
        _mockAnalyticsRepo.Verify(r => r.GetUniqueViewerCountAsync(editionId, fromUtc, toUtc, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ExportAttendeeSavesCsvAsync Tests

    [Fact]
    public async Task ExportAttendeeSavesCsvAsync_WithValidEdition_ReturnsCsvData()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);
        var engagementId = Guid.NewGuid();

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockAnalyticsRepo.Setup(r => r.GetTopSavedEngagementsAsync(editionId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, int)> { (engagementId, 42) });
        _mockEngagementRepo.Setup(r => r.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Engagement?)null);

        // Act
        var result = await _sut.ExportAttendeeSavesCsvAsync(editionId, organizerId);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Contain("attendee_saves");
        result.ContentType.Should().Be("text/csv");
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("EngagementId,ArtistName,StageName,StartTimeUtc,EndTimeUtc,SaveCount");
    }

    [Fact]
    public async Task ExportAttendeeSavesCsvAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var edition = CreateTestEdition(editionId, festivalId);

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.ExportAttendeeSavesCsvAsync(editionId, organizerId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Helper Methods

    private FestivalEdition CreateTestEdition(Guid editionId, Guid festivalId, string? name = null)
    {
        return new FestivalEdition
        {
            EditionId = editionId,
            FestivalId = festivalId,
            Name = name ?? "Test Edition 2026",
            StartDateUtc = _now.AddDays(30),
            EndDateUtc = _now.AddDays(32),
            TimezoneId = "America/New_York",
            Status = EditionStatus.Published,
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = Guid.NewGuid()
        };
    }

    private Artist CreateTestArtist(Guid festivalId, string? name = null)
    {
        return new Artist
        {
            ArtistId = Guid.NewGuid(),
            FestivalId = festivalId,
            Name = name ?? "Test Artist",
            Genre = "Rock",
            Bio = "Test bio",
            ImageUrl = "https://example.com/image.jpg",
            WebsiteUrl = "https://example.com",
            SpotifyUrl = "https://spotify.com/artist/test",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = Guid.NewGuid()
        };
    }

    private void SetupAnalyticsMocks(Guid editionId)
    {
        _mockAnalyticsRepo.Setup(r => r.GetScheduleViewCountAsync(editionId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        _mockAnalyticsRepo.Setup(r => r.GetUniqueViewerCountAsync(editionId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);
        _mockAnalyticsRepo.Setup(r => r.GetPersonalScheduleCountAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        _mockAnalyticsRepo.Setup(r => r.GetTotalEngagementSavesAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);
        _mockAnalyticsRepo.Setup(r => r.GetPlatformDistributionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, int)> { ("iOS", 30), ("Android", 20) });
        _mockAnalyticsRepo.Setup(r => r.GetTopArtistsAsync(editionId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string, int)> { (Guid.NewGuid(), "Artist One", 10) });
    }

    #endregion
}

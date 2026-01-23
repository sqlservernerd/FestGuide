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
        var editionId = 1L;
        var festivalId = 2L;
        var organizerId = 3L;
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
        var editionId = 4L;
        var organizerId = 5L;
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
        var editionId = 6L;
        var festivalId = 7L;
        var organizerId = 8L;
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
        var editionId = 9L;
        var festivalId = 10L;
        var organizerId = 11L;
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
        var editionId = 12L;
        var festivalId = 13L;
        var organizerId = 14L;
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
        var editionId = 15L;
        var festivalId = 16L;
        var organizerId = 17L;
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
        var editionId = 18L;
        var festivalId = 19L;
        var organizerId = 20L;
        var edition = CreateTestEdition(editionId, festivalId);
        var artists = new List<Artist>
        {
            CreateTestArtist(1L, "Artist One"),
            CreateTestArtist(1L, "Artist Two")
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
        var editionId = 21L;
        var festivalId = 22L;
        var organizerId = 23L;
        var edition = CreateTestEdition(editionId, festivalId);
        var artists = new List<Artist>
        {
            CreateTestArtist(1L, "Artist, with comma")
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
        var editionId = 24L;
        var festivalId = 25L;
        var organizerId = 26L;
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
        var editionId = 27L;
        var festivalId = 28L;
        var organizerId = 29L;
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
        var editionId = 30L;
        var festivalId = 31L;
        var organizerId = 32L;
        var edition = CreateTestEdition(editionId, festivalId);
        var engagementId = 33L;
        var artistId = 34L;
        var timeSlotId = 35L;
        var stageId = 36L;

        var artist = new Artist
        {
            ArtistId = artistId,
            FestivalId = festivalId,
            Name = "Test Artist",
            Genre = "Rock",
            Bio = "Test bio",
            ImageUrl = "https://example.com/image.jpg",
            WebsiteUrl = "https://example.com",
            SpotifyUrl = "https://spotify.com/artist/test",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };

        var stage = new Stage
        {
            StageId = stageId,
            VenueId = 1L,
            Name = "Main Stage",
            Description = "Main performance stage",
            SortOrder = 1,
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };

        var timeSlot = new TimeSlot
        {
            TimeSlotId = timeSlotId,
            EditionId = editionId,
            StageId = stageId,
            StartTimeUtc = _now.AddDays(1),
            EndTimeUtc = _now.AddDays(1).AddHours(1),
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };

        var engagement = new Engagement
        {
            EngagementId = engagementId,
            TimeSlotId = timeSlotId,
            ArtistId = artistId,
            Notes = null,
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };

        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockAuthService.Setup(a => a.CanViewAnalyticsAsync(organizerId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockAnalyticsRepo.Setup(r => r.GetTopSavedEngagementsAsync(editionId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(long, int)> { (engagementId, 42) });
        _mockEngagementRepo.Setup(r => r.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement);
        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockTimeSlotRepo.Setup(r => r.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlot);
        _mockStageRepo.Setup(r => r.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);

        // Act
        var result = await _sut.ExportAttendeeSavesCsvAsync(editionId, organizerId);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Contain("attendee_saves");
        result.ContentType.Should().Be("text/csv");
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Data);
        csvContent.Should().Contain("EngagementId,ArtistName,StageName,StartTimeUtc,EndTimeUtc,SaveCount");
        csvContent.Should().Contain("Test Artist");
        csvContent.Should().Contain("Main Stage");
    }

    [Fact]
    public async Task ExportAttendeeSavesCsvAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = 37L;
        var festivalId = 38L;
        var organizerId = 39L;
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

    private FestivalEdition CreateTestEdition(long editionId, long festivalId, string? name = null)
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
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    private Artist CreateTestArtist(long festivalId, string? name = null)
    {
        return new Artist
        {
            ArtistId = 1L,
            FestivalId = festivalId,
            Name = name ?? "Test Artist",
            Genre = "Rock",
            Bio = "Test bio",
            ImageUrl = "https://example.com/image.jpg",
            WebsiteUrl = "https://example.com",
            SpotifyUrl = "https://spotify.com/artist/test",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    private void SetupAnalyticsMocks(long editionId)
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
            .ReturnsAsync(new List<(long, string, int)> { (1L, "Artist One", 10) });
    }

    #endregion
}

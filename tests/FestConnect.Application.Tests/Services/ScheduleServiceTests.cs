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

public class ScheduleServiceTests
{
    private readonly Mock<IScheduleRepository> _mockScheduleRepo;
    private readonly Mock<ITimeSlotRepository> _mockTimeSlotRepo;
    private readonly Mock<IEngagementRepository> _mockEngagementRepo;
    private readonly Mock<IStageRepository> _mockStageRepo;
    private readonly Mock<IArtistRepository> _mockArtistRepo;
    private readonly Mock<IEditionRepository> _mockEditionRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<ScheduleService>> _mockLogger;
    private readonly ScheduleService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public ScheduleServiceTests()
    {
        _mockScheduleRepo = new Mock<IScheduleRepository>();
        _mockTimeSlotRepo = new Mock<ITimeSlotRepository>();
        _mockEngagementRepo = new Mock<IEngagementRepository>();
        _mockStageRepo = new Mock<IStageRepository>();
        _mockArtistRepo = new Mock<IArtistRepository>();
        _mockEditionRepo = new Mock<IEditionRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<ScheduleService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new ScheduleService(
            _mockScheduleRepo.Object,
            _mockTimeSlotRepo.Object,
            _mockEngagementRepo.Object,
            _mockStageRepo.Object,
            _mockArtistRepo.Object,
            _mockEditionRepo.Object,
            _mockAuthService.Object,
            _mockNotificationService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetScheduleDetailAsync_WithValidEdition_ReturnsScheduleDetail()
    {
        // Arrange
        var editionId = 1L;
        var scheduleId = 2L;
        var stage1Id = 3L;
        var stage2Id = 4L;
        var artist1Id = 5L;
        var artist2Id = 6L;
        var timeSlot1Id = 7L;
        var timeSlot2Id = 8L;

        var schedule = new Schedule
        {
            ScheduleId = scheduleId,
            EditionId = editionId,
            Version = 1,
            PublishedAtUtc = _now
        };

        var timeSlots = new List<TimeSlot>
        {
            new()
            {
                TimeSlotId = timeSlot1Id,
                StageId = stage1Id,
                StartTimeUtc = _now.AddHours(2),
                EndTimeUtc = _now.AddHours(3)
            },
            new()
            {
                TimeSlotId = timeSlot2Id,
                StageId = stage2Id,
                StartTimeUtc = _now.AddHours(3),
                EndTimeUtc = _now.AddHours(4)
            }
        };

        var engagements = new List<Engagement>
        {
            new()
            {
                EngagementId = 1L,
                TimeSlotId = timeSlot1Id,
                ArtistId = artist1Id,
                Notes = "Great performance"
            },
            new()
            {
                EngagementId = 2L,
                TimeSlotId = timeSlot2Id,
                ArtistId = artist2Id,
                Notes = "Acoustic set"
            }
        };

        var stages = new List<Stage>
        {
            new() { StageId = stage1Id, Name = "Main Stage", VenueId = 1L },
            new() { StageId = stage2Id, Name = "Acoustic Stage", VenueId = 1L }
        };

        var artists = new List<Artist>
        {
            new() { ArtistId = artist1Id, Name = "Artist 1", FestivalId = 1L },
            new() { ArtistId = artist2Id, Name = "Artist 2", FestivalId = 1L }
        };

        _mockEditionRepo.Setup(x => x.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockTimeSlotRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlots);
        _mockEngagementRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagements);
        _mockStageRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stages);
        _mockArtistRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Act
        var result = await _sut.GetScheduleDetailAsync(editionId);

        // Assert
        result.Should().NotBeNull();
        result.ScheduleId.Should().Be(scheduleId);
        result.EditionId.Should().Be(editionId);
        result.Items.Should().HaveCount(2);
        result.Items[0].StageName.Should().Be("Main Stage");
        result.Items[0].ArtistName.Should().Be("Artist 1");
        result.Items[1].StageName.Should().Be("Acoustic Stage");
        result.Items[1].ArtistName.Should().Be("Artist 2");
        
        // Verify batch fetches were used
        _mockStageRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockArtistRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleDetailAsync_WithInvalidEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = 9L;

        _mockEditionRepo.Setup(x => x.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.GetScheduleDetailAsync(editionId);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    [Fact]
    public async Task GetScheduleAsync_WithValidEdition_ReturnsSchedule()
    {
        // Arrange
        var editionId = 10L;
        var scheduleId = 11L;

        var schedule = new Schedule
        {
            ScheduleId = scheduleId,
            EditionId = editionId,
            Version = 1,
            PublishedAtUtc = _now
        };

        _mockEditionRepo.Setup(x => x.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var result = await _sut.GetScheduleAsync(editionId);

        // Assert
        result.Should().NotBeNull();
        result.ScheduleId.Should().Be(scheduleId);
        result.EditionId.Should().Be(editionId);
        result.Version.Should().Be(1);
        result.IsPublished.Should().BeTrue();
    }

    #region PublishScheduleAsync Tests

    [Fact]
    public async Task PublishScheduleAsync_WithValidPermission_PublishesSchedule()
    {
        // Arrange
        var editionId = 12L;
        var festivalId = 13L;
        var userId = 14L;
        var scheduleId = 15L;

        var schedule = new Schedule
        {
            ScheduleId = scheduleId,
            EditionId = editionId,
            Version = 1,
            PublishedAtUtc = _now
        };

        _mockEditionRepo.Setup(x => x.GetFestivalIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(x => x.GetOrCreateAsync(editionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockScheduleRepo.Setup(x => x.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);


        // Act
        var result = await _sut.PublishScheduleAsync(editionId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ScheduleId.Should().Be(scheduleId);
        result.IsPublished.Should().BeTrue();

        _mockScheduleRepo.Verify(x => x.PublishAsync(scheduleId, userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEditionRepo.Verify(x => x.UpdateStatusAsync(editionId, EditionStatus.Published, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishScheduleAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var editionId = 16L;
        var festivalId = 17L;
        var userId = 18L;

        _mockEditionRepo.Setup(x => x.GetFestivalIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.PublishScheduleAsync(editionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task PublishScheduleAsync_WithNonExistentEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = 19L;
        var userId = 20L;

        _mockEditionRepo.Setup(x => x.GetFestivalIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        // Act
        var act = () => _sut.PublishScheduleAsync(editionId, userId);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    #endregion

    #region TimeSlot Tests

    [Fact]
    public async Task GetTimeSlotByIdAsync_WithValidId_ReturnsTimeSlot()
    {
        // Arrange
        var timeSlotId = 21L;
        var timeSlot = CreateTestTimeSlot(timeSlotId);

        _mockTimeSlotRepo.Setup(x => x.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlot);

        // Act
        var result = await _sut.GetTimeSlotByIdAsync(timeSlotId);

        // Assert
        result.Should().NotBeNull();
        result.TimeSlotId.Should().Be(timeSlotId);
    }

    [Fact]
    public async Task GetTimeSlotByIdAsync_WithInvalidId_ThrowsTimeSlotNotFoundException()
    {
        // Arrange
        var timeSlotId = 22L;

        _mockTimeSlotRepo.Setup(x => x.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeSlot?)null);

        // Act
        var act = () => _sut.GetTimeSlotByIdAsync(timeSlotId);

        // Assert
        await act.Should().ThrowAsync<TimeSlotNotFoundException>();
    }

    [Fact]
    public async Task GetTimeSlotsByStageAsync_WithValidIds_ReturnsTimeSlots()
    {
        // Arrange
        var stageId = 23L;
        var editionId = 24L;
        var timeSlots = new List<TimeSlot>
        {
            CreateTestTimeSlot(stageId: stageId, editionId: editionId),
            CreateTestTimeSlot(stageId: stageId, editionId: editionId)
        };

        _mockTimeSlotRepo.Setup(x => x.GetByStageAndEditionAsync(stageId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlots);

        // Act
        var result = await _sut.GetTimeSlotsByStageAsync(stageId, editionId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateTimeSlotAsync_WithValidRequest_CreatesTimeSlot()
    {
        // Arrange
        var stageId = 25L;
        var festivalId = 26L;
        var editionId = 27L;
        var userId = 28L;
        var request = new CreateTimeSlotRequest(
            EditionId: editionId,
            StartTimeUtc: _now.AddHours(2),
            EndTimeUtc: _now.AddHours(3));

        _mockStageRepo.Setup(x => x.GetFestivalIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEditionRepo.Setup(x => x.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(x => x.HasOverlapAsync(stageId, editionId, request.StartTimeUtc, request.EndTimeUtc, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockTimeSlotRepo.Setup(x => x.CreateAsync(It.IsAny<TimeSlot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101L);

        // Act
        var result = await _sut.CreateTimeSlotAsync(stageId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.StageId.Should().Be(stageId);
        result.EditionId.Should().Be(editionId);

        _mockTimeSlotRepo.Verify(x => x.CreateAsync(
            It.Is<TimeSlot>(t => t.StageId == stageId && t.EditionId == editionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTimeSlotAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var stageId = 29L;
        var festivalId = 30L;
        var userId = 31L;
        var request = new CreateTimeSlotRequest(100L, _now, _now.AddHours(1));

        _mockStageRepo.Setup(x => x.GetFestivalIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateTimeSlotAsync(stageId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateTimeSlotAsync_WithNonExistentStage_ThrowsStageNotFoundException()
    {
        // Arrange
        var stageId = 32L;
        var userId = 33L;
        var request = new CreateTimeSlotRequest(101L, _now, _now.AddHours(1));

        _mockStageRepo.Setup(x => x.GetFestivalIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        // Act
        var act = () => _sut.CreateTimeSlotAsync(stageId, userId, request);

        // Assert
        await act.Should().ThrowAsync<StageNotFoundException>();
    }

    [Fact]
    public async Task CreateTimeSlotAsync_WithOverlappingTimeSlot_ThrowsValidationException()
    {
        // Arrange
        var stageId = 34L;
        var festivalId = 35L;
        var editionId = 36L;
        var userId = 37L;
        var request = new CreateTimeSlotRequest(editionId, _now.AddHours(2), _now.AddHours(3));

        _mockStageRepo.Setup(x => x.GetFestivalIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEditionRepo.Setup(x => x.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(x => x.HasOverlapAsync(stageId, editionId, request.StartTimeUtc, request.EndTimeUtc, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateTimeSlotAsync(stageId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*overlap*");
    }

    [Fact]
    public async Task UpdateTimeSlotAsync_WithValidRequest_UpdatesTimeSlot()
    {
        // Arrange
        var timeSlotId = 38L;
        var festivalId = 39L;
        var userId = 40L;
        var timeSlot = CreateTestTimeSlot(timeSlotId);
        var request = new UpdateTimeSlotRequest(
            StartTimeUtc: _now.AddHours(4),
            EndTimeUtc: _now.AddHours(5));

        _mockTimeSlotRepo.Setup(x => x.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlot);
        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(x => x.HasOverlapAsync(timeSlot.StageId, timeSlot.EditionId, request.StartTimeUtc!.Value, request.EndTimeUtc!.Value, timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UpdateTimeSlotAsync(timeSlotId, userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockTimeSlotRepo.Verify(x => x.UpdateAsync(It.IsAny<TimeSlot>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTimeSlotAsync_WithNonExistentTimeSlot_ThrowsTimeSlotNotFoundException()
    {
        // Arrange
        var timeSlotId = 41L;
        var userId = 42L;
        var request = new UpdateTimeSlotRequest(_now, _now.AddHours(1));

        _mockTimeSlotRepo.Setup(x => x.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeSlot?)null);

        // Act
        var act = () => _sut.UpdateTimeSlotAsync(timeSlotId, userId, request);

        // Assert
        await act.Should().ThrowAsync<TimeSlotNotFoundException>();
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_WithValidPermission_DeletesTimeSlot()
    {
        // Arrange
        var timeSlotId = 43L;
        var festivalId = 44L;
        var userId = 45L;

        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteTimeSlotAsync(timeSlotId, userId);

        // Assert
        _mockTimeSlotRepo.Verify(x => x.DeleteAsync(timeSlotId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTimeSlotAsync_WithNonExistentTimeSlot_ThrowsTimeSlotNotFoundException()
    {
        // Arrange
        var timeSlotId = 46L;
        var userId = 47L;

        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        // Act
        var act = () => _sut.DeleteTimeSlotAsync(timeSlotId, userId);

        // Assert
        await act.Should().ThrowAsync<TimeSlotNotFoundException>();
    }

    #endregion

    #region Engagement Tests

    [Fact]
    public async Task GetEngagementByIdAsync_WithValidId_ReturnsEngagement()
    {
        // Arrange
        var engagementId = 48L;
        var engagement = CreateTestEngagement(engagementId);

        _mockEngagementRepo.Setup(x => x.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement);

        // Act
        var result = await _sut.GetEngagementByIdAsync(engagementId);

        // Assert
        result.Should().NotBeNull();
        result.EngagementId.Should().Be(engagementId);
    }

    [Fact]
    public async Task GetEngagementByIdAsync_WithInvalidId_ThrowsEngagementNotFoundException()
    {
        // Arrange
        var engagementId = 49L;

        _mockEngagementRepo.Setup(x => x.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Engagement?)null);

        // Act
        var act = () => _sut.GetEngagementByIdAsync(engagementId);

        // Assert
        await act.Should().ThrowAsync<EngagementNotFoundException>();
    }

    [Fact]
    public async Task CreateEngagementAsync_WithValidRequest_CreatesEngagement()
    {
        // Arrange
        var timeSlotId = 50L;
        var festivalId = 51L;
        var artistId = 52L;
        var userId = 53L;
        var request = new CreateEngagementRequest(ArtistId: artistId, Notes: "Great set");

        _mockTimeSlotRepo.Setup(x => x.ExistsAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(x => x.ExistsAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEngagementRepo.Setup(x => x.TimeSlotHasEngagementAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockEngagementRepo.Setup(x => x.CreateAsync(It.IsAny<Engagement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(102L);

        // Act
        var result = await _sut.CreateEngagementAsync(timeSlotId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.TimeSlotId.Should().Be(timeSlotId);
        result.ArtistId.Should().Be(artistId);

        _mockEngagementRepo.Verify(x => x.CreateAsync(
            It.Is<Engagement>(e => e.TimeSlotId == timeSlotId && e.ArtistId == artistId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEngagementAsync_WithNonExistentTimeSlot_ThrowsTimeSlotNotFoundException()
    {
        // Arrange
        var timeSlotId = 54L;
        var userId = 55L;
        var request = new CreateEngagementRequest(ArtistId: 102L, Notes: null);

        _mockTimeSlotRepo.Setup(x => x.ExistsAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateEngagementAsync(timeSlotId, userId, request);

        // Assert
        await act.Should().ThrowAsync<TimeSlotNotFoundException>();
    }

    [Fact]
    public async Task CreateEngagementAsync_WithNonExistentArtist_ThrowsArtistNotFoundException()
    {
        // Arrange
        var timeSlotId = 56L;
        var festivalId = 57L;
        var artistId = 58L;
        var userId = 59L;
        var request = new CreateEngagementRequest(ArtistId: artistId, Notes: null);

        _mockTimeSlotRepo.Setup(x => x.ExistsAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(x => x.ExistsAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateEngagementAsync(timeSlotId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ArtistNotFoundException>();
    }

    [Fact]
    public async Task CreateEngagementAsync_WithTimeSlotAlreadyAssigned_ThrowsValidationException()
    {
        // Arrange
        var timeSlotId = 60L;
        var festivalId = 61L;
        var artistId = 62L;
        var userId = 63L;
        var request = new CreateEngagementRequest(ArtistId: artistId, Notes: null);

        _mockTimeSlotRepo.Setup(x => x.ExistsAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(x => x.ExistsAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEngagementRepo.Setup(x => x.TimeSlotHasEngagementAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateEngagementAsync(timeSlotId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already*");
    }

    [Fact]
    public async Task UpdateEngagementAsync_WithValidRequest_UpdatesEngagement()
    {
        // Arrange
        var engagementId = 64L;
        var festivalId = 65L;
        var newArtistId = 66L;
        var userId = 67L;
        var engagement = CreateTestEngagement(engagementId);
        var request = new UpdateEngagementRequest(ArtistId: newArtistId, Notes: "Updated notes");

        _mockEngagementRepo.Setup(x => x.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement);
        _mockEngagementRepo.Setup(x => x.GetFestivalIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockArtistRepo.Setup(x => x.ExistsAsync(newArtistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateEngagementAsync(engagementId, userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockEngagementRepo.Verify(x => x.UpdateAsync(It.IsAny<Engagement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEngagementAsync_WithNonExistentEngagement_ThrowsEngagementNotFoundException()
    {
        // Arrange
        var engagementId = 68L;
        var userId = 69L;
        var request = new UpdateEngagementRequest(null, "Notes");

        _mockEngagementRepo.Setup(x => x.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Engagement?)null);

        // Act
        var act = () => _sut.UpdateEngagementAsync(engagementId, userId, request);

        // Assert
        await act.Should().ThrowAsync<EngagementNotFoundException>();
    }

    [Fact]
    public async Task DeleteEngagementAsync_WithValidPermission_DeletesEngagement()
    {
        // Arrange
        var engagementId = 70L;
        var festivalId = 71L;
        var userId = 72L;

        _mockEngagementRepo.Setup(x => x.GetFestivalIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteEngagementAsync(engagementId, userId);

        // Assert
        _mockEngagementRepo.Verify(x => x.DeleteAsync(engagementId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEngagementAsync_WithNonExistentEngagement_ThrowsEngagementNotFoundException()
    {
        // Arrange
        var engagementId = 73L;
        var userId = 74L;

        _mockEngagementRepo.Setup(x => x.GetFestivalIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        // Act
        var act = () => _sut.DeleteEngagementAsync(engagementId, userId);

        // Assert
        await act.Should().ThrowAsync<EngagementNotFoundException>();
    }

    [Fact]
    public async Task DeleteEngagementAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var engagementId = 75L;
        var festivalId = 76L;
        var userId = 77L;

        _mockEngagementRepo.Setup(x => x.GetFestivalIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalId);
        _mockAuthService.Setup(x => x.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.DeleteEngagementAsync(engagementId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region Helper Methods

    private TimeSlot CreateTestTimeSlot(long? timeSlotId = null, long? stageId = null, long? editionId = null)
    {
        return new TimeSlot
        {
            TimeSlotId = timeSlotId ?? 0L,
            StageId = stageId ?? 0L,
            EditionId = editionId ?? 0L,
            StartTimeUtc = _now.AddHours(2),
            EndTimeUtc = _now.AddHours(3),
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    private Engagement CreateTestEngagement(long? engagementId = null, long? timeSlotId = null, long? artistId = null)
    {
        return new Engagement
        {
            EngagementId = engagementId ?? 0L,
            TimeSlotId = timeSlotId ?? 0L,
            ArtistId = artistId ?? 0L,
            Notes = "Test notes",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = 1L,
            ModifiedAtUtc = _now,
            ModifiedBy = 1L
        };
    }

    #endregion
}

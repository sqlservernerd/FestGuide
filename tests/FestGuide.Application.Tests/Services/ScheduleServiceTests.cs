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
        var editionId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var stage1Id = Guid.NewGuid();
        var stage2Id = Guid.NewGuid();
        var artist1Id = Guid.NewGuid();
        var artist2Id = Guid.NewGuid();
        var timeSlot1Id = Guid.NewGuid();
        var timeSlot2Id = Guid.NewGuid();

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
                EngagementId = Guid.NewGuid(),
                TimeSlotId = timeSlot1Id,
                ArtistId = artist1Id,
                Notes = "Great performance"
            },
            new()
            {
                EngagementId = Guid.NewGuid(),
                TimeSlotId = timeSlot2Id,
                ArtistId = artist2Id,
                Notes = "Acoustic set"
            }
        };

        var stages = new List<Stage>
        {
            new() { StageId = stage1Id, Name = "Main Stage", VenueId = Guid.NewGuid() },
            new() { StageId = stage2Id, Name = "Acoustic Stage", VenueId = Guid.NewGuid() }
        };

        var artists = new List<Artist>
        {
            new() { ArtistId = artist1Id, Name = "Artist 1", FestivalId = Guid.NewGuid() },
            new() { ArtistId = artist2Id, Name = "Artist 2", FestivalId = Guid.NewGuid() }
        };

        _mockEditionRepo.Setup(x => x.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockTimeSlotRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlots);
        _mockEngagementRepo.Setup(x => x.GetByEditionAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagements);
        _mockStageRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stages);
        _mockArtistRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
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
        _mockStageRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockArtistRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleDetailAsync_WithInvalidEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var editionId = Guid.NewGuid();

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
        var editionId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();

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
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();

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
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
        var editionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockEditionRepo.Setup(x => x.GetFestivalIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

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
        var timeSlotId = Guid.NewGuid();
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
        var timeSlotId = Guid.NewGuid();

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
        var stageId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
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
        var stageId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
            .ReturnsAsync(Guid.NewGuid());

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
        var stageId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateTimeSlotRequest(Guid.NewGuid(), _now, _now.AddHours(1));

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
        var stageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateTimeSlotRequest(Guid.NewGuid(), _now, _now.AddHours(1));

        _mockStageRepo.Setup(x => x.GetFestivalIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        var act = () => _sut.CreateTimeSlotAsync(stageId, userId, request);

        // Assert
        await act.Should().ThrowAsync<StageNotFoundException>();
    }

    [Fact]
    public async Task CreateTimeSlotAsync_WithOverlappingTimeSlot_ThrowsValidationException()
    {
        // Arrange
        var stageId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var timeSlotId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var timeSlotId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var timeSlotId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
        var timeSlotId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockTimeSlotRepo.Setup(x => x.GetFestivalIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

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
        var engagementId = Guid.NewGuid();
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
        var engagementId = Guid.NewGuid();

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
        var timeSlotId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
            .ReturnsAsync(Guid.NewGuid());

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
        var timeSlotId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateEngagementRequest(ArtistId: Guid.NewGuid(), Notes: null);

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
        var timeSlotId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var timeSlotId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var engagementId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var newArtistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var engagementId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var engagementId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
        var engagementId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockEngagementRepo.Setup(x => x.GetFestivalIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        var act = () => _sut.DeleteEngagementAsync(engagementId, userId);

        // Assert
        await act.Should().ThrowAsync<EngagementNotFoundException>();
    }

    [Fact]
    public async Task DeleteEngagementAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var engagementId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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

    private TimeSlot CreateTestTimeSlot(Guid? timeSlotId = null, Guid? stageId = null, Guid? editionId = null)
    {
        return new TimeSlot
        {
            TimeSlotId = timeSlotId ?? Guid.NewGuid(),
            StageId = stageId ?? Guid.NewGuid(),
            EditionId = editionId ?? Guid.NewGuid(),
            StartTimeUtc = _now.AddHours(2),
            EndTimeUtc = _now.AddHours(3),
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = Guid.NewGuid()
        };
    }

    private Engagement CreateTestEngagement(Guid? engagementId = null, Guid? timeSlotId = null, Guid? artistId = null)
    {
        return new Engagement
        {
            EngagementId = engagementId ?? Guid.NewGuid(),
            TimeSlotId = timeSlotId ?? Guid.NewGuid(),
            ArtistId = artistId ?? Guid.NewGuid(),
            Notes = "Test notes",
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = Guid.NewGuid()
        };
    }

    #endregion
}

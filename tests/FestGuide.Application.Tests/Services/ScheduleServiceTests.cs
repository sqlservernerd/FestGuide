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
}

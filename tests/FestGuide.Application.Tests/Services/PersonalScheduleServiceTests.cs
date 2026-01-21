using FluentAssertions;
using Moq;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

public class PersonalScheduleServiceTests
{
    private readonly Mock<IPersonalScheduleRepository> _mockScheduleRepo;
    private readonly Mock<IEditionRepository> _mockEditionRepo;
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IEngagementRepository> _mockEngagementRepo;
    private readonly Mock<ITimeSlotRepository> _mockTimeSlotRepo;
    private readonly Mock<IArtistRepository> _mockArtistRepo;
    private readonly Mock<IStageRepository> _mockStageRepo;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<PersonalScheduleService>> _mockLogger;
    private readonly PersonalScheduleService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public PersonalScheduleServiceTests()
    {
        _mockScheduleRepo = new Mock<IPersonalScheduleRepository>();
        _mockEditionRepo = new Mock<IEditionRepository>();
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockEngagementRepo = new Mock<IEngagementRepository>();
        _mockTimeSlotRepo = new Mock<ITimeSlotRepository>();
        _mockArtistRepo = new Mock<IArtistRepository>();
        _mockStageRepo = new Mock<IStageRepository>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<PersonalScheduleService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new PersonalScheduleService(
            _mockScheduleRepo.Object,
            _mockEditionRepo.Object,
            _mockFestivalRepo.Object,
            _mockEngagementRepo.Object,
            _mockTimeSlotRepo.Object,
            _mockArtistRepo.Object,
            _mockStageRepo.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSchedule()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockScheduleRepo.Setup(r => r.GetEntriesAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalScheduleEntry>());

        // Act
        var result = await _sut.GetByIdAsync(scheduleId, userId);

        // Assert
        result.Should().NotBeNull();
        result.PersonalScheduleId.Should().Be(scheduleId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsPersonalScheduleNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);

        // Act
        var act = () => _sut.GetByIdAsync(scheduleId, userId);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, ownerId);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.GetByIdAsync(scheduleId, differentUserId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region GetMySchedulesAsync Tests

    [Fact]
    public async Task GetMySchedulesAsync_WithSchedules_ReturnsSchedules()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var schedules = new List<PersonalSchedule>
        {
            CreateTestSchedule(userId: userId, editionId: editionId),
            CreateTestSchedule(userId: userId, editionId: editionId)
        };

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId, Name = "2026 Edition" };
        var festival = new Festival { FestivalId = festivalId, Name = "Test Festival" };

        _mockScheduleRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);
        _mockScheduleRepo.Setup(r => r.GetEntriesByScheduleIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyList<PersonalScheduleEntry>>());
        _mockEditionRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FestivalEdition> { edition });
        _mockFestivalRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Festival> { festival });

        // Act
        var result = await _sut.GetMySchedulesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMySchedulesAsync_WithNoSchedules_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule>());

        // Act
        var result = await _sut.GetMySchedulesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByEditionAsync Tests

    [Fact]
    public async Task GetByEditionAsync_WithValidEdition_ReturnsSchedules()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var schedules = new List<PersonalSchedule>
        {
            CreateTestSchedule(userId: userId, editionId: editionId),
            CreateTestSchedule(userId: userId, editionId: editionId)
        };

        _mockScheduleRepo.Setup(r => r.GetByUserAndEditionAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);
        _mockScheduleRepo.Setup(r => r.GetEntriesByScheduleIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyList<PersonalScheduleEntry>>());

        // Act
        var result = await _sut.GetByEditionAsync(userId, editionId);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.EditionId == editionId).Should().BeTrue();
    }

    [Fact]
    public async Task GetByEditionAsync_WithNoSchedules_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetByUserAndEditionAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule>());

        // Act
        var result = await _sut.GetByEditionAsync(userId, editionId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetDetailAsync Tests

    [Fact]
    public async Task GetDetailAsync_WithValidSchedule_ReturnsDetailWithEntries()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId, editionId);
        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId, Name = "2026 Edition" };
        var festival = new Festival { FestivalId = festivalId, Name = "Test Festival" };

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockScheduleRepo.Setup(r => r.GetEntriesAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalScheduleEntry>());
        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);
        _mockEngagementRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Engagement>());

        // Act
        var result = await _sut.GetDetailAsync(scheduleId, userId);

        // Assert
        result.Should().NotBeNull();
        result.PersonalScheduleId.Should().Be(scheduleId);
        result.EditionName.Should().Be("2026 Edition");
        result.FestivalName.Should().Be("Test Festival");
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailAsync_WithNonExistentSchedule_ThrowsPersonalScheduleNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);

        // Act
        var act = () => _sut.GetDetailAsync(scheduleId, userId);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleNotFoundException>();
    }

    [Fact]
    public async Task GetDetailAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, ownerId);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.GetDetailAsync(scheduleId, differentUserId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesSchedule()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var request = new CreatePersonalScheduleRequest(EditionId: editionId, Name: "My Festival Schedule");

        _mockEditionRepo.Setup(r => r.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(r => r.GetByUserAndEditionAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule>());
        _mockScheduleRepo.Setup(r => r.CreateAsync(It.IsAny<PersonalSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.EditionId.Should().Be(editionId);
        result.IsDefault.Should().BeTrue(); // First schedule is default

        _mockScheduleRepo.Verify(r => r.CreateAsync(
            It.Is<PersonalSchedule>(s => s.Name == request.Name && s.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithExistingSchedules_SetsIsDefaultFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var request = new CreatePersonalScheduleRequest(EditionId: editionId, Name: "Second Schedule");

        var existingSchedule = CreateTestSchedule(userId: userId, editionId: editionId);

        _mockEditionRepo.Setup(r => r.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(r => r.GetByUserAndEditionAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule> { existingSchedule });
        _mockScheduleRepo.Setup(r => r.CreateAsync(It.IsAny<PersonalSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentEdition_ThrowsEditionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var request = new CreatePersonalScheduleRequest(EditionId: editionId, Name: null);

        _mockEditionRepo.Setup(r => r.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<EditionNotFoundException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesSchedule()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId);
        var request = new UpdatePersonalScheduleRequest(Name: "Updated Name", IsDefault: null);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockScheduleRepo.Setup(r => r.GetEntriesAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalScheduleEntry>());

        // Act
        var result = await _sut.UpdateAsync(scheduleId, userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockScheduleRepo.Verify(r => r.UpdateAsync(It.IsAny<PersonalSchedule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SetAsDefault_ClearsOtherDefaults()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var otherScheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();

        var schedule = CreateTestSchedule(scheduleId, userId, editionId);
        schedule.IsDefault = false;

        var otherSchedule = CreateTestSchedule(otherScheduleId, userId, editionId);
        otherSchedule.IsDefault = true;

        var request = new UpdatePersonalScheduleRequest(Name: null, IsDefault: true);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockScheduleRepo.Setup(r => r.GetByUserAndEditionAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule> { schedule, otherSchedule });
        _mockScheduleRepo.Setup(r => r.GetEntriesAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalScheduleEntry>());

        // Act
        var result = await _sut.UpdateAsync(scheduleId, userId, request);

        // Assert
        result.Should().NotBeNull();
        // Verify other schedule was updated to not be default
        _mockScheduleRepo.Verify(r => r.UpdateAsync(
            It.Is<PersonalSchedule>(s => s.PersonalScheduleId == otherScheduleId && !s.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentSchedule_ThrowsPersonalScheduleNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdatePersonalScheduleRequest(Name: "Updated", IsDefault: null);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);

        // Act
        var act = () => _sut.UpdateAsync(scheduleId, userId, request);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, ownerId);
        var request = new UpdatePersonalScheduleRequest(Name: "Updated", IsDefault: null);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.UpdateAsync(scheduleId, differentUserId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPermission_DeletesSchedule()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        await _sut.DeleteAsync(scheduleId, userId);

        // Assert
        _mockScheduleRepo.Verify(r => r.DeleteAsync(scheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentSchedule_ThrowsPersonalScheduleNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);

        // Act
        var act = () => _sut.DeleteAsync(scheduleId, userId);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, ownerId);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.DeleteAsync(scheduleId, differentUserId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region GetOrCreateDefaultAsync Tests

    [Fact]
    public async Task GetOrCreateDefaultAsync_WithExistingDefault_ReturnsExisting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var existingSchedule = CreateTestSchedule(userId: userId, editionId: editionId);

        _mockScheduleRepo.Setup(r => r.GetDefaultAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSchedule);
        _mockScheduleRepo.Setup(r => r.GetEntriesAsync(existingSchedule.PersonalScheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalScheduleEntry>());

        // Act
        var result = await _sut.GetOrCreateDefaultAsync(userId, editionId);

        // Assert
        result.Should().NotBeNull();
        result.PersonalScheduleId.Should().Be(existingSchedule.PersonalScheduleId);
        _mockScheduleRepo.Verify(r => r.CreateAsync(It.IsAny<PersonalSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateDefaultAsync_WithNoExisting_CreatesNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetDefaultAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);
        _mockEditionRepo.Setup(r => r.ExistsAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockScheduleRepo.Setup(r => r.GetByUserAndEditionAsync(userId, editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule>());
        _mockScheduleRepo.Setup(r => r.CreateAsync(It.IsAny<PersonalSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.GetOrCreateDefaultAsync(userId, editionId);

        // Assert
        result.Should().NotBeNull();
        _mockScheduleRepo.Verify(r => r.CreateAsync(It.IsAny<PersonalSchedule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddEntryAsync Tests

    [Fact]
    public async Task AddEntryAsync_WithValidRequest_AddsEntry()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var engagementId = Guid.NewGuid();
        var timeSlotId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var schedule = CreateTestSchedule(scheduleId, userId);
        var engagement = new Engagement { EngagementId = engagementId, TimeSlotId = timeSlotId, ArtistId = artistId };
        var timeSlot = new TimeSlot { TimeSlotId = timeSlotId, StageId = stageId, StartTimeUtc = _now, EndTimeUtc = _now.AddHours(1) };
        var artist = new Artist { ArtistId = artistId, Name = "Test Artist" };
        var stage = new Stage { StageId = stageId, Name = "Main Stage" };
        var request = new AddScheduleEntryRequest(EngagementId: engagementId, Notes: "Can't miss this!", NotificationsEnabled: true);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockEngagementRepo.Setup(r => r.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement);
        _mockScheduleRepo.Setup(r => r.HasEngagementAsync(scheduleId, engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockScheduleRepo.Setup(r => r.AddEntryAsync(It.IsAny<PersonalScheduleEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockTimeSlotRepo.Setup(r => r.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlot);
        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockStageRepo.Setup(r => r.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);

        // Act
        var result = await _sut.AddEntryAsync(scheduleId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.EngagementId.Should().Be(engagementId);
        result.ArtistName.Should().Be("Test Artist");
        result.StageName.Should().Be("Main Stage");

        _mockScheduleRepo.Verify(r => r.AddEntryAsync(
            It.Is<PersonalScheduleEntry>(e => e.EngagementId == engagementId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEntryAsync_WithNonExistentSchedule_ThrowsPersonalScheduleNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new AddScheduleEntryRequest(Guid.NewGuid(), null, true);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);

        // Act
        var act = () => _sut.AddEntryAsync(scheduleId, userId, request);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleNotFoundException>();
    }

    [Fact]
    public async Task AddEntryAsync_WithNonExistentEngagement_ThrowsEngagementNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var engagementId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId);
        var request = new AddScheduleEntryRequest(engagementId, null, true);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockEngagementRepo.Setup(r => r.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Engagement?)null);

        // Act
        var act = () => _sut.AddEntryAsync(scheduleId, userId, request);

        // Assert
        await act.Should().ThrowAsync<EngagementNotFoundException>();
    }

    [Fact]
    public async Task AddEntryAsync_WithDuplicateEngagement_ThrowsConflictException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var engagementId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId);
        var engagement = new Engagement { EngagementId = engagementId };
        var request = new AddScheduleEntryRequest(engagementId, null, true);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockEngagementRepo.Setup(r => r.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement);
        _mockScheduleRepo.Setup(r => r.HasEngagementAsync(scheduleId, engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.AddEntryAsync(scheduleId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already*");
    }

    [Fact]
    public async Task AddEntryAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, ownerId);
        var request = new AddScheduleEntryRequest(Guid.NewGuid(), null, true);

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.AddEntryAsync(scheduleId, differentUserId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region UpdateEntryAsync Tests

    [Fact]
    public async Task UpdateEntryAsync_WithValidRequest_UpdatesEntry()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var engagementId = Guid.NewGuid();
        var timeSlotId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var entry = new PersonalScheduleEntry
        {
            PersonalScheduleEntryId = entryId,
            PersonalScheduleId = scheduleId,
            EngagementId = engagementId,
            Notes = "Old notes",
            NotificationsEnabled = false
        };

        var schedule = CreateTestSchedule(scheduleId, userId);
        var engagement = new Engagement { EngagementId = engagementId, TimeSlotId = timeSlotId, ArtistId = artistId };
        var timeSlot = new TimeSlot { TimeSlotId = timeSlotId, StageId = stageId, StartTimeUtc = _now, EndTimeUtc = _now.AddHours(1) };
        var artist = new Artist { ArtistId = artistId, Name = "Test Artist" };
        var stage = new Stage { StageId = stageId, Name = "Main Stage" };

        var request = new UpdateScheduleEntryRequest(Notes: "Updated notes", NotificationsEnabled: true);

        _mockScheduleRepo.Setup(r => r.GetEntryByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockEngagementRepo.Setup(r => r.GetByIdAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement);
        _mockTimeSlotRepo.Setup(r => r.GetByIdAsync(timeSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeSlot);
        _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        _mockStageRepo.Setup(r => r.GetByIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);

        // Act
        var result = await _sut.UpdateEntryAsync(entryId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.PersonalScheduleEntryId.Should().Be(entryId);
        result.ArtistName.Should().Be("Test Artist");
        result.StageName.Should().Be("Main Stage");

        _mockScheduleRepo.Verify(r => r.UpdateEntryAsync(
            It.Is<PersonalScheduleEntry>(e => e.PersonalScheduleEntryId == entryId && e.Notes == "Updated notes" && e.NotificationsEnabled),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEntryAsync_WithNonExistentEntry_ThrowsPersonalScheduleEntryNotFoundException()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateScheduleEntryRequest(Notes: "Updated", NotificationsEnabled: true);

        _mockScheduleRepo.Setup(r => r.GetEntryByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalScheduleEntry?)null);

        // Act
        var act = () => _sut.UpdateEntryAsync(entryId, userId, request);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleEntryNotFoundException>();
    }

    [Fact]
    public async Task UpdateEntryAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var entry = new PersonalScheduleEntry
        {
            PersonalScheduleEntryId = entryId,
            PersonalScheduleId = scheduleId,
            EngagementId = Guid.NewGuid()
        };

        var schedule = CreateTestSchedule(scheduleId, ownerId);
        var request = new UpdateScheduleEntryRequest(Notes: "Updated", NotificationsEnabled: null);

        _mockScheduleRepo.Setup(r => r.GetEntryByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.UpdateEntryAsync(entryId, differentUserId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region RemoveEntryAsync Tests

    [Fact]
    public async Task RemoveEntryAsync_WithValidPermission_RemovesEntry()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId);

        _mockScheduleRepo.Setup(r => r.GetScheduleIdForEntryAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleId);
        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        await _sut.RemoveEntryAsync(entryId, userId);

        // Assert
        _mockScheduleRepo.Verify(r => r.RemoveEntryAsync(entryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEntryAsync_WithNonExistentEntry_ThrowsPersonalScheduleEntryNotFoundException()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetScheduleIdForEntryAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        var act = () => _sut.RemoveEntryAsync(entryId, userId);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleEntryNotFoundException>();
    }

    [Fact]
    public async Task RemoveEntryAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, ownerId);

        _mockScheduleRepo.Setup(r => r.GetScheduleIdForEntryAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleId);
        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var act = () => _sut.RemoveEntryAsync(entryId, differentUserId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    #endregion

    #region SyncAsync Tests

    [Fact]
    public async Task SyncAsync_WithValidSchedule_UpdatesLastSyncedAndReturnsDetail()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var editionId = Guid.NewGuid();
        var festivalId = Guid.NewGuid();
        var schedule = CreateTestSchedule(scheduleId, userId, editionId);

        var edition = new FestivalEdition { EditionId = editionId, FestivalId = festivalId, Name = "2026 Edition" };
        var festival = new Festival { FestivalId = festivalId, Name = "Test Festival" };

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        _mockScheduleRepo.Setup(r => r.GetEntriesAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalScheduleEntry>());
        _mockEditionRepo.Setup(r => r.GetByIdAsync(editionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);
        _mockFestivalRepo.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);

        // Act
        var result = await _sut.SyncAsync(scheduleId, userId);

        // Assert
        result.Should().NotBeNull();
        result.PersonalScheduleId.Should().Be(scheduleId);
        result.FestivalName.Should().Be("Test Festival");
        result.EditionName.Should().Be("2026 Edition");

        _mockScheduleRepo.Verify(r => r.UpdateLastSyncedAsync(scheduleId, _now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_WithNonExistentSchedule_ThrowsPersonalScheduleNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockScheduleRepo.Setup(r => r.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalSchedule?)null);

        // Act
        var act = () => _sut.SyncAsync(scheduleId, userId);

        // Assert
        await act.Should().ThrowAsync<PersonalScheduleNotFoundException>();
    }

    #endregion

    #region Helper Methods

    private PersonalSchedule CreateTestSchedule(
        Guid? scheduleId = null,
        Guid? userId = null,
        Guid? editionId = null)
    {
        return new PersonalSchedule
        {
            PersonalScheduleId = scheduleId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            EditionId = editionId ?? Guid.NewGuid(),
            Name = "My Schedule",
            IsDefault = true,
            IsDeleted = false,
            CreatedAtUtc = _now,
            CreatedBy = userId ?? Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = userId ?? Guid.NewGuid()
        };
    }

    #endregion
}

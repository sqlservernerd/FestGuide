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
using FestGuide.Infrastructure.Email;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

public class PermissionServiceTests
{
    private readonly Mock<IFestivalPermissionRepository> _mockPermissionRepo;
    private readonly Mock<IFestivalRepository> _mockFestivalRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IFestivalAuthorizationService> _mockAuthService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<IDbTransactionProvider> _mockTransactionProvider;
    private readonly Mock<ITransactionScope> _mockTransactionScope;
    private readonly Mock<ILogger<PermissionService>> _mockLogger;
    private readonly PermissionService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public PermissionServiceTests()
    {
        _mockPermissionRepo = new Mock<IFestivalPermissionRepository>();
        _mockFestivalRepo = new Mock<IFestivalRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockAuthService = new Mock<IFestivalAuthorizationService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockTransactionProvider = new Mock<IDbTransactionProvider>();
        _mockTransactionScope = new Mock<ITransactionScope>();
        _mockLogger = new Mock<ILogger<PermissionService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);
        _mockTransactionProvider.Setup(x => x.BeginTransaction()).Returns(_mockTransactionScope.Object);

        _sut = new PermissionService(
            _mockPermissionRepo.Object,
            _mockFestivalRepo.Object,
            _mockUserRepo.Object,
            _mockAuthService.Object,
            _mockEmailService.Object,
            _mockDateTimeProvider.Object,
            _mockTransactionProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetByFestivalAsync_WithValidFestivalId_ReturnsPermissions()
    {
        // Arrange
        var festivalId = 1L;
        var userId = 2L;
        var user1Id = 3L;
        var user2Id = 4L;

        var permissions = new List<FestivalPermission>
        {
            new()
            {
                FestivalPermissionId = 1L,
                FestivalId = festivalId,
                UserId = user1Id,
                Role = FestivalRole.Manager,
                Scope = PermissionScope.Artists,
                IsPending = false,
                IsRevoked = false
            },
            new()
            {
                FestivalPermissionId = 1L,
                FestivalId = festivalId,
                UserId = user2Id,
                Role = FestivalRole.Viewer,
                Scope = PermissionScope.All,
                IsPending = true,
                IsRevoked = false
            }
        };

        var users = new List<User>
        {
            new() { UserId = user1Id, Email = "user1@test.com", DisplayName = "User 1" },
            new() { UserId = user2Id, Email = "user2@test.com", DisplayName = "User 2" }
        };

        _mockAuthService.Setup(x => x.CanViewFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockPermissionRepo.Setup(x => x.GetActiveByFestivalAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _sut.GetByFestivalAsync(festivalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].UserEmail.Should().Be("user1@test.com");
        result[0].UserDisplayName.Should().Be("User 1");
        result[1].UserEmail.Should().Be("user2@test.com");
        result[1].UserDisplayName.Should().Be("User 2");
        
        // Verify batch fetch was used (called once, not per permission)
        _mockUserRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByFestivalAsync_WithoutPermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = 5L;
        var userId = 6L;

        _mockAuthService.Setup(x => x.CanViewFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.GetByFestivalAsync(festivalId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to view this festival's permissions.");
    }

    [Fact]
    public async Task GetPendingInvitationsAsync_WithPendingInvitations_ReturnsInvitations()
    {
        // Arrange
        var userId = 7L;
        var festivalId1 = 100L;
        var festivalId2 = 101L;
        var inviterId = 8L;

        var permissions = new List<FestivalPermission>
        {
            new()
            {
                FestivalPermissionId = 1L,
                FestivalId = festivalId1,
                UserId = userId,
                Role = FestivalRole.Manager,
                Scope = PermissionScope.Artists,
                IsPending = true,
                IsRevoked = false,
                InvitedByUserId = inviterId,
                CreatedAtUtc = _now.AddDays(-1)
            },
            new()
            {
                FestivalPermissionId = 1L,
                FestivalId = festivalId2,
                UserId = userId,
                Role = FestivalRole.Viewer,
                Scope = PermissionScope.All,
                IsPending = true,
                IsRevoked = false,
                InvitedByUserId = inviterId,
                CreatedAtUtc = _now.AddDays(-2)
            },
            new()
            {
                FestivalPermissionId = 1L,
                FestivalId = festivalId1,
                UserId = userId,
                Role = FestivalRole.Viewer,
                Scope = PermissionScope.All,
                IsPending = false, // Not pending
                IsRevoked = false,
                CreatedAtUtc = _now.AddDays(-3)
            }
        };

        var festivals = new List<Festival>
        {
            new() { FestivalId = festivalId1, Name = "Festival 1", OwnerUserId = 1L },
            new() { FestivalId = festivalId2, Name = "Festival 2", OwnerUserId = 1L }
        };

        var inviter = new User { UserId = inviterId, Email = "inviter@test.com", DisplayName = "Inviter" };

        _mockPermissionRepo.Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockFestivalRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivals);
        _mockUserRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { inviter });

        // Act
        var result = await _sut.GetPendingInvitationsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only pending invitations
        result[0].FestivalName.Should().Be("Festival 1");
        result[0].InvitedByUserName.Should().Be("Inviter");
        result[1].FestivalName.Should().Be("Festival 2");
        
        // Verify batch fetches were used
        _mockFestivalRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepo.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidPermission_ReturnsPermission()
    {
        // Arrange
        var permissionId = 9L;
        var userId = 10L;
        var targetUserId = 11L;
        var festivalId = 12L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = targetUserId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = false,
            IsRevoked = false
        };

        var user = new User { UserId = targetUserId, Email = "user@test.com", DisplayName = "Test User" };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanViewFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetByIdAsync(permissionId, userId);

        // Assert
        result.Should().NotBeNull();
        result.PermissionId.Should().Be(permissionId);
        result.UserEmail.Should().Be("user@test.com");
        result.UserDisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentPermission_ThrowsPermissionNotFoundException()
    {
        // Arrange
        var permissionId = 13L;
        var userId = 14L;

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalPermission?)null);

        // Act
        var act = async () => await _sut.GetByIdAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<PermissionNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithoutViewPermission_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 15L;
        var userId = 16L;
        var festivalId = 17L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = 1L,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanViewFestivalAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.GetByIdAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to view this permission.");
    }

    [Fact]
    public async Task InviteUserAsync_WithExistingUser_CreatesInvitation()
    {
        // Arrange
        var festivalId = 18L;
        var invitingUserId = 19L;
        var invitedUserId = 20L;
        var email = "invited@test.com";

        var request = new InviteUserRequest(
            Email: email,
            Role: FestivalRole.Manager,
            Scope: PermissionScope.Artists);

        var invitedUser = new User { UserId = invitedUserId, Email = email, DisplayName = "Invited User" };

        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(invitingUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(x => x.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitedUser);
        _mockPermissionRepo.Setup(x => x.GetByUserAndFestivalAsync(invitedUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalPermission?)null);

        // Act
        var result = await _sut.InviteUserAsync(festivalId, invitingUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.InvitedEmail.Should().Be(email);
        result.Role.Should().Be(FestivalRole.Manager);
        result.Scope.Should().Be(PermissionScope.Artists);
        result.IsNewUser.Should().BeFalse();
        _mockPermissionRepo.Verify(x => x.CreateAsync(It.IsAny<FestivalPermission>(), It.IsAny<ITransactionScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionScope.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_WithNewUser_CreatesInvitationWithPlaceholderId()
    {
        // Arrange
        var festivalId = 21L;
        var invitingUserId = 22L;
        var email = "newuser@test.com";

        var request = new InviteUserRequest(
            Email: email,
            Role: FestivalRole.Viewer,
            Scope: PermissionScope.All);

        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(invitingUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(x => x.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.InviteUserAsync(festivalId, invitingUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.InvitedEmail.Should().Be(email);
        result.IsNewUser.Should().BeTrue();
        result.Message.Should().Contain("need to create an account");
        _mockPermissionRepo.Verify(x => x.CreateAsync(It.IsAny<FestivalPermission>(), It.IsAny<ITransactionScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionScope.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_WithExistingPermission_ThrowsConflictException()
    {
        // Arrange
        var festivalId = 23L;
        var invitingUserId = 24L;
        var invitedUserId = 25L;
        var email = "invited@test.com";

        var request = new InviteUserRequest(
            Email: email,
            Role: FestivalRole.Manager,
            Scope: PermissionScope.Artists);

        var invitedUser = new User { UserId = invitedUserId, Email = email, DisplayName = "Invited User" };
        var existingPermission = new FestivalPermission
        {
            FestivalPermissionId = 1L,
            FestivalId = festivalId,
            UserId = invitedUserId,
            IsRevoked = false
        };

        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(invitingUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(x => x.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitedUser);
        _mockPermissionRepo.Setup(x => x.GetByUserAndFestivalAsync(invitedUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPermission);

        // Act
        var act = async () => await _sut.InviteUserAsync(festivalId, invitingUserId, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("User already has permission for this festival.");
    }

    [Fact]
    public async Task InviteUserAsync_WithoutManagePermission_ThrowsForbiddenException()
    {
        // Arrange
        var festivalId = 26L;
        var invitingUserId = 27L;
        var request = new InviteUserRequest(
            Email: "test@test.com",
            Role: FestivalRole.Manager,
            Scope: PermissionScope.Artists);

        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(invitingUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.InviteUserAsync(festivalId, invitingUserId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to invite users to this festival.");
    }

    /// <summary>
    /// Verifies that when email sending fails, the transaction is rolled back and the permission is not persisted.
    /// This unit test verifies the transaction behavior with mocks.
    /// Note: Consider adding an integration test that verifies the actual rollback behavior against a real database
    /// to ensure the transaction pattern works correctly end-to-end.
    /// </summary>
    [Fact]
    public async Task InviteUserAsync_WhenEmailSendingFails_RollsBackTransaction()
    {
        // Arrange
        var festivalId = 28L;
        var invitingUserId = 29L;
        var invitedUserId = 30L;
        var email = "invited@test.com";

        var request = new InviteUserRequest(
            Email: email,
            Role: FestivalRole.Manager,
            Scope: PermissionScope.Artists);

        var invitedUser = new User { UserId = invitedUserId, Email = email, DisplayName = "Invited User" };
        var festival = new Festival { FestivalId = festivalId, Name = "Test Festival" };
        var inviter = new User { UserId = invitingUserId, Email = "inviter@test.com", DisplayName = "Inviter" };

        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(invitingUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFestivalRepo.Setup(x => x.ExistsAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitedUser);
        _mockPermissionRepo.Setup(x => x.GetByUserAndFestivalAsync(invitedUserId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FestivalPermission?)null);
        _mockFestivalRepo.Setup(x => x.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(festival);
        _mockUserRepo.Setup(x => x.GetByIdAsync(invitingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);
        _mockEmailService.Setup(x => x.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email service unavailable"));

        // Act
        var act = async () => await _sut.InviteUserAsync(festivalId, invitingUserId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email service unavailable");
        _mockPermissionRepo.Verify(x => x.CreateAsync(It.IsAny<FestivalPermission>(), It.IsAny<ITransactionScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionScope.Verify(x => x.Commit(), Times.Never);
        _mockTransactionScope.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesPermission()
    {
        // Arrange
        var permissionId = 31L;
        var userId = 32L;
        var targetUserId = 33L;
        var festivalId = 34L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = targetUserId,
            Role = FestivalRole.Viewer,
            Scope = PermissionScope.All,
            IsPending = false,
            IsRevoked = false
        };

        var request = new UpdatePermissionRequest(
            Role: FestivalRole.Manager,
            Scope: PermissionScope.Artists);

        var user = new User { UserId = targetUserId, Email = "user@test.com", DisplayName = "Test User" };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.UpdateAsync(permissionId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be(FestivalRole.Manager);
        result.Scope.Should().Be(PermissionScope.Artists);
        _mockPermissionRepo.Verify(x => x.UpdateAsync(It.IsAny<FestivalPermission>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithOwnerRole_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 35L;
        var userId = 36L;
        var festivalId = 37L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = 1L,
            Role = FestivalRole.Owner,
            Scope = PermissionScope.All
        };

        var request = new UpdatePermissionRequest(Role: FestivalRole.Manager, Scope: null);

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.UpdateAsync(permissionId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Cannot modify the owner's permission. Use ownership transfer instead.");
    }

    [Fact]
    public async Task UpdateAsync_PromotingToOwner_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 38L;
        var userId = 39L;
        var festivalId = 40L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = 1L,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.All
        };

        var request = new UpdatePermissionRequest(Role: FestivalRole.Owner, Scope: null);

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.UpdateAsync(permissionId, userId, request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Cannot change role to Owner. Use ownership transfer instead.");
    }

    [Fact]
    public async Task RevokeAsync_WithValidPermission_RevokesPermission()
    {
        // Arrange
        var permissionId = 41L;
        var userId = 42L;
        var targetUserId = 43L;
        var festivalId = 44L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = targetUserId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.RevokeAsync(permissionId, userId);

        // Assert
        _mockPermissionRepo.Verify(x => x.RevokeAsync(permissionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_WithOwnerPermission_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 45L;
        var userId = 46L;
        var festivalId = 47L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = 1L,
            Role = FestivalRole.Owner,
            Scope = PermissionScope.All
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.RevokeAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Cannot revoke the owner's permission. Transfer ownership first.");
    }

    [Fact]
    public async Task RevokeAsync_RevokeOwnPermission_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 48L;
        var userId = 49L;
        var festivalId = 50L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = userId, // Same as the revoking user
            Role = FestivalRole.Manager,
            Scope = PermissionScope.All
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mockAuthService.Setup(x => x.CanManagePermissionsAsync(userId, festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.RevokeAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Cannot revoke your own permission.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithValidInvitation_AcceptsInvitation()
    {
        // Arrange
        var permissionId = 51L;
        var userId = 52L;
        var festivalId = 53L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = true,
            IsRevoked = false
        };

        var acceptedPermission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = false,
            IsRevoked = false
        };

        var user = new User { UserId = userId, Email = "user@test.com", DisplayName = "Test User" };

        _mockPermissionRepo.SetupSequence(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission)
            .ReturnsAsync(acceptedPermission);
        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AcceptInvitationAsync(permissionId, userId);

        // Assert
        result.Should().NotBeNull();
        result.PermissionId.Should().Be(permissionId);
        _mockPermissionRepo.Verify(x => x.AcceptInvitationAsync(permissionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 54L;
        var userId = 55L;
        var differentUserId = 56L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = 1L,
            UserId = differentUserId, // Different user
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = true,
            IsRevoked = false
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var act = async () => await _sut.AcceptInvitationAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("This invitation is not for you.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithAlreadyProcessedInvitation_ThrowsConflictException()
    {
        // Arrange
        var permissionId = 57L;
        var userId = 58L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = 1L,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = false, // Already processed
            IsRevoked = false
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var act = async () => await _sut.AcceptInvitationAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("This invitation has already been processed.");
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithRevokedInvitation_ThrowsConflictException()
    {
        // Arrange
        var permissionId = 59L;
        var userId = 60L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = 1L,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = true,
            IsRevoked = true
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var act = async () => await _sut.AcceptInvitationAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("This invitation has been revoked.");
    }

    [Fact]
    public async Task DeclineInvitationAsync_WithValidInvitation_DeclinesInvitation()
    {
        // Arrange
        var permissionId = 61L;
        var userId = 62L;
        var festivalId = 63L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = festivalId,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = true,
            IsRevoked = false
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        await _sut.DeclineInvitationAsync(permissionId, userId);

        // Assert
        _mockPermissionRepo.Verify(x => x.RevokeAsync(permissionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeclineInvitationAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var permissionId = 64L;
        var userId = 65L;
        var differentUserId = 66L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = 1L,
            UserId = differentUserId, // Different user
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = true,
            IsRevoked = false
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var act = async () => await _sut.DeclineInvitationAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("This invitation is not for you.");
    }

    [Fact]
    public async Task DeclineInvitationAsync_WithAlreadyProcessedInvitation_ThrowsConflictException()
    {
        // Arrange
        var permissionId = 67L;
        var userId = 68L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = 1L,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = false, // Already processed
            IsRevoked = false
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var act = async () => await _sut.DeclineInvitationAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("This invitation has already been processed.");
    }

    [Fact]
    public async Task DeclineInvitationAsync_WithRevokedInvitation_ThrowsConflictException()
    {
        // Arrange
        var permissionId = 69L;
        var userId = 70L;

        var permission = new FestivalPermission
        {
            FestivalPermissionId = permissionId,
            FestivalId = 1L,
            UserId = userId,
            Role = FestivalRole.Manager,
            Scope = PermissionScope.Artists,
            IsPending = true,
            IsRevoked = true
        };

        _mockPermissionRepo.Setup(x => x.GetByIdAsync(permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var act = async () => await _sut.DeclineInvitationAsync(permissionId, userId);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("This invitation has been revoked.");
    }
}

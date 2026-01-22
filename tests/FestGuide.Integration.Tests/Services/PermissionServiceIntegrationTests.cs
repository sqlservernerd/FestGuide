using FluentAssertions;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace FestGuide.Integration.Tests.Services;

/// <summary>
/// Integration tests for PermissionService that verify transaction behavior against a real database.
/// Tests are marked with Skip until test database infrastructure is configured.
/// </summary>
public class PermissionServiceIntegrationTests : IClassFixture<FestGuideWebApplicationFactory>
{
    private readonly FestGuideWebApplicationFactory _factory;

    public PermissionServiceIntegrationTests(FestGuideWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that when email sending fails during user invitation, the transaction is properly rolled back
    /// and the permission is NOT persisted to the database.
    /// This integration test validates the end-to-end transaction behavior against a real database.
    /// </summary>
    [Fact(Skip = "Requires database - enable after test database infrastructure is configured")]
    public async Task InviteUserAsync_WhenEmailSendingFails_RollsBackPermissionInDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        var festivalRepository = scope.ServiceProvider.GetRequiredService<IFestivalRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IFestivalPermissionRepository>();

        // Create test festival and users in the database
        var organizerId = Guid.NewGuid();
        var organizer = new Domain.Entities.User
        {
            UserId = organizerId,
            Email = $"organizer-{Guid.NewGuid()}@test.com",
            DisplayName = "Test Organizer",
            UserType = UserType.Organizer,
            PasswordHash = "test-hash",
            CreatedAtUtc = DateTime.UtcNow
        };
        await userRepository.CreateAsync(organizer, CancellationToken.None);

        var festivalId = Guid.NewGuid();
        var festival = new Domain.Entities.Festival
        {
            FestivalId = festivalId,
            Name = $"Test Festival {Guid.NewGuid()}",
            OwnerUserId = organizerId,
            CreatedAtUtc = DateTime.UtcNow
        };
        await festivalRepository.CreateAsync(festival, CancellationToken.None);

        var invitedUserId = Guid.NewGuid();
        var invitedUser = new Domain.Entities.User
        {
            UserId = invitedUserId,
            Email = $"invited-{Guid.NewGuid()}@test.com",
            DisplayName = "Invited User",
            UserType = UserType.Organizer,
            PasswordHash = "test-hash",
            CreatedAtUtc = DateTime.UtcNow
        };
        await userRepository.CreateAsync(invitedUser, CancellationToken.None);

        var request = new InviteUserRequest(
            Email: invitedUser.Email,
            Role: FestivalRole.Manager,
            Scope: PermissionScope.Artists);

        // Note: The email service should be configured to fail for this test
        // This could be done by configuring a mock email service in the test factory
        // that throws an exception for this specific scenario

        // Act - Attempt to invite user, which should fail when sending email
        var act = async () => await permissionService.InviteUserAsync(festivalId, organizerId, request);

        // Assert - The invitation should fail
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email*");

        // Verify that the permission was NOT persisted in the database
        var permission = await permissionRepository.GetByUserAndFestivalAsync(
            invitedUserId,
            festivalId,
            CancellationToken.None);

        permission.Should().BeNull("because the transaction should have been rolled back when email sending failed");

        // Clean up - Delete test data
        try
        {
            await festivalRepository.DeleteAsync(festivalId, organizerId, CancellationToken.None);
            await userRepository.DeleteAsync(organizerId, CancellationToken.None);
            await userRepository.DeleteAsync(invitedUserId, CancellationToken.None);
        }
        catch
        {
            // Ignore cleanup errors in test
        }
    }
}

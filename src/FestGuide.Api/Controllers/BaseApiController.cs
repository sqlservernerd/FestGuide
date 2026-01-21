using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FestGuide.Api.Controllers;

/// <summary>
/// Base controller providing common functionality for API controllers.
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the current user's ID from the authentication claims.
    /// </summary>
    /// <returns>The current user's ID as a Guid.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the user is not authenticated or the NameIdentifier claim is not a valid GUID.
    /// </exception>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null)
        {
            throw new InvalidOperationException("Authenticated user does not contain a NameIdentifier claim.");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User NameIdentifier claim is not a valid GUID.");
        }

        return userId;
    }
}

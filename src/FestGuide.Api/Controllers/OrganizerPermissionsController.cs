using System.Security.Claims;
using FluentValidation;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestGuide.Api.Controllers;

/// <summary>
/// Organizer endpoints for permission management.
/// </summary>
[ApiController]
[Route("api/v1/organizer")]
[Produces("application/json")]
[Authorize]
public class OrganizerPermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IValidator<InviteUserRequest> _inviteValidator;
    private readonly IValidator<UpdatePermissionRequest> _updateValidator;
    private readonly ILogger<OrganizerPermissionsController> _logger;

    public OrganizerPermissionsController(
        IPermissionService permissionService,
        IValidator<InviteUserRequest> inviteValidator,
        IValidator<UpdatePermissionRequest> updateValidator,
        ILogger<OrganizerPermissionsController> logger)
    {
        _permissionService = permissionService;
        _inviteValidator = inviteValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all permissions for a festival.
    /// </summary>
    [HttpGet("festivals/{festivalId:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissions(Guid festivalId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var permissions = await _permissionService.GetByFestivalAsync(festivalId, userId.Value, ct);
            return Ok(ApiResponse<IReadOnlyList<PermissionSummaryDto>>.Success(permissions));
        }
        catch (FestivalNotFoundException)
        {
            return NotFound(CreateError("FESTIVAL_NOT_FOUND", "Festival not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets a specific permission by ID.
    /// </summary>
    [HttpGet("permissions/{permissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermission(Guid permissionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var permission = await _permissionService.GetByIdAsync(permissionId, userId.Value, ct);
            return Ok(ApiResponse<PermissionDto>.Success(permission));
        }
        catch (PermissionNotFoundException)
        {
            return NotFound(CreateError("PERMISSION_NOT_FOUND", "Permission not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Invites a user to a festival.
    /// </summary>
    [HttpPost("festivals/{festivalId:guid}/permissions/invite")]
    [ProducesResponseType(typeof(ApiResponse<InvitationResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteUser(Guid festivalId, [FromBody] InviteUserRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _inviteValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var result = await _permissionService.InviteUserAsync(festivalId, userId.Value, request, ct);
            return CreatedAtAction(
                nameof(GetPermission),
                new { permissionId = result.PermissionId },
                ApiResponse<InvitationResultDto>.Success(result));
        }
        catch (FestivalNotFoundException)
        {
            return NotFound(CreateError("FESTIVAL_NOT_FOUND", "Festival not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
        catch (ConflictException ex)
        {
            return Conflict(CreateError("CONFLICT", ex.Message));
        }
    }

    /// <summary>
    /// Updates a permission.
    /// </summary>
    [HttpPut("permissions/{permissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermission(Guid permissionId, [FromBody] UpdatePermissionRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var permission = await _permissionService.UpdateAsync(permissionId, userId.Value, request, ct);
            return Ok(ApiResponse<PermissionDto>.Success(permission));
        }
        catch (PermissionNotFoundException)
        {
            return NotFound(CreateError("PERMISSION_NOT_FOUND", "Permission not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Revokes a permission.
    /// </summary>
    [HttpDelete("permissions/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePermission(Guid permissionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _permissionService.RevokeAsync(permissionId, userId.Value, ct);
            return NoContent();
        }
        catch (PermissionNotFoundException)
        {
            return NotFound(CreateError("PERMISSION_NOT_FOUND", "Permission not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets pending invitations for the current user.
    /// </summary>
    [HttpGet("invitations/pending")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PendingInvitationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingInvitations(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var invitations = await _permissionService.GetPendingInvitationsAsync(userId.Value, ct);
        return Ok(ApiResponse<IReadOnlyList<PendingInvitationDto>>.Success(invitations));
    }

    /// <summary>
    /// Accepts a pending invitation.
    /// </summary>
    [HttpPost("invitations/{permissionId:guid}/accept")]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(Guid permissionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var permission = await _permissionService.AcceptInvitationAsync(permissionId, userId.Value, ct);
            return Ok(ApiResponse<PermissionDto>.Success(permission));
        }
        catch (PermissionNotFoundException)
        {
            return NotFound(CreateError("PERMISSION_NOT_FOUND", "Invitation not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
        catch (ConflictException ex)
        {
            return Conflict(CreateError("CONFLICT", ex.Message));
        }
    }

    /// <summary>
    /// Declines a pending invitation.
    /// </summary>
    [HttpPost("invitations/{permissionId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeclineInvitation(Guid permissionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _permissionService.DeclineInvitationAsync(permissionId, userId.Value, ct);
            return NoContent();
        }
        catch (PermissionNotFoundException)
        {
            return NotFound(CreateError("PERMISSION_NOT_FOUND", "Invitation not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
        catch (ConflictException ex)
        {
            return Conflict(CreateError("CONFLICT", ex.Message));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static ApiErrorResponse CreateError(string code, string message) =>
        new(new ApiError(code, message), new ApiMetadata(DateTime.UtcNow));

    private static ApiErrorResponse CreateValidationError(FluentValidation.Results.ValidationResult validation) =>
        new(
            new ApiError(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                validation.Errors.Select(e => new ApiErrorDetail(e.PropertyName, e.ErrorMessage))),
            new ApiMetadata(DateTime.UtcNow));
}

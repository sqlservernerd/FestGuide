namespace FestConnect.Api.Models;

/// <summary>
/// Standard API response wrapper.
/// </summary>
public record ApiResponse<T>(T Data, ApiMetadata Meta)
{
    public static ApiResponse<T> Success(T data) =>
        new(data, new ApiMetadata(DateTime.UtcNow));
}

/// <summary>
/// Standard API error response.
/// </summary>
public record ApiErrorResponse(ApiError Error, ApiMetadata Meta);

/// <summary>
/// API error details.
/// </summary>
public record ApiError(
    string Code,
    string Message,
    IEnumerable<ApiErrorDetail>? Details = null);

/// <summary>
/// API error detail for validation errors.
/// </summary>
public record ApiErrorDetail(string Field, string Message);

/// <summary>
/// API response metadata.
/// </summary>
public record ApiMetadata(DateTime Timestamp, string? CorrelationId = null);

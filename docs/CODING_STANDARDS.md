# 🎵 FestGuide - Coding Standards

---

## Document Control

| **Document Title** | FestGuide - Coding Standards |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Overview

### 1.1 Purpose

This document establishes coding standards and conventions for the FestGuide project to ensure consistency, maintainability, and quality across the codebase.

### 1.2 Scope

These standards apply to all code in the FestGuide solution:
- C# (.NET 10)
- SQL Server (T-SQL)
- XAML (.NET MAUI)
- Configuration files (JSON, YAML)

### 1.3 Guiding Principles

| **Principle** | **Description** |
|---|---|
| Readability | Code should be easy to read and understand |
| Consistency | Follow established patterns throughout the codebase |
| Simplicity | Prefer simple solutions over clever ones |
| Maintainability | Write code that is easy to modify and extend |
| Testability | Design for testability from the start |

---

## 2. C# Coding Standards

### 2.1 Naming Conventions

| **Element** | **Convention** | **Example** |
|---|---|---|
| Namespaces | PascalCase | `FestGuide.Application.Services` |
| Classes | PascalCase | `FestivalService` |
| Interfaces | IPascalCase | `IFestivalRepository` |
| Methods | PascalCase | `GetFestivalByIdAsync` |
| Properties | PascalCase | `FestivalName` |
| Public Fields | PascalCase | `MaxRetryCount` |
| Private Fields | _camelCase | `_festivalRepository` |
| Parameters | camelCase | `festivalId` |
| Local Variables | camelCase | `festivalCount` |
| Constants | PascalCase | `DefaultPageSize` |
| Enums | PascalCase | `FestivalStatus` |
| Enum Values | PascalCase | `Published`, `Draft` |
| Type Parameters | T or TPascalCase | `T`, `TEntity` |

### 2.2 Naming Guidelines

```csharp
// ✓ Good: Descriptive names
public async Task<Festival> GetFestivalByIdAsync(Guid festivalId)

// ✗ Bad: Abbreviated or unclear names
public async Task<Festival> GetFest(Guid id)

// ✓ Good: Boolean properties start with Is, Has, Can
public bool IsPublished { get; set; }
public bool HasSchedule { get; set; }
public bool CanEdit { get; set; }

// ✓ Good: Async methods end with Async
public Task<Festival> GetFestivalAsync(Guid id)
public Task SaveChangesAsync(CancellationToken ct)

// ✓ Good: Event handlers use EventHandler pattern
public event EventHandler<SchedulePublishedEventArgs> SchedulePublished;
```

### 2.3 File Organization

Each file should contain a single public type:

```csharp
// FestivalService.cs
namespace FestGuide.Application.Services;

public class FestivalService : IFestivalService
{
    // Implementation
}
```

**File Structure Order:**
1. Using directives (sorted alphabetically)
2. Namespace declaration
3. Type declaration
4. Constants
5. Static fields
6. Instance fields
7. Constructors
8. Properties
9. Public methods
10. Internal methods
11. Protected methods
12. Private methods
13. Nested types

### 2.4 Formatting

```csharp
// Braces on new lines (Allman style)
public class FestivalService
{
    public void ProcessFestival()
    {
        if (condition)
        {
            // Implementation
        }
    }
}

// Single statement if - still use braces
if (festival == null)
{
    throw new ArgumentNullException(nameof(festival));
}

// Line length: max 120 characters
// Indent: 4 spaces (no tabs)
```

### 2.5 Using Directives

```csharp
// System namespaces first, then third-party, then project
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using NodaTime;

using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
```

### 2.6 Null Handling

```csharp
// Enable nullable reference types
#nullable enable

// Use null-conditional operator
var name = festival?.Name;

// Use null-coalescing operator
var displayName = festival?.Name ?? "Unknown";

// Guard clauses at method start
public async Task<Festival> GetFestivalAsync(Guid festivalId, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(festivalId);
    
    var festival = await _repository.GetByIdAsync(festivalId, ct);
    
    return festival ?? throw new FestivalNotFoundException(festivalId);
}
```

### 2.7 Async/Await Patterns

```csharp
// ✓ Good: Use async/await throughout
public async Task<Festival> GetFestivalAsync(Guid id, CancellationToken ct)
{
    var festival = await _repository.GetByIdAsync(id, ct);
    return festival;
}

// ✓ Good: Always pass CancellationToken
public async Task ProcessAsync(CancellationToken ct = default)
{
    await _service.DoWorkAsync(ct);
}

// ✗ Bad: Blocking on async code
public Festival GetFestival(Guid id)
{
    return _repository.GetByIdAsync(id).Result; // Deadlock risk!
}

// ✓ Good: ConfigureAwait(false) in library code
public async Task<Festival> GetFestivalAsync(Guid id, CancellationToken ct)
{
    var festival = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
    return festival;
}
```

### 2.8 Exception Handling

```csharp
// Define custom exceptions for domain errors
public class FestivalNotFoundException : DomainException
{
    public FestivalNotFoundException(Guid festivalId)
        : base($"Festival with ID {festivalId} was not found.")
    {
        FestivalId = festivalId;
    }

    public Guid FestivalId { get; }
}

// Catch specific exceptions
try
{
    await _service.ProcessAsync(ct);
}
catch (FestivalNotFoundException ex)
{
    _logger.LogWarning(ex, "Festival not found: {FestivalId}", ex.FestivalId);
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing festival");
    throw;
}

// Never swallow exceptions silently
// ✗ Bad
catch (Exception) { }
```

### 2.9 Dependency Injection

```csharp
// Constructor injection only
public class FestivalService : IFestivalService
{
    private readonly IFestivalRepository _festivalRepository;
    private readonly ILogger<FestivalService> _logger;
    private readonly ITimezoneService _timezoneService;

    public FestivalService(
        IFestivalRepository festivalRepository,
        ILogger<FestivalService> logger,
        ITimezoneService timezoneService)
    {
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));
    }
}

// Register services in composition root
services.AddScoped<IFestivalService, FestivalService>();
services.AddScoped<IFestivalRepository, SqlServerFestivalRepository>();
```

### 2.10 LINQ Usage

```csharp
// ✓ Good: Method syntax for complex queries
var activeEditions = festivals
    .Where(f => !f.IsDeleted)
    .SelectMany(f => f.Editions)
    .Where(e => e.Status == EditionStatus.Published)
    .OrderBy(e => e.StartDateUtc)
    .ToList();

// ✓ Good: Query syntax for joins
var scheduleItems = 
    from e in engagements
    join ts in timeSlots on e.TimeSlotId equals ts.TimeSlotId
    join a in artists on e.ArtistId equals a.ArtistId
    select new ScheduleItemDto(a.Name, ts.StartTimeUtc, ts.EndTimeUtc);

// Avoid multiple enumerations
var festivals = await _repository.GetAllAsync(ct);
var count = festivals.Count;    // ✓ Materialized
var first = festivals.First();  // ✓ Uses same collection

// ✓ Good: Explicit filtering in foreach loops
// Always use .Where() to filter explicitly rather than if statements inside the loop
foreach (var artist in artists.Where(a => a != null))
{
    artistDictionary[artist!.ArtistId] = artist;
}

// ✗ Bad: Implicit filtering with if inside foreach
foreach (var artist in artists)
{
    if (artist != null)
    {
        artistDictionary[artist.ArtistId] = artist;
    }
}
```

---

## 3. Architecture Patterns

### 3.1 Layer Dependencies

```
┌──────────────────────────────────────────────────────────────┐
│                          API Layer                            │
│                    (Controllers, DTOs)                        │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                      Application Layer                        │
│               (Services, Commands, Queries)                   │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                        Domain Layer                           │
│               (Entities, Value Objects, Enums)                │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                      Data Access Layer                        │
│                  (Repositories, Dapper)                       │
└──────────────────────────────────────────────────────────────┘
```

**Dependency Rules:**
- Upper layers depend on lower layers
- Never reference upward
- Domain layer has no external dependencies

### 3.2 Repository Pattern

```csharp
// Repository interface in DataAccess.Abstractions
public interface IFestivalRepository
{
    Task<Festival?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Festival>> GetAllAsync(CancellationToken ct);
    Task<Guid> CreateAsync(Festival festival, CancellationToken ct);
    Task UpdateAsync(Festival festival, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

// Implementation in DataAccess
public class SqlServerFestivalRepository : IFestivalRepository
{
    private readonly IDbConnection _connection;

    public async Task<Festival?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = """
            SELECT FestivalId, Name, Description, OwnerUserId, CreatedAtUtc
            FROM core.Festival
            WHERE FestivalId = @Id AND IsDeleted = 0
            """;

        return await _connection.QuerySingleOrDefaultAsync<Festival>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
```

### 3.3 Service Pattern

```csharp
// Service interface
public interface IFestivalService
{
    Task<FestivalDto> GetFestivalAsync(Guid id, CancellationToken ct);
    Task<Guid> CreateFestivalAsync(CreateFestivalRequest request, CancellationToken ct);
    Task UpdateFestivalAsync(Guid id, UpdateFestivalRequest request, CancellationToken ct);
    Task DeleteFestivalAsync(Guid id, CancellationToken ct);
}

// Service implementation
public class FestivalService : IFestivalService
{
    private readonly IFestivalRepository _repository;
    private readonly IAuthorizationService _authService;
    private readonly ILogger<FestivalService> _logger;

    public async Task<FestivalDto> GetFestivalAsync(Guid id, CancellationToken ct)
    {
        var festival = await _repository.GetByIdAsync(id, ct)
            ?? throw new FestivalNotFoundException(id);

        return FestivalDto.FromEntity(festival);
    }
}
```

### 3.4 DTO Pattern

```csharp
// Request DTOs (input)
public sealed record CreateFestivalRequest(
    string Name,
    string? Description,
    string? WebsiteUrl);

// Response DTOs (output)
public sealed record FestivalDto(
    Guid FestivalId,
    string Name,
    string? Description,
    string? WebsiteUrl,
    DateTime CreatedAtUtc)
{
    public static FestivalDto FromEntity(Festival entity) =>
        new(
            entity.FestivalId,
            entity.Name,
            entity.Description,
            entity.WebsiteUrl,
            entity.CreatedAtUtc);
}
```

---

## 4. API Standards

### 4.1 Controller Structure

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class FestivalsController : ControllerBase
{
    private readonly IFestivalService _festivalService;
    private readonly ILogger<FestivalsController> _logger;

    public FestivalsController(
        IFestivalService festivalService,
        ILogger<FestivalsController> logger)
    {
        _festivalService = festivalService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a festival by ID.
    /// </summary>
    /// <param name="id">The festival ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The festival details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FestivalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFestival(Guid id, CancellationToken ct)
    {
        var festival = await _festivalService.GetFestivalAsync(id, ct);
        return Ok(ApiResponse.Success(festival));
    }
}
```

### 4.2 Validation

```csharp
// Use FluentValidation
public class CreateFestivalRequestValidator : AbstractValidator<CreateFestivalRequest>
{
    public CreateFestivalRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Festival name is required and must be 200 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => x.Description != null);

        RuleFor(x => x.WebsiteUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Website URL must be valid");
    }

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var result) &&
        (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
}
```

---

## 5. Database Standards

### 5.1 SQL Naming Conventions

| **Object** | **Convention** | **Example** |
|---|---|---|
| Tables | PascalCase, singular | `Festival`, `TimeSlot` |
| Columns | PascalCase | `FestivalId`, `CreatedAtUtc` |
| DateTime columns | Suffix with `Utc` | `StartTimeUtc` |
| Primary Keys | `{Table}Id` | `FestivalId` |
| Foreign Keys | `{ReferencedTable}Id` | `FestivalId` |
| Indexes | `IX_{Table}_{Columns}` | `IX_Festival_Name` |
| Unique Constraints | `UQ_{Table}_{Columns}` | `UQ_User_Email` |
| Check Constraints | `CK_{Table}_{Description}` | `CK_TimeSlot_EndAfterStart` |

### 5.2 Query Patterns

```csharp
// Always use parameterized queries
const string sql = """
    SELECT FestivalId, Name, Description
    FROM core.Festival
    WHERE FestivalId = @FestivalId
      AND IsDeleted = 0
    """;

var festival = await connection.QuerySingleOrDefaultAsync<Festival>(
    sql, new { FestivalId = id });

// Never concatenate user input
// ✗ Bad - SQL Injection vulnerability!
var sql = $"SELECT * FROM Festival WHERE Name = '{userInput}'";
```

### 5.3 Stored Procedures

```sql
-- Use schemas
CREATE PROCEDURE [core].[usp_Festival_GetById]
    @FestivalId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        FestivalId,
        Name,
        Description,
        OwnerUserId,
        CreatedAtUtc,
        ModifiedAtUtc
    FROM core.Festival
    WHERE FestivalId = @FestivalId
      AND IsDeleted = 0;
END;
```

---

## 6. Testing Standards

### 6.1 Test Naming Convention

```csharp
// Pattern: Method_Scenario_ExpectedResult
[Fact]
public async Task GetFestivalAsync_WithValidId_ReturnsFestival()
{
    // Arrange
    var festivalId = Guid.NewGuid();
    var expectedFestival = new Festival { FestivalId = festivalId, Name = "Test" };
    _mockRepository.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedFestival);

    // Act
    var result = await _service.GetFestivalAsync(festivalId, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(festivalId, result.FestivalId);
}

[Fact]
public async Task GetFestivalAsync_WithInvalidId_ThrowsNotFoundException()
{
    // Arrange
    var festivalId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Festival?)null);

    // Act & Assert
    await Assert.ThrowsAsync<FestivalNotFoundException>(
        () => _service.GetFestivalAsync(festivalId, CancellationToken.None));
}
```

### 6.2 Test Organization

```
tests/
├── FestGuide.Application.Tests/
│   ├── Services/
│   │   ├── FestivalServiceTests.cs
│   │   └── ScheduleServiceTests.cs
│   └── Validators/
│       └── CreateFestivalRequestValidatorTests.cs
├── FestGuide.Api.Tests/
│   └── Controllers/
│       └── FestivalsControllerTests.cs
└── FestGuide.Integration.Tests/
    └── Endpoints/
        └── FestivalEndpointTests.cs
```

### 6.3 Test Coverage Requirements

| **Layer** | **Minimum Coverage** |
|---|---|
| Application (Services) | 80% |
| Domain (Entities) | 70% |
| API (Controllers) | 70% |
| Data Access | Integration tests |

---

## 7. Logging Standards

### 7.1 Log Levels

| **Level** | **Usage** |
|---|---|
| `Error` | Exceptions, failures requiring attention |
| `Warning` | Unusual conditions, potential issues |
| `Information` | Significant application events |
| `Debug` | Detailed diagnostic information |

### 7.2 Structured Logging

```csharp
// ✓ Good: Use structured logging with named properties
_logger.LogInformation(
    "Festival {FestivalId} created by user {UserId}",
    festival.FestivalId,
    userId);

// ✗ Bad: String interpolation (loses structure)
_logger.LogInformation(
    $"Festival {festival.FestivalId} created by user {userId}");

// ✓ Good: Include relevant context
_logger.LogError(
    ex,
    "Failed to publish schedule for edition {EditionId}. Affected attendees: {AttendeeCount}",
    editionId,
    affectedAttendees);
```

### 7.3 What to Log

| **Do Log** | **Don't Log** |
|---|---|
| Authentication events | Passwords or tokens |
| Authorization failures | Full request bodies with PII |
| Business events | Credit card numbers |
| Performance metrics | Social security numbers |
| Errors with context | Excessive debug info in production |

---

## 8. Documentation Standards

### 8.1 XML Documentation

```csharp
/// <summary>
/// Retrieves a festival by its unique identifier.
/// </summary>
/// <param name="festivalId">The unique identifier of the festival.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>The festival DTO if found.</returns>
/// <exception cref="FestivalNotFoundException">
/// Thrown when no festival exists with the specified ID.
/// </exception>
public async Task<FestivalDto> GetFestivalAsync(
    Guid festivalId,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

### 8.2 Code Comments

```csharp
// ✓ Good: Explain why, not what
// Cache festival data for 5 minutes to reduce database load
// during high-traffic festival events
_cache.Set(cacheKey, festival, TimeSpan.FromMinutes(5));

// ✗ Bad: Explain what (code already does that)
// Set the festival in cache
_cache.Set(cacheKey, festival);

// ✓ Good: Document non-obvious business rules
// Festivals are archived 3 months after their end date per
// product requirement REQ-042 to reduce clutter for attendees
var archiveDate = edition.EndDateUtc.AddMonths(3);
```

---

## 9. Security Standards

### 9.1 Input Validation

```csharp
// Always validate input at API boundary
public IActionResult CreateFestival([FromBody] CreateFestivalRequest request)
{
    // FluentValidation handles this automatically
}

// Sanitize output to prevent XSS
public string SanitizeHtml(string input) =>
    HtmlEncoder.Default.Encode(input);
```

### 9.2 Authentication

```csharp
// Use authorization attributes
[Authorize]
[HttpPost]
public async Task<IActionResult> CreateFestival(CreateFestivalRequest request)

// Check permissions explicitly
if (!await _authService.CanEditFestivalAsync(userId, festivalId, ct))
{
    return Forbid();
}
```

### 9.3 Secrets

```csharp
// ✓ Good: Use configuration/secrets
var connectionString = _configuration.GetConnectionString("DefaultConnection");

// ✗ Bad: Hardcoded secrets
var connectionString = "Server=prod;Database=FestGuide;User=admin;Password=secret123";
```

---

## 10. Code Review Checklist

### 10.1 Functionality
- [ ] Code meets requirements
- [ ] Edge cases handled
- [ ] Error handling appropriate

### 10.2 Code Quality
- [ ] Follows naming conventions
- [ ] No code duplication
- [ ] Methods are focused (single responsibility)
- [ ] No dead code

### 10.3 Security
- [ ] No hardcoded secrets
- [ ] Input validated
- [ ] Authorization checked
- [ ] SQL injection prevented

### 10.4 Performance
- [ ] No N+1 queries
- [ ] Appropriate caching
- [ ] Async used correctly

### 10.5 Testing
- [ ] Unit tests added/updated
- [ ] Edge cases tested
- [ ] Tests are meaningful

---

*This document is a living artifact and will be updated as standards evolve.*

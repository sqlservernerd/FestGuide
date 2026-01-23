# GitHub Copilot Instructions for FestConnect

This file provides context and guidelines for GitHub Copilot when working with the FestConnect codebase.

## Project Overview

FestConnect is a festival and events guide application built with .NET 10, featuring:
- **Backend**: ASP.NET Web API with JWT authentication
- **Frontend**: .NET MAUI (Blazor Hybrid) for cross-platform mobile
- **Database**: SQL Server with Dapper ORM
- **Architecture**: Clean Architecture with layered separation

## Solution Structure

The solution follows a clean architecture pattern with clear layer separation:

```
src/
├── Presentation/              # .NET MAUI Blazor Hybrid UI
│   └── FestConnect.Presentation.Maui/
├── Interface/                 # REST API endpoints
│   └── FestConnect.Api/
├── Application/               # Business logic, services
│   └── FestConnect.Application/
├── DataAccess/               # Dapper repositories
│   ├── FestConnect.DataAccess/
│   └── FestConnect.DataAccess.Abstractions/
├── Domain/                   # Core entities, enums, exceptions
│   └── FestConnect.Domain/
├── Infrastructure/           # Cross-cutting concerns
│   ├── FestConnect.Infrastructure/
│   └── FestConnect.Security/
├── Integrations/             # External integrations
│   └── FestConnect.Integrations/
└── Database/                 # SQL Server SSDT project
    └── FestConnect.Database/
```

### Layer Dependencies

- Upper layers depend on lower layers (never reference upward)
- Domain layer has NO external dependencies
- Application layer depends only on Domain and DataAccess.Abstractions
- API layer depends on Application, Infrastructure, and Security

## Coding Standards

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespaces | PascalCase | `FestConnect.Application.Services` |
| Classes | PascalCase | `FestivalService` |
| Interfaces | IPascalCase | `IFestivalRepository` |
| Methods | PascalCase | `GetFestivalByIdAsync` |
| Properties | PascalCase | `FestivalName` |
| Private Fields | _camelCase | `_festivalRepository` |
| Parameters | camelCase | `festivalId` |
| Constants | PascalCase | `DefaultPageSize` |

### Key Guidelines

1. **Async Methods**: Always end with `Async` suffix
2. **Nullable Reference Types**: Enabled - use `?` for nullable types
3. **Bracing Style**: Allman style (braces on new lines)
4. **Max Line Length**: 120 characters
5. **Indentation**: 4 spaces (no tabs)
6. **Boolean Properties**: Prefix with `Is`, `Has`, or `Can`
7. **CancellationToken**: Always pass `ct` parameter in async methods

### File Organization

Each file contains a single public type with members ordered as:
1. Constants
2. Static fields
3. Instance fields (private readonly first)
4. Constructors
5. Properties
6. Public methods
7. Internal methods
8. Protected methods
9. Private methods
10. Nested types

## Architecture Patterns

### Repository Pattern

```csharp
// Interface in DataAccess.Abstractions
public interface IFestivalRepository
{
    Task<Festival?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Festival>> GetAllAsync(CancellationToken ct);
    Task<Guid> CreateAsync(Festival festival, CancellationToken ct);
    Task UpdateAsync(Festival festival, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

// Implementation in DataAccess using Dapper
public class SqlServerFestivalRepository : IFestivalRepository
{
    private readonly IDbConnection _connection;
    // Implementation using Dapper with parameterized queries
}
```

### Service Pattern

```csharp
// Service in Application layer
public class FestivalService : IFestivalService
{
    private readonly IFestivalRepository _repository;
    private readonly ILogger<FestivalService> _logger;

    // Constructor injection only
    public FestivalService(
        IFestivalRepository repository,
        ILogger<FestivalService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### DTO Pattern

```csharp
// Use records for DTOs
public sealed record CreateFestivalRequest(
    string Name,
    string? Description,
    string? WebsiteUrl);

public sealed record FestivalDto(
    Guid FestivalId,
    string Name,
    DateTime CreatedAtUtc)
{
    public static FestivalDto FromEntity(Festival entity) =>
        new(entity.FestivalId, entity.Name, entity.CreatedAtUtc);
}
```

## Database Standards

### SQL Conventions

- **Tables**: PascalCase, singular (`Festival`, `TimeSlot`)
- **Columns**: PascalCase (`FestivalId`, `Name`)
- **DateTime Columns**: Always suffix with `Utc` (`CreatedAtUtc`)
- **Primary Keys**: `{Table}Id` (`FestivalId`)
- **Foreign Keys**: `{ReferencedTable}Id`
- **Indexes**: `IX_{Table}_{Columns}`
- **Always use parameterized queries** - NEVER concatenate user input

### Query Patterns

```csharp
// Always use CommandDefinition with cancellation token
const string sql = """
    SELECT FestivalId, Name, Description
    FROM core.Festival
    WHERE FestivalId = @FestivalId AND IsDeleted = 0
    """;

var festival = await connection.QuerySingleOrDefaultAsync<Festival>(
    new CommandDefinition(sql, new { FestivalId = id }, cancellationToken: ct));
```

## API Standards

### Controller Structure

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class FestivalsController : ControllerBase
{
    private readonly IFestivalService _service;
    private readonly ILogger<FestivalsController> _logger;

    // Constructor injection
    // XML documentation comments required
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FestivalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFestival(Guid id, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Validation

- Use FluentValidation for request validation
- Validate at API boundary
- Return structured error responses

## Testing Standards

### Test Framework Stack

- **xUnit** for test framework
- **Moq** for mocking
- **FluentAssertions** for assertions
- **AutoFixture** for test data generation

### Test Naming

Pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `GetFestivalAsync_WithValidId_ReturnsFestival`
- `GetFestivalAsync_WithInvalidId_ThrowsNotFoundException`
- `CreateFestivalAsync_WithValidRequest_ReturnsCreatedFestival`

### Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task GetFestivalAsync_WithValidId_ReturnsFestival()
{
    // Arrange
    var festivalId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedFestival);

    // Act
    var result = await _sut.GetFestivalAsync(festivalId, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.FestivalId.Should().Be(festivalId);
}
```

### Coverage Requirements

| Project | Target | Minimum |
|---------|--------|---------|
| Application | 85% | 80% |
| Domain | 80% | 70% |
| API | 75% | 70% |

## Security Guidelines

### Input Validation

- Always validate input at API boundary
- Use FluentValidation for structured validation
- Sanitize HTML output to prevent XSS
- Never trust client input

### Authentication & Authorization

```csharp
// Use authorization attributes
[Authorize]
[HttpPost]
public async Task<IActionResult> CreateFestival(CreateFestivalRequest request)

// Check permissions explicitly when needed
if (!await _authService.CanEditFestivalAsync(userId, festivalId, ct))
{
    return Forbid();
}
```

### Secrets Management

- ✅ Use configuration/secrets: `_configuration.GetConnectionString("DefaultConnection")`
- ❌ Never hardcode secrets in source code
- ❌ Never log sensitive data (passwords, tokens, PII, credit cards)

### SQL Injection Prevention

- Always use parameterized queries with Dapper
- Never concatenate user input into SQL strings

## Logging Standards

### Structured Logging

```csharp
// ✅ Good: Use structured logging with named properties
_logger.LogInformation(
    "Festival {FestivalId} created by user {UserId}",
    festival.FestivalId,
    userId);

// ❌ Bad: String interpolation (loses structure)
_logger.LogInformation($"Festival {festival.FestivalId} created");
```

### Log Levels

- **Error**: Exceptions, failures requiring attention
- **Warning**: Unusual conditions, potential issues
- **Information**: Significant application events
- **Debug**: Detailed diagnostic information

## Build & Run

### Prerequisites

- .NET 10 SDK
- For MAUI: `dotnet workload install maui`
- For Database: SQL Server Data Tools

### Common Commands

```bash
# Restore packages
dotnet restore FestConnect.sln

# Build all projects
dotnet build FestConnect.sln

# Build specific project
dotnet build src/Interface/FestConnect.Api/FestConnect.Api.csproj

# Run tests
dotnet test FestConnect.sln

# Run API locally
dotnet run --project src/Interface/FestConnect.Api/FestConnect.Api.csproj
```

### Central Package Management

- All package versions are managed in `Directory.Packages.props`
- Individual projects reference packages WITHOUT version numbers
- To update a package version, edit `Directory.Packages.props`

## Exception Handling

### Custom Exceptions

```csharp
// Define domain-specific exceptions
public class FestivalNotFoundException : DomainException
{
    public FestivalNotFoundException(Guid festivalId)
        : base($"Festival with ID {festivalId} was not found.")
    {
        FestivalId = festivalId;
    }

    public Guid FestivalId { get; }
}
```

### Exception Patterns

```csharp
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
    _logger.LogError(ex, "Unexpected error");
    throw;
}

// Never swallow exceptions silently
// ❌ Bad: catch (Exception) { }
```

## Dependency Injection

- Use constructor injection ONLY
- Register services in composition root:
  - `AddScoped<IFestivalService, FestivalService>()`
  - `AddScoped<IFestivalRepository, SqlServerFestivalRepository>()`
- Validate dependencies are not null in constructor

## Async/Await Patterns

```csharp
// ✅ Always use async/await throughout
public async Task<Festival> GetFestivalAsync(Guid id, CancellationToken ct)
{
    var festival = await _repository.GetByIdAsync(id, ct);
    return festival;
}

// ✅ Always pass CancellationToken
public async Task ProcessAsync(CancellationToken ct = default)
{
    await _service.DoWorkAsync(ct);
}

// ✅ Use ConfigureAwait(false) in library code
public async Task<Festival> GetFestivalAsync(Guid id, CancellationToken ct)
{
    var festival = await _repository
        .GetByIdAsync(id, ct)
        .ConfigureAwait(false);
    return festival;
}

// ❌ Never block on async code (.Result, .Wait())
```

## What NOT to Do

- ❌ Don't reference layers upward (violates clean architecture)
- ❌ Don't put business logic in controllers
- ❌ Don't use `var` when type is not obvious from the right side
- ❌ Don't use magic strings or numbers (use constants or configuration)
- ❌ Don't commit commented-out code
- ❌ Don't use `catch (Exception)` without logging and re-throwing
- ❌ Don't use blocking calls on async code (`.Result`, `.Wait()`)
- ❌ Don't forget to dispose `IDisposable` resources (use `using`)

## Additional Resources

See the `docs/` directory for detailed documentation:
- `CODING_STANDARDS.md` - Comprehensive coding standards
- `TECHNICAL_ARCHITECTURE.md` - Architecture details
- `TESTING_STRATEGY.md` - Testing guidelines
- `SECURITY_GUIDELINES.md` - Security best practices
- `API_SPECIFICATION.md` - API design and specifications
- `DATABASE_SCHEMA.md` - Database schema documentation

# 🎵 FestConnect - Testing Strategy

---

## Document Control

| **Document Title** | FestConnect - Testing Strategy |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Overview

### 1.1 Purpose

This document defines the testing strategy for FestConnect, establishing testing types, coverage requirements, tools, and processes to ensure software quality.

### 1.2 Testing Objectives

| **Objective** | **Description** |
|---|---|
| Quality Assurance | Verify functionality meets requirements |
| Bug Prevention | Catch issues before production |
| Regression Prevention | Ensure changes don't break existing functionality |
| Performance Validation | Confirm system meets performance requirements |
| Security Verification | Validate security controls work as expected |

### 1.3 Testing Pyramid

```
                    ┌───────────────┐
                    │   Manual/     │    ← Exploratory, UAT
                    │   E2E Tests   │       (Few, Expensive)
                    └───────────────┘
                  ┌───────────────────┐
                  │  Integration      │  ← API, Database
                  │  Tests            │     (Some)
                  └───────────────────┘
              ┌───────────────────────────┐
              │      Unit Tests           │  ← Fast, Isolated
              │                           │     (Many, Cheap)
              └───────────────────────────┘
```

---

## 2. Testing Types

### 2.1 Unit Testing

**Purpose:** Test individual components in isolation.

| **Attribute** | **Value** |
|---|---|
| Scope | Single class/method |
| Dependencies | Mocked |
| Execution Speed | Very fast (<100ms per test) |
| Coverage Target | 80% for business logic |

**What to Unit Test:**
- Application services
- Domain entities and value objects
- Validators
- Utility classes
- Authorization logic

**What NOT to Unit Test:**
- Third-party libraries
- Framework code
- Trivial getters/setters
- Database queries (use integration tests)

### 2.2 Integration Testing

**Purpose:** Test component interactions and external dependencies.

| **Attribute** | **Value** |
|---|---|
| Scope | Multiple components |
| Dependencies | Real or test doubles |
| Execution Speed | Moderate (seconds) |
| Coverage Target | Critical paths |

**What to Integration Test:**
- Repository implementations
- API endpoints
- Database migrations
- External service integrations (Firebase, email)

### 2.3 End-to-End (E2E) Testing

**Purpose:** Test complete user workflows.

| **Attribute** | **Value** |
|---|---|
| Scope | Full application stack |
| Dependencies | Real environment |
| Execution Speed | Slow (minutes) |
| Coverage Target | Critical user journeys |

**Critical E2E Scenarios:**
- User registration and login
- Festival discovery and search
- Personal schedule creation
- Schedule publishing
- Push notification delivery

### 2.4 Performance Testing

**Purpose:** Validate system meets performance requirements.

| **Test Type** | **Purpose** |
|---|---|
| Load Testing | Verify performance under expected load |
| Stress Testing | Find breaking points |
| Endurance Testing | Verify stability over time |
| Spike Testing | Handle sudden traffic increases |

**Performance Targets:**

| **Metric** | **Initial Launch** | **Full Scale** |
|---|---|---|
| Concurrent Users | 2,000 - 5,000 | 400,000 |
| API Response (P95) | < 2 seconds | < 2 seconds |
| API Response (P50) | < 500ms | < 500ms |
| Database Query (P95) | < 200ms | < 200ms |

### 2.5 Security Testing

**Purpose:** Validate security controls and find vulnerabilities.

| **Test Type** | **Frequency** |
|---|---|
| SAST (Static Analysis) | Every build |
| DAST (Dynamic Analysis) | Weekly |
| Dependency Scanning | Daily |
| Penetration Testing | Annually / Major releases |

---

## 3. Test Organization

### 3.1 Project Structure

```
tests/
├── FestConnect.Application.Tests/           # Application layer unit tests
│   ├── Services/
│   │   ├── FestivalServiceTests.cs
│   │   ├── ScheduleServiceTests.cs
│   │   └── AuthenticationServiceTests.cs
│   ├── Validators/
│   │   └── CreateFestivalRequestValidatorTests.cs
│   └── FestConnect.Application.Tests.csproj
│
├── FestConnect.Api.Tests/                   # API controller unit tests
│   ├── Controllers/
│   │   ├── FestivalsControllerTests.cs
│   │   └── AuthControllerTests.cs
│   └── FestConnect.Api.Tests.csproj
│
├── FestConnect.Domain.Tests/                # Domain logic unit tests
│   ├── Entities/
│   │   └── FestivalTests.cs
│   └── FestConnect.Domain.Tests.csproj
│
├── FestConnect.DataAccess.Tests/            # Repository integration tests
│   ├── Repositories/
│   │   ├── FestivalRepositoryTests.cs
│   │   └── UserRepositoryTests.cs
│   ├── TestDatabaseFixture.cs
│   └── FestConnect.DataAccess.Tests.csproj
│
├── FestConnect.Integration.Tests/           # Full integration tests
│   ├── Endpoints/
│   │   ├── FestivalEndpointTests.cs
│   │   └── AuthEndpointTests.cs
│   ├── WebApplicationFactory.cs
│   └── FestConnect.Integration.Tests.csproj
│
└── FestConnect.Performance.Tests/           # Performance/load tests
    ├── LoadTests/
    │   └── ScheduleLoadTests.cs
    └── FestConnect.Performance.Tests.csproj
```

### 3.2 Test Naming Convention

**Pattern:** `MethodName_Scenario_ExpectedResult`

```csharp
// Examples
[Fact]
public async Task GetFestivalAsync_WithValidId_ReturnsFestival()

[Fact]
public async Task GetFestivalAsync_WithInvalidId_ThrowsNotFoundException()

[Fact]
public async Task CreateFestivalAsync_WithValidRequest_ReturnsCreatedFestival()

[Fact]
public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
```

---

## 4. Unit Testing

### 4.1 Test Framework

| **Tool** | **Purpose** |
|---|---|
| xUnit | Test framework |
| Moq | Mocking framework |
| FluentAssertions | Assertion library |
| AutoFixture | Test data generation |

### 4.2 Unit Test Structure (AAA Pattern)

```csharp
using FluentAssertions;
using Moq;
using Xunit;

public class FestivalServiceTests
{
    private readonly Mock<IFestivalRepository> _mockRepository;
    private readonly Mock<ILogger<FestivalService>> _mockLogger;
    private readonly FestivalService _sut; // System Under Test

    public FestivalServiceTests()
    {
        _mockRepository = new Mock<IFestivalRepository>();
        _mockLogger = new Mock<ILogger<FestivalService>>();
        _sut = new FestivalService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetFestivalAsync_WithValidId_ReturnsFestival()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        var expectedFestival = new Festival 
        { 
            FestivalId = festivalId, 
            Name = "Test Festival" 
        };
        
        _mockRepository
            .Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFestival);

        // Act
        var result = await _sut.GetFestivalAsync(festivalId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FestivalId.Should().Be(festivalId);
        result.Name.Should().Be("Test Festival");
        
        _mockRepository.Verify(
            r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetFestivalAsync_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var festivalId = Guid.NewGuid();
        
        _mockRepository
            .Setup(r => r.GetByIdAsync(festivalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Festival?)null);

        // Act
        var act = () => _sut.GetFestivalAsync(festivalId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FestivalNotFoundException>()
            .WithMessage($"*{festivalId}*");
    }
}
```

### 4.3 Testing Async Code

```csharp
[Fact]
public async Task ProcessScheduleAsync_CancellationRequested_ThrowsOperationCanceledException()
{
    // Arrange
    var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act
    var act = () => _sut.ProcessScheduleAsync(cts.Token);

    // Assert
    await act.Should().ThrowAsync<OperationCanceledException>();
}
```

### 4.4 Testing Validators

```csharp
public class CreateFestivalRequestValidatorTests
{
    private readonly CreateFestivalRequestValidator _validator;

    public CreateFestivalRequestValidatorTests()
    {
        _validator = new CreateFestivalRequestValidator();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        // Arrange
        var request = new CreateFestivalRequest(
            Name: "",
            Description: "A great festival",
            WebsiteUrl: "https://example.com");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com", false)]  // HTTPS required
    [InlineData("not-a-url", false)]
    [InlineData("", true)]  // Optional field
    public void Validate_WebsiteUrl_ValidatesCorrectly(string url, bool expectedValid)
    {
        // Arrange
        var request = new CreateFestivalRequest(
            Name: "Test Festival",
            Description: null,
            WebsiteUrl: url);

        // Act
        var result = _validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Should().NotContain(e => e.PropertyName == "WebsiteUrl");
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "WebsiteUrl");
        }
    }
}
```

---

## 5. Integration Testing

### 5.1 Database Integration Tests

```csharp
public class FestivalRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public FestivalRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_ValidFestival_ReturnsId()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var repository = new SqlServerFestivalRepository(connection);
        
        var festival = new Festival
        {
            Name = "Integration Test Festival",
            Description = "Test Description",
            OwnerUserId = _fixture.TestUserId
        };

        // Act
        var id = await repository.CreateAsync(festival, CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        
        var retrieved = await repository.GetByIdAsync(id, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Integration Test Festival");
    }
}

public class TestDatabaseFixture : IDisposable
{
    public Guid TestUserId { get; }
    private readonly string _connectionString;

    public TestDatabaseFixture()
    {
        _connectionString = GetTestConnectionString();
        InitializeDatabase();
        TestUserId = SeedTestUser();
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        CleanupDatabase();
    }
}
```

### 5.2 API Integration Tests

```csharp
public class FestivalEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public FestivalEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFestival_WithValidId_Returns200()
    {
        // Arrange
        var festivalId = await SeedFestivalAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", await GetTestTokenAsync());

        // Act
        var response = await _client.GetAsync($"/api/v1/festivals/{festivalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<FestivalDto>>();
        content!.Data.FestivalId.Should().Be(festivalId);
    }

    [Fact]
    public async Task GetFestival_WithoutAuth_Returns401()
    {
        // Arrange
        var festivalId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/festivals/{festivalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFestival_WithValidRequest_Returns201()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", await GetOrganizerTokenAsync());

        var request = new CreateFestivalRequest(
            Name: "New Test Festival",
            Description: "Test Description",
            WebsiteUrl: "https://example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/organizer/festivals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace database with test database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbConnection));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddScoped<IDbConnection>(_ =>
            {
                var connection = new SqlConnection(GetTestConnectionString());
                connection.Open();
                return connection;
            });
        });
    }
}
```

---

## 6. Performance Testing

### 6.1 Load Testing with k6

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '2m', target: 100 },    // Ramp up
        { duration: '5m', target: 100 },    // Steady state
        { duration: '2m', target: 500 },    // Peak load
        { duration: '5m', target: 500 },    // Sustained peak
        { duration: '2m', target: 0 },      // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<2000'],  // 95% of requests < 2s
        http_req_failed: ['rate<0.01'],     // < 1% failure rate
    },
};

const BASE_URL = __ENV.BASE_URL || 'https://api.FestConnect.com';

export default function () {
    // Get festival schedule (most common operation)
    const scheduleRes = http.get(`${BASE_URL}/api/v1/editions/${__ENV.EDITION_ID}/schedule`, {
        headers: {
            'Authorization': `Bearer ${__ENV.TOKEN}`,
            'Content-Type': 'application/json',
        },
    });

    check(scheduleRes, {
        'schedule status is 200': (r) => r.status === 200,
        'schedule response time OK': (r) => r.timings.duration < 2000,
    });

    sleep(1);
}
```

### 6.2 Performance Test Scenarios

| **Scenario** | **Users** | **Duration** | **Purpose** |
|---|---|---|---|
| Baseline | 100 | 10 min | Establish baseline metrics |
| Initial Launch | 5,000 | 30 min | Validate initial capacity |
| Full Scale | 400,000 | 1 hour | Validate scale target |
| Spike | 0→50,000→0 | 15 min | Handle sudden load |

---

## 7. Test Data Management

### 7.1 Test Data Strategies

| **Strategy** | **Use Case** |
|---|---|
| In-memory fixtures | Unit tests |
| Database seeding | Integration tests |
| Factories | Dynamic test data |
| Snapshots | Known-good states |

### 7.2 Test Data Factories

```csharp
public static class TestDataFactory
{
    public static Festival CreateFestival(
        Guid? id = null,
        string? name = null,
        Guid? ownerId = null)
    {
        return new Festival
        {
            FestivalId = id ?? Guid.NewGuid(),
            Name = name ?? $"Test Festival {Guid.NewGuid():N}",
            Description = "Test Description",
            OwnerUserId = ownerId ?? Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static FestivalEdition CreateEdition(
        Guid festivalId,
        DateTime? startDate = null,
        string timezoneId = "America/Los_Angeles")
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(1);
        return new FestivalEdition
        {
            EditionId = Guid.NewGuid(),
            FestivalId = festivalId,
            Name = $"Edition {start.Year}",
            StartDateUtc = start,
            EndDateUtc = start.AddDays(3),
            TimezoneId = timezoneId,
            Status = "draft"
        };
    }
}
```

### 7.3 Database Cleanup

```csharp
public class TestDatabaseFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await SeedRequiredDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        using var connection = CreateConnection();
        await connection.ExecuteAsync(@"
            DELETE FROM attendee.PersonalScheduleEntry WHERE 1=1;
            DELETE FROM attendee.PersonalSchedule WHERE 1=1;
            DELETE FROM schedule.Engagement WHERE 1=1;
            DELETE FROM venue.TimeSlot WHERE 1=1;
            DELETE FROM venue.Stage WHERE 1=1;
            DELETE FROM venue.Venue WHERE 1=1;
            DELETE FROM core.Artist WHERE 1=1;
            DELETE FROM core.FestivalEdition WHERE 1=1;
            DELETE FROM core.Festival WHERE 1=1;
        ");
    }
}
```

---

## 8. Continuous Integration

### 8.1 CI Pipeline

```yaml
# .github/workflows/test.yml
name: Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Unit Tests
        run: dotnet test tests/FestConnect.Application.Tests --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          files: ./tests/**/coverage.cobertura.xml

  integration-tests:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: TestPassword123!
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Integration Tests
        run: dotnet test tests/FestConnect.Integration.Tests --no-build --verbosity normal
        env:
          ConnectionStrings__TestDatabase: "Server=localhost;Database=FestConnect_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true"
```

### 8.2 Test Execution Order

```
1. Unit Tests (parallel)
   ├── FestConnect.Application.Tests
   ├── FestConnect.Api.Tests
   └── FestConnect.Domain.Tests

2. Integration Tests (sequential)
   ├── FestConnect.DataAccess.Tests
   └── FestConnect.Integration.Tests

3. Security Scans
   ├── CodeQL Analysis
   └── Dependency Scanning
```

---

## 9. Test Metrics & Reporting

### 9.1 Coverage Requirements

| **Project** | **Target** | **Minimum** |
|---|---|---|
| FestConnect.Application | 85% | 80% |
| FestConnect.Domain | 80% | 70% |
| FestConnect.Api | 75% | 70% |
| FestConnect.DataAccess | Integration tests | N/A |

### 9.2 Quality Gates

| **Metric** | **Threshold** | **Action if Failed** |
|---|---|---|
| Unit Test Pass Rate | 100% | Block merge |
| Integration Test Pass Rate | 100% | Block merge |
| Code Coverage | 80% | Warning |
| New Code Coverage | 80% | Block merge |
| Security Vulnerabilities | 0 critical/high | Block merge |

### 9.3 Test Reports

```xml
<!-- test-results.xml (JUnit format) -->
<testsuites>
  <testsuite name="FestConnect.Application.Tests" tests="150" failures="0" time="5.234">
    <testcase classname="FestivalServiceTests" name="GetFestivalAsync_WithValidId_ReturnsFestival" time="0.123"/>
    <!-- ... -->
  </testsuite>
</testsuites>
```

---

## 10. Testing Checklist

### 10.1 Pre-Commit Checklist

- [ ] All unit tests passing locally
- [ ] New functionality has tests
- [ ] Test names follow convention
- [ ] No skipped tests without justification

### 10.2 PR Review Checklist

- [ ] Tests cover happy path
- [ ] Tests cover edge cases
- [ ] Tests cover error cases
- [ ] No flaky tests introduced
- [ ] Integration tests pass
- [ ] Coverage maintained or improved

### 10.3 Release Checklist

- [ ] All CI tests passing
- [ ] Performance tests completed
- [ ] Security scans clean
- [ ] UAT sign-off (if applicable)

---

*This document is a living artifact and will be updated as testing practices evolve.*

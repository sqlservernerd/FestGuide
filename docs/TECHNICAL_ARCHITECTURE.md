# 🎵 FestConnect - Technical Architecture Document

---

## Document Control

| **Document Title** | FestConnect - Technical Architecture Document |
|---|---|
| **Version** | 1.1 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

| **Version** | **Date** | **Author** | **Changes** |
|---|---|---|---|
| 1.0 | 2026-01-20 | Project Team | Initial draft |
| 1.1 | 2026-01-20 | Project Team | Fixed project naming consistency (removed errant spaces) |

---

## 1. Introduction

### 1.1 Purpose

This document describes the technical architecture for FestConnect, a mobile-first platform for music festival attendees. It provides a comprehensive view of the system's structure, components, and technical decisions.

### 1.2 Scope

This architecture supports:
- **Initial Launch**: 1 festival, 2,000-5,000 attendees
- **Full Scale**: 400,000 concurrent users (2-year horizon)

### 1.3 Architectural Principles

| **Principle** | **Description** |
|---|---|
| **Attendee-First** | Every design decision prioritizes the attendee experience |
| **Offline-First** | Mobile clients function fully without connectivity |
| **Security by Design** | Security at every layer, not bolted on |
| **Portability** | No vendor lock-in; infrastructure-agnostic |
| **Separation of Concerns** | Clear boundaries between layers |
| **Scalability** | Architecture supports growth from 5K to 400K users |

---

## 2. High-Level Architecture

### 2.1 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              PRESENTATION LAYER                                  │
│                           (.NET MAUI - iOS, Android, Web)                        │
│                         Offline-First │ Local Caching │ Timezone Display        │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │ HTTPS / JSON
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              INTERFACE LAYER                                     │
│                              (ASP.NET Web API)                                   │
│     JWT Auth │ API Keys │ CORS │ Rate Limiting │ Validation │ UTC Timestamps   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                             APPLICATION LAYER                                    │
│                              (Business Logic)                                    │
│     Authorization │ Orchestration │ Timezone Conversion │ Notification Triggers │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            DATA ACCESS LAYER                                     │
│                         (Dapper + Repository Pattern)                           │
│                    Distributed Caching │ Database Abstraction                   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          DATA PERSISTENCE LAYER                                  │
│                              (SQL Server 2022)                                   │
│                         All DateTime in UTC │ SSDT Projects                     │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│  CROSS-CUTTING:  Caching (all layers) │ Security (all layers) │ Logging/Metrics │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│  EXTERNAL:  Firebase (Push) │ Organizer Websites (Widgets) │ Webhooks │ Social  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Layer Responsibilities

| **Layer** | **Technology** | **Responsibilities** |
|---|---|---|
| **Presentation** | .NET MAUI (Blazor Hybrid) | UI/UX, offline storage, sync engine, local caching, timezone display |
| **Interface** | ASP.NET Web API | REST endpoints, JWT validation, CORS, DTOs, validation, rate limiting, caching |
| **Application** | C# Class Libraries | Business rules, authorization, orchestration, notifications, timezone conversion |
| **Data Access** | Dapper + Repository | Data retrieval/persistence, parameterized queries, caching, DB abstraction |
| **Data Persistence** | SQL Server 2022 + SSDT | Relational storage, referential integrity, stored procedures |

---

## 3. Technology Stack

### 3.1 Core Technologies

| **Layer** | **Technology** | **Version** | **Notes** |
|---|---|---|---|
| Presentation | .NET MAUI | .NET 10 | Blazor Hybrid for Web; Native for iOS/Android |
| Interface | ASP.NET Web API | .NET 10 | REST, JSON, CORS enabled |
| Application | C# Class Libraries | .NET 10 | Business logic, authorization |
| Data Access | Dapper | Latest | Repository pattern, database-agnostic |
| Data Persistence | SQL Server 2022 | Latest | SSDT for schema management |
| Timezone | NodaTime | Latest | IANA timezone database; DST handling |
| Push Notifications | Firebase Cloud Messaging | Latest | iOS, Android, Web push |
| Caching | Redis | Latest | Distributed cache (optional for initial launch) |
| Logging | Serilog + OpenTelemetry | Latest | Structured logging, metrics |
| CI/CD | GitHub Actions | - | Automated build, test, deploy |
| Source Control | GitHub | - | Repository hosting |

### 3.2 Development Tools

| **Tool** | **Purpose** |
|---|---|
| Visual Studio 2025 | Primary IDE |
| SQL Server Management Studio | Database management |
| Azure Data Studio | Cross-platform database tools |
| Postman | API testing |
| Figma | UI/UX design |

---

## 4. Solution Structure

### 4.1 Project Organization

```
FestConnect.sln
│
├── src/
│   ├── FestConnect.Presentation.Maui/        # .NET MAUI (iOS, Android, Web)
│   ├── FestConnect.Api/                      # ASP.NET Web API (Interface Layer)
│   ├── FestConnect.Application/              # Business Logic (Application Layer)
│   ├── FestConnect.DataAccess/               # Dapper Repositories (Data Access Layer)
│   ├── FestConnect.DataAccess.Abstractions/  # Interfaces for DB portability
│   ├── FestConnect.Domain/                   # Domain Entities, Enums, Exceptions
│   ├── FestConnect.Infrastructure/           # Cross-cutting: Caching, Logging, Firebase
│   ├── FestConnect.Security/                 # Cross-cutting: Security utilities
│   ├── FestConnect.Integrations/             # Webhooks, widgets, social sharing
│   └── FestConnect.Database/                 # SQL Server Database Project (SSDT)
│
├── tests/
│   ├── FestConnect.Api.Tests/                # API endpoint unit tests
│   ├── FestConnect.Application.Tests/        # Business logic unit tests
│   ├── FestConnect.DataAccess.Tests/         # Repository integration tests
│   ├── FestConnect.Integrations.Tests/       # Integration feature tests
│   └── FestConnect.Integration.Tests/        # End-to-end integration tests
│
└── docs/
    ├── PROJECT_CHARTER.md
    ├── REQUIREMENTS.md
    └── TECHNICAL_ARCHITECTURE.md
```

### 4.2 Project Dependencies

```
┌─────────────────────────────────────────────────────────────────┐
│                    FestConnect.Presentation.Maui                   │
│                              │                                   │
│                              ▼                                   │
│                       FestConnect.Api ◄──────────────────────────┐│
│                              │                                  ││
│                              ▼                                  ││
│                    FestConnect.Application                        ││
│                         │         │                             ││
│            ┌────────────┘         └────────────┐                ││
│            ▼                                   ▼                ││
│   FestConnect.DataAccess              FestConnect.Infrastructure    ││
│            │                                   │                ││
│            ▼                                   │                ││
│   FestConnect.DataAccess.Abstractions           │                ││
│            │                                   │                ││
│            └───────────────┬───────────────────┘                ││
│                            ▼                                    ││
│                     FestConnect.Domain                            ││
│                            │                                    ││
│                            ▼                                    ││
│                    FestConnect.Security ──────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Domain Model

### 5.1 Entity Relationships

```
┌─────────────┐       ┌──────────────────┐       ┌─────────────┐
│  Festival   │1─────*│ Festival Edition │*─────*│    Venue    │
└─────────────┘       └──────────────────┘       └─────────────┘
       │                      │                        │
       │               (has TimezoneId)                │
       │                      │                        │
       │                      ▼                        ▼
       │              ┌──────────────┐          ┌───────────┐
       │              │   Schedule   │          │   Stage   │
       │              │ (Draft/Pub)  │          └───────────┘
       │              └──────────────┘                │
       │                      │                       │
       │                      ▼                       ▼
       │              ┌──────────────┐          ┌───────────┐
       │              │  Engagement  │◄────────►│ Time Slot │
       │              └──────────────┘          │(UTC times)│
       │                      ▲                 └───────────┘
       ▼                      │
┌─────────────┐               │
│   Artist    │───────────────┘
│(per festival)│
└─────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                   ORGANIZER DATA PUBLISHING                      │
├─────────────┬───────────────────────────────────────────────────┤
│  Organizer  │◄──── User account with organizer role             │
└─────────────┘                                                    
       │                                                             
       ▼                                                             
┌─────────────────────────────────────────────────────────────────┐
│ Festival Permission                                              │
│ ─────────────────                                                │
│ • Owner (1 per festival - auto-assigned on creation)            │
│ • Administrator (invited by Owner/Admin)                         │
│ • Manager (scoped to: venues, schedule, artists, etc.)          │
│ • Viewer (read-only, scoped)                                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    ATTENDEE EXPERIENCE                           │
├─────────────┬───────────────────────────────────────────────────┤
│  Attendee   │◄──── User account with attendee role (PRIMARY)    │
│             │      (optional: PreferredTimezoneId)              │
└─────────────┘                                                    
       │                                                             
       ▼                                                             
┌───────────────────┐       ┌──────────────────────────┐
│ Personal Schedule │──────►│ Personal Schedule Entry  │
│ (per edition)     │1─────*│ (references Engagement)  │
└───────────────────┘       └──────────────────────────┘
```

### 5.2 Core Entities

| **Entity** | **Description** |
|---|---|
| **User** | Base user account (Attendee or Organizer) |
| **Festival** | Recurring event brand; owned by one organizer |
| **FestivalEdition** | Specific instance with dates, lineup, and timezone |
| **Venue** | Physical location with stages; reusable across editions |
| **Stage** | Performance area with time slots |
| **TimeSlot** | Block of time for performance (stored in UTC) |
| **Artist** | Performer; scoped to festival, reusable across editions |
| **Engagement** | Links artist to time slot |
| **Schedule** | Master schedule; draft or published state |
| **FestivalPermission** | User access to festival with role and scope |
| **PersonalSchedule** | Attendee's saved performances for an edition |
| **PersonalScheduleEntry** | Individual performance in personal schedule |

---

## 6. Database Design

### 6.1 Schema Organization

| **Schema** | **Purpose** | **Tables** |
|---|---|---|
| `core` | Core domain entities | Festival, FestivalEdition, Artist |
| `venue` | Venue management | Venue, Stage, TimeSlot |
| `schedule` | Schedule data | Schedule, Engagement |
| `identity` | User accounts | User, RefreshToken |
| `permissions` | Authorization | FestivalPermission, PermissionScope |
| `attendee` | Attendee data | PersonalSchedule, PersonalScheduleEntry |
| `notifications` | Push notifications | DeviceToken, NotificationLog |
| `integrations` | External integrations | ApiKey, WebhookSubscription |
| `audit` | Audit logging | AuditLog |

### 6.2 Naming Conventions

| **Object Type** | **Convention** | **Example** |
|---|---|---|
| Tables | PascalCase, singular | `Festival`, `TimeSlot` |
| Columns | PascalCase | `FestivalId`, `CreatedAtUtc` |
| DateTime columns | Suffix `Utc` | `StartTimeUtc`, `EndTimeUtc` |
| Timezone columns | `TimezoneId` | IANA identifier (e.g., "America/Los_Angeles") |
| Primary keys | `{Table}Id` | `FestivalId`, `UserId` |
| Foreign keys | `{ReferencedTable}Id` | `FestivalId` in `FestivalEdition` |
| Indexes | `IX_{Table}_{Columns}` | `IX_Festival_Name` |
| Unique constraints | `UQ_{Table}_{Columns}` | `UQ_User_Email` |
| Check constraints | `CK_{Table}_{Description}` | `CK_TimeSlot_EndAfterStart` |
| Default constraints | `DF_{Table}_{Column}` | `DF_Festival_CreatedAtUtc` |
| Foreign key constraints | `FK_{Child}_{Parent}` | `FK_FestivalEdition_Festival` |

### 6.3 Database Standards

| **Standard** | **Requirement** |
|---|---|
| Primary Keys | GUID with `NEWSEQUENTIALID()` for clustered index performance |
| DateTime | `datetime2(7)`, always UTC, suffix with `Utc` |
| Timezone Storage | `NVARCHAR(100)` for IANA identifiers |
| Audit Columns | All tables: `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy` |
| Soft Deletes | `IsDeleted` flag + `DeletedAtUtc` where required |
| File Organization | One object per file in SSDT, organized by schema |

### 6.4 Database Portability

| **Strategy** | **Implementation** |
|---|---|
| Repository Abstraction | All data access through `IRepository<T>` interfaces |
| Standard SQL | ANSI-compliant SQL where possible |
| Dapper Flexibility | Works with any ADO.NET provider |
| Avoid Lock-in | Minimize SQL Server-specific features |

```csharp
// Database-agnostic interface
public interface IFestivalRepository
{
    Task<Festival?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Festival>> SearchAsync(FestivalSearchCriteria criteria, CancellationToken ct);
    Task<Guid> CreateAsync(Festival festival, CancellationToken ct);
    Task UpdateAsync(Festival festival, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

// SQL Server implementation (current)
public class SqlServerFestivalRepository : IFestivalRepository { }

// Alternative implementation (if ever needed)
public class PostgreSqlFestivalRepository : IFestivalRepository { }
```

---

## 7. API Design

### 7.1 API Principles

| **Principle** | **Implementation** |
|---|---|
| RESTful | Resource-based URLs, standard HTTP methods |
| JSON | All request/response bodies in JSON |
| Versioning | URL-based versioning (`/api/v1/...`) |
| Timestamps | All timestamps in ISO 8601 UTC format |
| Pagination | Cursor-based for large collections |
| Error Handling | Consistent error response format |

### 7.2 Authentication & Authorization

| **Mechanism** | **Use Case** |
|---|---|
| JWT Bearer Token | User authentication (attendees, organizers) |
| API Key | Public API access for integrations |
| Permission-based | Organizer actions based on role and scope |

### 7.3 Request/Response Format

**Success Response:**
```json
{
  "data": { ... },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

**Error Response:**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": [
      { "field": "email", "message": "Email is required" }
    ]
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z",
    "correlationId": "abc-123"
  }
}
```

### 7.4 Rate Limiting

| **Limit Type** | **Threshold** |
|---|---|
| Per User | 100 requests/minute |
| Per IP (unauthenticated) | 20 requests/minute |
| Per API Key | 1000 requests/minute |

---

## 8. Cross-Cutting Concerns

### 8.1 Timezone Handling

| **Layer** | **Handling** |
|---|---|
| Database | All `datetime` columns store UTC |
| Data Access | Retrieve and persist as UTC only |
| Application | Timezone Conversion Service handles conversions |
| API | All timestamps in ISO 8601 UTC format |
| Presentation | Convert to festival timezone or user preference |

**Implementation:**
```csharp
public interface ITimezoneService
{
    DateTimeOffset ConvertFromUtc(DateTime utcDateTime, string timezoneId);
    DateTime ConvertToUtc(DateTimeOffset localDateTime, string timezoneId);
    bool IsValidTimezone(string timezoneId);
}
```

### 8.2 Caching Strategy

| **Layer** | **Approach** | **Technology** |
|---|---|---|
| Presentation | Local device cache | SQLite/LiteDB |
| Interface | HTTP response caching, ETags | ASP.NET Response Caching |
| Application | In-memory cache | IMemoryCache |
| Data Access | Distributed cache | Redis (optional for initial launch) |

**Cache Invalidation:**
- Time-based expiration (configurable per data type)
- Event-based invalidation (on data changes)
- Cache-aside pattern for data access layer

### 8.3 Security Architecture

| **Layer** | **Security Measures** |
|---|---|
| Presentation | Secure local storage, certificate pinning |
| Interface | JWT validation, API key auth, rate limiting, CORS, HTTPS |
| Application | Authorization logic, permission enforcement |
| Data Access | Parameterized queries (SQL injection prevention) |
| Database | Encryption at rest, minimal privileges |
| Infrastructure | Network segmentation, secrets management |

**Authentication Flow:**
```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  Client  │────►│   API    │────►│  Auth    │────►│   JWT    │
│          │     │          │     │ Service  │     │  Issued  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
     │                                                   │
     │◄──────────────────────────────────────────────────┘
     │           Access Token (15 min) + Refresh Token (7 days)
```

### 8.4 Logging & Monitoring

| **Aspect** | **Implementation** |
|---|---|
| Structured Logging | Serilog with JSON output |
| Correlation IDs | Propagated through all layers |
| Metrics | OpenTelemetry for APM |
| Health Checks | ASP.NET Health Checks middleware |
| Alerting | Threshold-based alerts |

**Log Levels:**
| **Level** | **Use Case** |
|---|---|
| Error | Exceptions, failures requiring attention |
| Warning | Unusual conditions, potential issues |
| Information | Significant application events |
| Debug | Detailed diagnostic information |

---

## 9. Offline-First Architecture

### 9.1 Mobile Client Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      .NET MAUI APPLICATION                       │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   UI Layer  │  │  ViewModel  │  │    Sync Engine          │  │
│  │   (XAML)    │  │   Layer     │  │  ┌─────────────────────┐│  │
│  └─────────────┘  └─────────────┘  │  │ Conflict Resolution ││  │
│         │                │         │  │ (Server Wins)       ││  │
│         ▼                ▼         │  └─────────────────────┘│  │
│  ┌─────────────────────────────┐   └─────────────────────────┘  │
│  │      Service Layer          │              │                 │
│  │  (Offline-aware services)   │◄─────────────┘                 │
│  └─────────────────────────────┘                                │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────────┐   ┌─────────────────────────┐  │
│  │    Local Database           │   │    HTTP Client          │  │
│  │    (SQLite/LiteDB)          │   │    (API calls)          │  │
│  └─────────────────────────────┘   └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 9.2 Sync Strategy

| **Aspect** | **Approach** |
|---|---|
| Sync Direction | Bidirectional (pull festival data, push personal schedule changes) |
| Conflict Resolution | Server wins for organizer data; last-write-wins for personal data |
| Sync Trigger | On app launch, on connectivity change, manual refresh |
| Incremental Sync | Only changed data since last sync (using timestamps) |
| Queue | Offline changes queued locally, synced when online |

### 9.3 Offline Capabilities

| **Feature** | **Offline Support** |
|---|---|
| View personal schedule | ✓ Full |
| View festival schedule | ✓ Cached data |
| Add to personal schedule | ✓ Queued |
| Remove from personal schedule | ✓ Queued |
| Search festivals | ✗ Requires connectivity |
| Receive notifications | ✗ Requires connectivity |

---

## 10. Integration Architecture

### 10.1 Public API

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Organizer     │     │   FestConnect     │     │   Organizer     │
│   Website       │────►│   Public API    │◄────│   Mobile App    │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                               │
                    API Key Authentication
                    Rate Limited (1000/min)
                    Read-Only Access
```

### 10.2 Embeddable Widgets

```
┌─────────────────────────────────────────────────────────────────┐
│                    Organizer Website                             │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  <script src="https://FestConnect.app/widget.js"></script>  │  │
│  │  <div data-FestConnect-widget="schedule"                    │  │
│  │       data-edition-id="abc123"></div>                     │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    FestConnect Widget Service                      │
│  • Lightweight JavaScript                                        │
│  • Fetches data from Public API                                 │
│  • Renders schedule in iframe or shadow DOM                     │
│  • WCAG 2.1 AA compliant                                        │
│  • Cached aggressively (CDN)                                    │
└─────────────────────────────────────────────────────────────────┘
```

### 10.3 Push Notifications (Firebase)

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   FestConnect     │     │    Firebase     │     │   Mobile        │
│   API           │────►│    FCM          │────►│   Device        │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │
   Schedule Published
        │
        ▼
   Identify affected users
   (personal schedules with changed engagements)
        │
        ▼
   Send batch notification to FCM
```

---

## 11. Infrastructure

### 11.1 Initial Launch Infrastructure

For 2,000-5,000 users, minimal infrastructure is required:

| **Component** | **Specification** | **Notes** |
|---|---|---|
| API Server | Single VM/container | 4 CPU, 8GB RAM |
| Database | SQL Server instance | Standard configuration |
| Caching | In-memory (IMemoryCache) | Redis optional |
| File Storage | Local or basic object storage | For any static assets |

### 11.2 Scale Infrastructure (Future)

For 400,000 concurrent users:

| **Component** | **Specification** | **Notes** |
|---|---|---|
| API Servers | Multiple instances behind load balancer | Horizontal scaling |
| Database | SQL Server with read replicas | Or PostgreSQL |
| Caching | Redis cluster | Distributed cache |
| CDN | For static assets and widgets | Edge caching |

### 11.3 Infrastructure Portability

| **Component** | **Self-Hosted** | **Cloud Options** |
|---|---|---|
| Compute | VMs / Docker | Azure App Service, AWS ECS, GCP Cloud Run |
| Database | SQL Server 2022 | Azure SQL, AWS RDS, PostgreSQL |
| Caching | Redis | Azure Cache, ElastiCache |
| Secrets | HashiCorp Vault | Azure Key Vault, AWS Secrets Manager |
| Load Balancing | HAProxy / NGINX | Azure LB, AWS ALB |

---

## 12. Security Architecture

### 12.1 Authentication

| **Aspect** | **Implementation** |
|---|---|
| Password Hashing | Argon2id (memory 64MB, iterations 3, parallelism 4) |
| Password Policy | Minimum 12 characters |
| JWT Access Token | 15-minute expiry |
| Refresh Token | 7-day expiry with rotation |
| Token Storage | Secure storage on mobile; HttpOnly cookies on web |

### 12.2 Authorization

| **Level** | **Implementation** |
|---|---|
| Route-level | `[Authorize]` attributes |
| Resource-level | Permission checks in Application layer |
| Data-level | Repository filters based on user context |

### 12.3 Data Protection

| **Data State** | **Protection** |
|---|---|
| In Transit | TLS 1.3 (minimum 1.2), HSTS |
| At Rest | AES-256 encryption, TDE for database |
| In Memory | Sensitive data cleared after use |

### 12.4 Security Headers

| **Header** | **Value** |
|---|---|
| Content-Security-Policy | Restrictive CSP |
| X-Content-Type-Options | nosniff |
| X-Frame-Options | DENY |
| Referrer-Policy | strict-origin-when-cross-origin |
| Permissions-Policy | Restrictive |

---

## 13. Testing Strategy

### 13.1 Test Pyramid

```
                    ┌───────────────┐
                    │   E2E Tests   │  (Few)
                   ┌┴───────────────┴┐
                   │ Integration Tests│ (Some)
                  ┌┴─────────────────┴┐
                  │    Unit Tests      │ (Many)
                 └────────────────────┘
```

### 13.2 Coverage Targets

| **Layer** | **Test Type** | **Coverage Target** |
|---|---|---|
| Application | Unit tests | 90%+ |
| API | Unit tests | 80%+ |
| Data Access | Integration tests | 70%+ |
| Timezone | Unit tests | 100% |
| End-to-End | Integration tests | Critical paths |

### 13.3 Testing Tools

| **Tool** | **Purpose** |
|---|---|
| xUnit | Test framework |
| Moq | Mocking |
| FluentAssertions | Assertions |
| Testcontainers | Database integration tests |
| k6 | Load testing |
| axe | Accessibility testing |

---

## 14. CI/CD Pipeline

### 14.1 Pipeline Stages

```
┌─────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐
│  Build  │──►│  Test   │──►│ Analyze │──►│ Package │──►│ Deploy  │
└─────────┘   └─────────┘   └─────────┘   └─────────┘   └─────────┘
```

### 14.2 Stage Details

| **Stage** | **Activities** |
|---|---|
| Build | Restore packages, compile code |
| Test | Run unit tests, integration tests |
| Analyze | Static code analysis, security scanning |
| Package | Build Docker images, app packages |
| Deploy | Deploy to target environment |

### 14.3 Environments

| **Environment** | **Purpose** | **Deployment** |
|---|---|---|
| Development | Local development | Manual |
| Integration | CI/CD testing | On PR merge |
| Staging | Pre-production validation | On release branch |
| Production | Live users | Manual approval |

---

## 15. Coding Standards

### 15.1 General Guidelines

| **Standard** | **Requirement** |
|---|---|
| Dependency Injection | Constructor injection; all services via DI |
| Coding Style | Microsoft C# Coding Conventions |
| Security | .NET Secure Coding Guidelines |
| Logging | Structured logging with correlation IDs |
| Timezone | Use NodaTime; never use `DateTime.Now` |

### 15.2 Code Quality

| **Metric** | **Target** |
|---|---|
| Test Coverage | 80%+ overall |
| Cyclomatic Complexity | < 10 per method |
| Method Length | < 30 lines preferred |
| Class Length | < 300 lines preferred |
| Static Analysis | Zero critical issues |

---

## 16. Decision Log

### 16.1 Architecture Decisions

| **Decision** | **Rationale** | **Alternatives Considered** |
|---|---|---|
| .NET MAUI for mobile | Cross-platform with shared codebase; .NET ecosystem | React Native, Flutter |
| Dapper for data access | Lightweight, fast, SQL control | Entity Framework Core |
| SQL Server for database | Familiarity, SSDT tooling, portability path | PostgreSQL |
| NodaTime for timezones | Robust DST handling, IANA database | System.TimeZoneInfo |
| Firebase for push | Industry standard, cross-platform | Azure Notification Hubs, OneSignal |
| JWT for auth | Stateless, scalable | Session-based auth |

---

## Appendix A: Glossary

| **Term** | **Definition** |
|---|---|
| **IANA** | Internet Assigned Numbers Authority (timezone database) |
| **JWT** | JSON Web Token |
| **MAUI** | .NET Multi-platform App UI |
| **SSDT** | SQL Server Data Tools |
| **FCM** | Firebase Cloud Messaging |
| **CDN** | Content Delivery Network |
| **TDE** | Transparent Data Encryption |
| **HSTS** | HTTP Strict Transport Security |
| **CSP** | Content Security Policy |

---

*This document is a living artifact and will be updated as the architecture evolves.*
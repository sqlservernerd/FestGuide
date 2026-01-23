# ADR 0001: Architecture Overview

---

## Status

**Accepted** - 2026-01-20

---

## Context

FestConnect is a mobile-first platform designed to deliver an exceptional experience for music festival attendees. The platform must:

- Support **initial launch** with 1 festival (2,000-5,000 attendees)
- Scale to **400,000 concurrent users** within a 2-year horizon
- Function **offline** in environments with poor connectivity
- Comply with **GDPR** and **WCAG 2.1 AA** requirements
- Be **portable** with no vendor lock-in
- Support **self-hosted infrastructure** with future cloud portability

Key stakeholders:
- **Attendees**: Primary users who discover festivals, build personal schedules, and receive notifications
- **Organizers**: Secondary users who publish festival data and manage team permissions

---

## Decision

We will implement a **layered architecture** with the following technology stack:

### Architecture Layers

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
```

### Technology Stack Decisions

| **Layer** | **Technology** | **Rationale** |
|---|---|---|
| **Presentation** | .NET MAUI (Blazor Hybrid) | Single codebase for iOS, Android, Web; native performance; offline-first capabilities |
| **Interface** | ASP.NET Web API (.NET 10) | Mature, performant, excellent tooling; aligns with team expertise |
| **Application** | C# Class Libraries | Clean separation; testable; no framework dependencies |
| **Data Access** | Dapper + Repository Pattern | Lightweight ORM; fine-grained SQL control; database-agnostic interfaces |
| **Persistence** | SQL Server 2022 + SSDT | Robust, well-understood; SSDT for schema management; can migrate to PostgreSQL |
| **Timezone** | NodaTime | Industry-standard; IANA timezone database; proper DST handling |
| **Push Notifications** | Firebase Cloud Messaging | Cross-platform; reliable; free tier sufficient for launch |
| **Caching** | Redis (optional initially) | Distributed cache for scale; in-memory cache for initial launch |
| **Source Control** | GitHub | Industry standard; excellent CI/CD integration |
| **CI/CD** | GitHub Actions | Integrated with repository; flexible workflows |

### Key Architectural Patterns

| **Pattern** | **Application** |
|---|---|
| **Repository Pattern** | Abstract data access; enable database portability |
| **CQRS-lite** | Separate read/write models where beneficial (not full event sourcing) |
| **DTOs** | Clear contracts between layers; version-safe API responses |
| **Dependency Injection** | Constructor injection; composition root in API project |
| **Offline-First** | Local storage on mobile; sync engine; conflict resolution (server wins) |

---

## Consequences

### Positive

1. **Portability**: Repository abstraction enables future database migration (e.g., PostgreSQL) without application changes

2. **Scalability**: Stateless API design supports horizontal scaling; caching layers reduce database load

3. **Testability**: Clear layer separation enables comprehensive unit testing; mocking at boundaries

4. **Offline Reliability**: Local storage and sync engine ensure attendees can access schedules during connectivity issues

5. **Developer Productivity**: .NET MAUI enables single codebase for all platforms; familiar C# throughout stack

6. **Security**: Layered architecture enforces security at each boundary; cross-cutting concerns isolated

### Negative

1. **Complexity**: Multiple layers add cognitive overhead compared to simpler architectures

2. **Performance Overhead**: DTO mapping between layers has small performance cost

3. **.NET MAUI Maturity**: Relatively newer framework compared to native development; some edge cases may require platform-specific code

4. **Self-Hosted Operations**: Requires infrastructure management expertise; no managed services initially

### Risks and Mitigations

| **Risk** | **Mitigation** |
|---|---|
| .NET MAUI limitations | Platform-specific handlers available; can drop to native code if needed |
| SQL Server-specific features | Use standard SQL where possible; abstract vendor-specific features |
| Scale bottlenecks | Designed for horizontal scaling; load testing validates capacity |
| Offline sync conflicts | Server-wins conflict resolution; clear user feedback on sync status |

---

## Alternatives Considered

### Alternative 1: Native Mobile Development (Swift/Kotlin)

**Rejected because:**
- Requires separate codebases for iOS and Android
- Increased development time and maintenance burden
- Would require separate web application development
- Team expertise is in .NET

### Alternative 2: React Native / Flutter

**Rejected because:**
- Different technology stack from backend (.NET)
- Additional languages to maintain (JavaScript/Dart)
- .NET MAUI provides native performance with familiar C#

### Alternative 3: Entity Framework Core

**Rejected because:**
- ORM overhead for high-performance scenarios
- Less control over generated SQL
- Dapper provides better query optimization control
- Repository pattern works well with Dapper's simplicity

### Alternative 4: Cloud-Native (Azure/AWS)

**Rejected because:**
- Vendor lock-in concerns
- Self-hosted requirement for initial launch
- Architecture is designed to be cloud-portable for future migration

### Alternative 5: NoSQL Database (MongoDB/CosmosDB)

**Rejected because:**
- Relational data model suits domain well
- Strong consistency requirements for schedule data
- Team expertise in SQL Server
- ACID transactions needed for permissions and notifications

---

## Related Decisions

| **ADR** | **Topic** |
|---|---|
| ADR-0002 | Authentication Strategy (JWT + Refresh Tokens) |
| ADR-0003 | Timezone Handling (UTC storage + NodaTime) |
| ADR-0004 | Offline Sync Strategy (Server Wins) |
| ADR-0005 | Notification Architecture (FCM + In-App) |
| ADR-0006 | API Versioning Strategy (URL-based) |

---

## References

- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [NodaTime Documentation](https://nodatime.org/)
- [OWASP Security Guidelines](https://owasp.org/www-project-web-security-testing-guide/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

---

## Notes

This ADR establishes the foundational architecture. Subsequent ADRs will document specific decisions for authentication, timezone handling, offline sync, and other cross-cutting concerns.

The architecture is designed to support the initial launch (2,000-5,000 users) while providing a clear path to scale (400,000 concurrent users) through:

1. Horizontal scaling of stateless API servers
2. Database read replicas
3. Distributed caching (Redis)
4. CDN for static content

---

*Authored by: Project Team*
*Last updated: 2026-01-20*

# FestConnect Project Structure

This document describes the layered architecture of the FestConnect solution.

## Overview

The solution follows a clean architecture pattern with clear separation of concerns across multiple layers.

## Directory Structure

```
FestConnect/
├── FestConnect.sln              # Solution file at root
├── .gitignore                 # Git ignore rules
├── .editorconfig              # Code style configuration
├── Directory.Build.props      # Common MSBuild properties
├── Directory.Packages.props   # Central package management
├── global.json                # .NET SDK version (10.0)
├── nuget.config              # NuGet configuration
│
├── src/                       # Source code
│   ├── Presentation/         # UI Layer
│   │   └── FestConnect.Presentation.Maui/   # .NET MAUI app
│   │
│   ├── Interface/            # API Layer
│   │   └── FestConnect.Api/                 # ASP.NET Web API
│   │
│   ├── Application/          # Business Logic Layer
│   │   └── FestConnect.Application/         # Application services
│   │
│   ├── DataAccess/          # Data Access Layer
│   │   ├── FestConnect.DataAccess/          # Dapper repositories
│   │   └── FestConnect.DataAccess.Abstractions/  # DB interfaces
│   │
│   ├── Domain/              # Domain Layer
│   │   └── FestConnect.Domain/              # Entities, enums, exceptions
│   │
│   ├── Infrastructure/      # Cross-cutting Concerns
│   │   ├── FestConnect.Infrastructure/      # Caching, logging, Firebase
│   │   └── FestConnect.Security/            # Security utilities
│   │
│   ├── Integrations/        # External Integrations
│   │   └── FestConnect.Integrations/        # Webhooks, widgets, social
│   │
│   └── Database/            # Database
│       └── FestConnect.Database/            # SQL Server SSDT project
│
├── tests/                    # Test projects (flat structure)
│   ├── FestConnect.Api.Tests/
│   ├── FestConnect.Application.Tests/
│   ├── FestConnect.DataAccess.Tests/
│   ├── FestConnect.Integrations.Tests/
│   └── FestConnect.Integration.Tests/
│
└── docs/                     # Documentation
    ├── PROJECT_CHARTER.md
    ├── REQUIREMENTS.md
    └── TECHNICAL_ARCHITECTURE.md
```

## Layer Responsibilities

### Presentation Layer
- **Technology**: .NET MAUI (Blazor Hybrid)
- **Purpose**: UI/UX, offline storage, sync engine, local caching, timezone display
- **Projects**: FestConnect.Presentation.Maui

### Interface Layer
- **Technology**: ASP.NET Web API
- **Purpose**: REST endpoints, JWT validation, CORS, DTOs, validation, rate limiting
- **Projects**: FestConnect.Api

### Application Layer
- **Technology**: .NET Class Library
- **Purpose**: Business logic, orchestration, authorization, timezone conversion
- **Projects**: FestConnect.Application

### Data Access Layer
- **Technology**: Dapper + Repository Pattern
- **Purpose**: Database operations, caching, data abstraction
- **Projects**: FestConnect.DataAccess, FestConnect.DataAccess.Abstractions

### Domain Layer
- **Technology**: .NET Class Library
- **Purpose**: Core domain entities, enums, exceptions, business rules
- **Projects**: FestConnect.Domain

### Infrastructure Layer
- **Technology**: .NET Class Libraries
- **Purpose**: Cross-cutting concerns (logging, caching, security)
- **Projects**: FestConnect.Infrastructure, FestConnect.Security

### Integrations Layer
- **Technology**: .NET Class Library
- **Purpose**: External integrations, webhooks, widgets, social sharing
- **Projects**: FestConnect.Integrations

### Database Layer
- **Technology**: SQL Server Database Project (SSDT)
- **Purpose**: Database schema, stored procedures, migrations
- **Projects**: FestConnect.Database

## Project Dependencies

```
FestConnect.Presentation.Maui
  └── FestConnect.Application

FestConnect.Api
  ├── FestConnect.Application
  ├── FestConnect.Infrastructure
  └── FestConnect.Security

FestConnect.Application
  ├── FestConnect.Domain
  └── FestConnect.DataAccess.Abstractions

FestConnect.DataAccess
  ├── FestConnect.Domain
  └── FestConnect.DataAccess.Abstractions

FestConnect.Infrastructure
  (no dependencies)

FestConnect.Security
  (no dependencies)

FestConnect.Integrations
  (no dependencies)

FestConnect.Domain
  (no dependencies)
```

## Building the Solution

### Prerequisites
- .NET 10 SDK
- For MAUI: MAUI workloads (`dotnet workload install maui`)
- For Database: SQL Server Data Tools

### Build Commands

```bash
# Restore packages
dotnet restore FestConnect.sln

# Build all projects (except MAUI and Database without workloads)
dotnet build FestConnect.sln

# Build specific project
dotnet build src/Interface/FestConnect.Api/FestConnect.Api.csproj

# Run tests
dotnet test FestConnect.sln
```

## Central Package Management

All package versions are managed centrally in `Directory.Packages.props`. Individual projects reference packages without version numbers.

## Code Style

Code style is enforced through `.editorconfig`. All projects follow:
- C# 10+ features enabled
- Nullable reference types enabled
- Implicit usings enabled
- PascalCase for public members
- camelCase with underscore prefix for private fields
- Interfaces start with 'I'
- Async methods end with 'Async'

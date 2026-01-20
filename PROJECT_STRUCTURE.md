# FestGuide Project Structure

This document describes the layered architecture of the FestGuide solution.

## Overview

The solution follows a clean architecture pattern with clear separation of concerns across multiple layers.

## Directory Structure

```
FestGuide/
├── FestGuide.sln              # Solution file at root
├── .gitignore                 # Git ignore rules
├── .editorconfig              # Code style configuration
├── Directory.Build.props      # Common MSBuild properties
├── Directory.Packages.props   # Central package management
├── global.json                # .NET SDK version (10.0)
├── nuget.config              # NuGet configuration
│
├── src/                       # Source code
│   ├── Presentation/         # UI Layer
│   │   └── FestGuide.Presentation.Maui/   # .NET MAUI app
│   │
│   ├── Interface/            # API Layer
│   │   └── FestGuide.Api/                 # ASP.NET Web API
│   │
│   ├── Application/          # Business Logic Layer
│   │   └── FestGuide.Application/         # Application services
│   │
│   ├── DataAccess/          # Data Access Layer
│   │   ├── FestGuide.DataAccess/          # Dapper repositories
│   │   └── FestGuide.DataAccess.Abstractions/  # DB interfaces
│   │
│   ├── Domain/              # Domain Layer
│   │   └── FestGuide.Domain/              # Entities, enums, exceptions
│   │
│   ├── Infrastructure/      # Cross-cutting Concerns
│   │   ├── FestGuide.Infrastructure/      # Caching, logging, Firebase
│   │   └── FestGuide.Security/            # Security utilities
│   │
│   ├── Integrations/        # External Integrations
│   │   └── FestGuide.Integrations/        # Webhooks, widgets, social
│   │
│   └── Database/            # Database
│       └── FestGuide.Database/            # SQL Server SSDT project
│
├── tests/                    # Test projects (flat structure)
│   ├── FestGuide.Api.Tests/
│   ├── FestGuide.Application.Tests/
│   ├── FestGuide.DataAccess.Tests/
│   ├── FestGuide.Integrations.Tests/
│   └── FestGuide.Integration.Tests/
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
- **Projects**: FestGuide.Presentation.Maui

### Interface Layer
- **Technology**: ASP.NET Web API
- **Purpose**: REST endpoints, JWT validation, CORS, DTOs, validation, rate limiting
- **Projects**: FestGuide.Api

### Application Layer
- **Technology**: .NET Class Library
- **Purpose**: Business logic, orchestration, authorization, timezone conversion
- **Projects**: FestGuide.Application

### Data Access Layer
- **Technology**: Dapper + Repository Pattern
- **Purpose**: Database operations, caching, data abstraction
- **Projects**: FestGuide.DataAccess, FestGuide.DataAccess.Abstractions

### Domain Layer
- **Technology**: .NET Class Library
- **Purpose**: Core domain entities, enums, exceptions, business rules
- **Projects**: FestGuide.Domain

### Infrastructure Layer
- **Technology**: .NET Class Libraries
- **Purpose**: Cross-cutting concerns (logging, caching, security)
- **Projects**: FestGuide.Infrastructure, FestGuide.Security

### Integrations Layer
- **Technology**: .NET Class Library
- **Purpose**: External integrations, webhooks, widgets, social sharing
- **Projects**: FestGuide.Integrations

### Database Layer
- **Technology**: SQL Server Database Project (SSDT)
- **Purpose**: Database schema, stored procedures, migrations
- **Projects**: FestGuide.Database

## Project Dependencies

```
FestGuide.Presentation.Maui
  └── FestGuide.Application

FestGuide.Api
  ├── FestGuide.Application
  ├── FestGuide.Infrastructure
  └── FestGuide.Security

FestGuide.Application
  ├── FestGuide.Domain
  └── FestGuide.DataAccess.Abstractions

FestGuide.DataAccess
  ├── FestGuide.Domain
  └── FestGuide.DataAccess.Abstractions

FestGuide.Infrastructure
  (no dependencies)

FestGuide.Security
  (no dependencies)

FestGuide.Integrations
  (no dependencies)

FestGuide.Domain
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
dotnet restore FestGuide.sln

# Build all projects (except MAUI and Database without workloads)
dotnet build FestGuide.sln

# Build specific project
dotnet build src/Interface/FestGuide.Api/FestGuide.Api.csproj

# Run tests
dotnet test FestGuide.sln
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

# 🎵 FestConnect - Deployment Guide

---

## Document Control

| **Document Title** | FestConnect - Deployment Guide |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Overview

### 1.1 Deployment Strategy

| **Environment** | **Purpose** | **Infrastructure** |
|---|---|---|
| Development | Local development and testing | Developer workstation |
| Staging | Pre-production testing | Self-hosted server |
| Production | Live application | Self-hosted data center |

### 1.2 Deployment Philosophy

| **Principle** | **Description** |
|---|---|
| Infrastructure as Code | All infrastructure defined in version control |
| Immutable Deployments | Containers rebuilt for each deployment |
| Zero-Downtime | Rolling deployments with health checks |
| Rollback Capability | Previous versions can be restored within minutes |
| Environment Parity | Staging mirrors production configuration |

---

## 2. Prerequisites

### 2.1 Infrastructure Requirements

#### Initial Launch (2,000-5,000 users)

| **Component** | **Specification** |
|---|---|
| Application Server | 4 vCPU, 16GB RAM |
| Database Server | 4 vCPU, 32GB RAM, SSD storage |
| Redis Cache | 2GB RAM (optional for initial launch) |
| Load Balancer | 1 instance |

#### Full Scale (400,000 users)

| **Component** | **Specification** |
|---|---|
| Application Servers | 8x (8 vCPU, 32GB RAM) |
| Database Server | 16 vCPU, 128GB RAM, NVMe SSD |
| Read Replicas | 2x database read replicas |
| Redis Cluster | 3-node cluster, 16GB each |
| Load Balancer | Redundant pair |

### 2.2 Software Requirements

| **Software** | **Version** | **Purpose** |
|---|---|---|
| .NET SDK | 10.0 | Application runtime |
| SQL Server | 2022 | Database |
| Docker | 24.0+ | Containerization |
| Docker Compose | 2.20+ | Local orchestration |
| Nginx | 1.25+ | Reverse proxy |
| Redis | 7.0+ | Distributed cache |

### 2.3 External Services

| **Service** | **Purpose** | **Required** |
|---|---|---|
| Firebase Cloud Messaging | Push notifications | Yes |
| SMTP Provider | Email delivery | Yes |
| SSL Certificate Provider | HTTPS | Yes |
| DNS Provider | Domain management | Yes |

---

## 3. Environment Configuration

### 3.1 Configuration Hierarchy

```
appsettings.json                    # Base configuration (checked into source)
├── appsettings.Development.json    # Development overrides
├── appsettings.Staging.json        # Staging overrides  
├── appsettings.Production.json     # Production overrides (not in source)
└── Environment Variables           # Secrets and runtime overrides
```

### 3.2 Required Environment Variables

| **Variable** | **Description** | **Example** |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ConnectionStrings__DefaultConnection` | Database connection string | `Server=...` |
| `Jwt__SecretKey` | JWT signing key (256-bit) | `<base64-key>` |
| `Jwt__Issuer` | JWT issuer | `https://api.FestConnect.com` |
| `Jwt__Audience` | JWT audience | `https://FestConnect.com` |
| `Firebase__ProjectId` | Firebase project ID | `FestConnect-prod` |
| `Firebase__CredentialsPath` | Path to Firebase credentials | `/secrets/firebase.json` |
| `Email__SmtpHost` | SMTP server host | `smtp.example.com` |
| `Email__SmtpPort` | SMTP server port | `587` |
| `Email__SmtpUsername` | SMTP username | `noreply@FestConnect.com` |
| `Email__SmtpPassword` | SMTP password | `<password>` |
| `Redis__ConnectionString` | Redis connection | `localhost:6379` |

### 3.3 Secrets Management

| **Environment** | **Method** |
|---|---|
| Development | User secrets (`dotnet user-secrets`) |
| Staging | Environment variables |
| Production | HashiCorp Vault or encrypted config files |

**Never store secrets in:**
- Source control
- Application logs
- Container images
- Unencrypted configuration files

---

## 4. Build Process

### 4.1 Build Pipeline (GitHub Actions)

```yaml
# .github/workflows/build.yml
name: Build and Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
    
    - name: Publish
      run: dotnet publish src/FestConnect.Api/FestConnect.Api.csproj -c Release -o ./publish
    
    - name: Build Docker image
      run: docker build -t FestConnect-api:${{ github.sha }} .
    
    - name: Push to registry
      run: |
        docker tag FestConnect-api:${{ github.sha }} registry.FestConnect.com/api:${{ github.sha }}
        docker push registry.FestConnect.com/api:${{ github.sha }}
```

### 4.2 Dockerfile

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FestConnect.Api/FestConnect.Api.csproj", "FestConnect.Api/"]
COPY ["src/FestConnect.Application/FestConnect.Application.csproj", "FestConnect.Application/"]
COPY ["src/FestConnect.DataAccess/FestConnect.DataAccess.csproj", "FestConnect.DataAccess/"]
COPY ["src/FestConnect.Domain/FestConnect.Domain.csproj", "FestConnect.Domain/"]
COPY ["src/FestConnect.Infrastructure/FestConnect.Infrastructure.csproj", "FestConnect.Infrastructure/"]
COPY ["src/FestConnect.Security/FestConnect.Security.csproj", "FestConnect.Security/"]
RUN dotnet restore "FestConnect.Api/FestConnect.Api.csproj"
COPY src/ .
RUN dotnet build "FestConnect.Api/FestConnect.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FestConnect.Api/FestConnect.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Run as non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "FestConnect.Api.dll"]
```

### 4.3 Build Commands

```bash
# Restore packages
dotnet restore FestConnect.sln

# Build solution
dotnet build FestConnect.sln --configuration Release

# Run tests
dotnet test FestConnect.sln --configuration Release --no-build

# Publish API
dotnet publish src/FestConnect.Api/FestConnect.Api.csproj \
    --configuration Release \
    --output ./artifacts/api

# Build Docker image
docker build -t FestConnect-api:latest .
```

---

## 5. Database Deployment

### 5.1 SSDT Deployment

Database schema is managed using SQL Server Data Tools (SSDT).

```bash
# Build database project
dotnet build src/FestConnect.Database/FestConnect.Database.sqlproj

# Generate deployment script
sqlpackage /Action:Script \
    /SourceFile:./src/FestConnect.Database/bin/Release/FestConnect.Database.dacpac \
    /TargetConnectionString:"<connection-string>" \
    /OutputPath:./deploy-script.sql

# Deploy to database
sqlpackage /Action:Publish \
    /SourceFile:./src/FestConnect.Database/bin/Release/FestConnect.Database.dacpac \
    /TargetConnectionString:"<connection-string>" \
    /p:BlockOnPossibleDataLoss=true
```

### 5.2 Migration Strategy

| **Step** | **Action** | **Rollback** |
|---|---|---|
| 1 | Backup current database | N/A |
| 2 | Deploy schema changes | Restore from backup |
| 3 | Run data migrations | Reverse migration script |
| 4 | Verify data integrity | Restore from backup |

### 5.3 Pre-Deployment Checklist

- [ ] Full database backup completed
- [ ] Deployment script reviewed
- [ ] Breaking changes identified
- [ ] Data migration scripts tested
- [ ] Rollback procedure documented

---

## 6. Application Deployment

### 6.1 Deployment Process

```
┌─────────────────────────────────────────────────────────────────┐
│                      DEPLOYMENT PIPELINE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   1. Build & Test     2. Package          3. Deploy             │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│   │ dotnet build │───►│ docker build │───►│ Rolling      │      │
│   │ dotnet test  │    │ docker push  │    │ Deployment   │      │
│   └──────────────┘    └──────────────┘    └──────────────┘      │
│                                                  │               │
│                                                  ▼               │
│                                           4. Health Check        │
│                                           ┌──────────────┐      │
│                                           │ Verify       │      │
│                                           │ Endpoints    │      │
│                                           └──────────────┘      │
│                                                  │               │
│                                    ┌─────────────┴─────────────┐ │
│                                    ▼                           ▼ │
│                              ┌──────────┐               ┌──────────┐
│                              │ Success  │               │ Rollback │
│                              └──────────┘               └──────────┘
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Docker Compose (Staging)

```yaml
# docker-compose.staging.yml
version: '3.8'

services:
  api:
    image: registry.FestConnect.com/api:${IMAGE_TAG}
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Jwt__SecretKey=${JWT_SECRET}
    depends_on:
      - db
      - redis
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    restart: unless-stopped

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${DB_PASSWORD}
    volumes:
      - sqldata:/var/opt/mssql
    ports:
      - "1433:1433"
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    restart: unless-stopped

  nginx:
    image: nginx:1.25-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
    depends_on:
      - api
    restart: unless-stopped

volumes:
  sqldata:
  redisdata:
```

### 6.3 Nginx Configuration

```nginx
# nginx.conf
upstream api_servers {
    server api:8080;
    keepalive 32;
}

server {
    listen 80;
    server_name api.FestConnect.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.FestConnect.com;

    ssl_certificate /etc/nginx/certs/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
    ssl_prefer_server_ciphers off;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    location / {
        proxy_pass http://api_servers;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    location /health {
        proxy_pass http://api_servers/health;
        access_log off;
    }
}
```

### 6.4 Deployment Commands

```bash
# Pull latest image
docker pull registry.FestConnect.com/api:${VERSION}

# Deploy with rolling update
docker-compose -f docker-compose.staging.yml up -d --no-deps api

# Verify health
curl -f https://api.FestConnect.com/health

# View logs
docker-compose -f docker-compose.staging.yml logs -f api

# Rollback (if needed)
docker-compose -f docker-compose.staging.yml up -d --no-deps api \
    -e IMAGE_TAG=${PREVIOUS_VERSION}
```

---

## 7. Health Checks

### 7.1 Health Check Endpoints

| **Endpoint** | **Purpose** | **Response** |
|---|---|---|
| `/health` | Overall health | 200 OK / 503 Unhealthy |
| `/health/ready` | Readiness probe | 200 OK / 503 Not Ready |
| `/health/live` | Liveness probe | 200 OK |

### 7.2 Health Check Response

```json
{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "Healthy",
      "responseTime": "12ms"
    },
    "redis": {
      "status": "Healthy",
      "responseTime": "2ms"
    },
    "firebase": {
      "status": "Healthy"
    }
  },
  "version": "1.0.0",
  "uptime": "2d 4h 32m"
}
```

---

## 8. SSL/TLS Configuration

### 8.1 Certificate Requirements

| **Requirement** | **Value** |
|---|---|
| Minimum TLS Version | TLS 1.2 |
| Preferred TLS Version | TLS 1.3 |
| Certificate Type | Domain Validated (DV) or Extended Validation (EV) |
| Key Size | RSA 2048-bit or ECDSA P-256 |

### 8.2 Certificate Management

```bash
# Generate certificate with Let's Encrypt
certbot certonly --webroot \
    -w /var/www/certbot \
    -d api.FestConnect.com \
    --email admin@FestConnect.com \
    --agree-tos

# Auto-renewal (cron)
0 0 * * * certbot renew --quiet --post-hook "nginx -s reload"
```

---

## 9. Monitoring & Logging

### 9.1 Log Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/FestConnect/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### 9.2 Metrics Collection

| **Metric** | **Type** | **Alert Threshold** |
|---|---|---|
| API Response Time (P95) | Histogram | > 2 seconds |
| Error Rate | Counter | > 1% |
| Active Connections | Gauge | > 80% capacity |
| Database Connection Pool | Gauge | > 90% utilized |
| Memory Usage | Gauge | > 85% |
| CPU Usage | Gauge | > 80% sustained |

---

## 10. Backup & Recovery

### 10.1 Backup Schedule

| **Data Type** | **Frequency** | **Retention** |
|---|---|---|
| Full Database | Daily | 30 days |
| Transaction Logs | Every 15 minutes | 7 days |
| Configuration Files | On change | 90 days |
| Application Logs | Daily archive | 90 days |

### 10.2 Backup Commands

```bash
# Full database backup
sqlcmd -S localhost -U sa -P ${DB_PASSWORD} -Q \
    "BACKUP DATABASE FestConnect TO DISK='/backups/FestConnect_$(date +%Y%m%d).bak' WITH COMPRESSION"

# Verify backup
sqlcmd -S localhost -U sa -P ${DB_PASSWORD} -Q \
    "RESTORE VERIFYONLY FROM DISK='/backups/FestConnect_$(date +%Y%m%d).bak'"
```

### 10.3 Recovery Procedure

| **Step** | **Action** | **Time Estimate** |
|---|---|---|
| 1 | Assess damage and determine recovery point | 15 minutes |
| 2 | Notify stakeholders | 5 minutes |
| 3 | Restore database from backup | 30-60 minutes |
| 4 | Apply transaction logs | 15 minutes |
| 5 | Verify data integrity | 15 minutes |
| 6 | Resume application | 5 minutes |
| 7 | Monitor and validate | 30 minutes |

---

## 11. Rollback Procedures

### 11.1 Application Rollback

```bash
# Identify previous version
docker images registry.FestConnect.com/api --format "{{.Tag}}"

# Rollback to previous version
export IMAGE_TAG=<previous-version>
docker-compose -f docker-compose.staging.yml up -d --no-deps api

# Verify rollback
curl -f https://api.FestConnect.com/health
```

### 11.2 Database Rollback

```bash
# Restore from backup
sqlcmd -S localhost -U sa -P ${DB_PASSWORD} -Q \
    "RESTORE DATABASE FestConnect FROM DISK='/backups/FestConnect_YYYYMMDD.bak' WITH REPLACE"
```

---

## 12. Pre-Deployment Checklist

### 12.1 General Checklist

- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] Database migrations tested
- [ ] Environment variables configured
- [ ] Secrets rotated (if needed)
- [ ] Monitoring alerts configured
- [ ] Rollback procedure tested

### 12.2 Production Deployment Checklist

- [ ] Staging deployment verified
- [ ] Load testing completed (for major releases)
- [ ] Database backup completed
- [ ] Stakeholders notified
- [ ] Deployment window confirmed
- [ ] On-call support available
- [ ] Rollback procedure documented

---

## 13. Troubleshooting

### 13.1 Common Issues

| **Issue** | **Symptoms** | **Resolution** |
|---|---|---|
| Database connection failure | 500 errors, health check fails | Check connection string, firewall rules |
| Out of memory | Application crashes, slow response | Increase container memory, check for leaks |
| Certificate expired | HTTPS errors | Renew certificate, restart nginx |
| Redis connection failure | Slow performance, cache misses | Check Redis service, connection string |

### 13.2 Diagnostic Commands

```bash
# View container logs
docker logs FestConnect-api --tail 100 -f

# Check container resources
docker stats FestConnect-api

# Test database connectivity
docker exec FestConnect-api dotnet FestConnect.Api.dll --test-db

# Check nginx configuration
nginx -t

# View nginx error logs
tail -f /var/log/nginx/error.log
```

---

*This document is a living artifact and will be updated as deployment procedures evolve.*

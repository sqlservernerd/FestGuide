# 🎵 FestConnect - Security Guidelines

---

## Document Control

| **Document Title** | FestConnect - Security Guidelines |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Overview

### 1.1 Purpose

This document establishes security requirements, guidelines, and best practices for the FestConnect platform. Security is a critical cross-cutting concern that applies to all layers of the application.

### 1.2 Security Principles

| **Principle** | **Description** |
|---|---|
| Defense in Depth | Multiple layers of security controls |
| Least Privilege | Minimum access required for each operation |
| Secure by Default | Security enabled out of the box |
| Privacy by Design | Data protection built into architecture |
| Fail Securely | Errors don't expose sensitive information |

### 1.3 Compliance Requirements

| **Standard** | **Requirement** | **Status** |
|---|---|---|
| GDPR | EU data protection compliance | Required |
| CCPA | California privacy compliance | Required |
| WCAG 2.1 AA | Accessibility compliance | Required |
| OWASP Top 10 | Web security best practices | Required |

---

## 2. Authentication

### 2.1 Password Requirements

| **Requirement** | **Value** |
|---|---|
| Minimum Length | 12 characters |
| Maximum Length | 128 characters |
| Complexity | Not enforced (length > complexity) |
| Common Password Check | Required |
| Password History | Last 5 passwords |

### 2.2 Password Hashing

| **Algorithm** | **Configuration** |
|---|---|
| Algorithm | Argon2id |
| Memory | 64 MB |
| Iterations | 3 |
| Parallelism | 4 |
| Salt Length | 16 bytes |
| Hash Length | 32 bytes |

```csharp
// Password hashing implementation
public class PasswordHasher : IPasswordHasher
{
    private readonly Argon2id _argon2 = new()
    {
        MemorySize = 65536,  // 64 MB
        Iterations = 3,
        DegreeOfParallelism = 4,
        HashLength = 32,
        SaltLength = 16
    };

    public string Hash(string password)
    {
        return _argon2.Hash(password);
    }

    public bool Verify(string password, string hash)
    {
        return _argon2.Verify(hash, password);
    }
}
```

### 2.3 JWT Token Configuration

| **Token Type** | **Expiry** | **Usage** |
|---|---|---|
| Access Token | 15 minutes | API authentication |
| Refresh Token | 7 days | Obtain new access token |

| **JWT Claim** | **Description** |
|---|---|
| `sub` | User ID (GUID) |
| `email` | User email |
| `role` | User role (attendee/organizer) |
| `exp` | Expiration timestamp |
| `iat` | Issued at timestamp |
| `jti` | Unique token identifier |

```csharp
// JWT configuration
public class JwtSettings
{
    public string SecretKey { get; set; }      // 256-bit minimum
    public string Issuer { get; set; }          // https://api.FestConnect.com
    public string Audience { get; set; }        // https://FestConnect.com
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
```

### 2.4 Token Security

| **Requirement** | **Implementation** |
|---|---|
| Token Storage | Secure cookie (web) / Secure storage (mobile) |
| Token Rotation | Refresh token rotated on each use |
| Token Revocation | Logout invalidates refresh token |
| Token Blacklist | Revoked tokens tracked until expiry |

### 2.5 Account Security

| **Feature** | **Implementation** |
|---|---|
| Email Verification | Required before full access |
| Account Lockout | 5 failed attempts = 15-minute lockout |
| Password Reset | Token-based, 1-hour expiry |
| Session Termination | Logout available on all devices |

---

## 3. Authorization

### 3.1 Role-Based Access Control (RBAC)

| **Role** | **Description** | **Scope** |
|---|---|---|
| Attendee | Festival goer | Personal data only |
| Organizer | Festival manager | Assigned festival(s) |
| Owner | Festival creator | Full festival control |
| Administrator | Delegated admin | Full control (no ownership transfer) |
| Manager | Scoped access | Assigned areas only |
| Viewer | Read-only | Assigned areas only |

### 3.2 Permission Scopes

| **Scope** | **Access** |
|---|---|
| `venues` | Manage venues and stages |
| `schedule` | Manage time slots and engagements |
| `artists` | Manage artists |
| `editions` | Manage editions |
| `integrations` | Manage API keys and webhooks |
| `all` | All scopes |

### 3.3 Authorization Implementation

```csharp
// Permission check service
public interface IAuthorizationService
{
    Task<bool> CanViewFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct);
    Task<bool> CanEditFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct);
    Task<bool> CanDeleteFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct);
    Task<bool> HasScopeAsync(Guid userId, Guid festivalId, string scope, CancellationToken ct);
}

// Controller usage
[HttpPut("{festivalId:guid}")]
[Authorize]
public async Task<IActionResult> UpdateFestival(Guid festivalId, UpdateFestivalRequest request)
{
    var userId = User.GetUserId();
    
    if (!await _authService.CanEditFestivalAsync(userId, festivalId, HttpContext.RequestAborted))
    {
        return Forbid();
    }
    
    await _festivalService.UpdateAsync(festivalId, request, HttpContext.RequestAborted);
    return NoContent();
}
```

### 3.4 Resource-Based Authorization

```csharp
// Ensure users can only access their own data
public async Task<PersonalSchedule> GetPersonalScheduleAsync(Guid userId, Guid editionId, CancellationToken ct)
{
    var schedule = await _repository.GetByUserAndEditionAsync(userId, editionId, ct);
    
    // Verify ownership
    if (schedule != null && schedule.UserId != userId)
    {
        throw new UnauthorizedAccessException("Access denied to this resource");
    }
    
    return schedule;
}
```

---

## 4. Data Protection

### 4.1 Encryption Requirements

| **Data State** | **Encryption** |
|---|---|
| In Transit | TLS 1.2+ (TLS 1.3 preferred) |
| At Rest | AES-256 |
| Passwords | Argon2id hashing |
| API Keys | SHA-256 hashing |
| Tokens | HMAC-SHA256 signing |

### 4.2 TLS Configuration

| **Setting** | **Value** |
|---|---|
| Minimum Version | TLS 1.2 |
| Preferred Version | TLS 1.3 |
| HSTS | Enabled (max-age=31536000) |
| Certificate | RSA 2048+ or ECDSA P-256 |

```nginx
# Nginx TLS configuration
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
ssl_prefer_server_ciphers off;
ssl_session_timeout 1d;
ssl_session_cache shared:SSL:50m;
ssl_stapling on;
ssl_stapling_verify on;
```

### 4.3 Database Encryption

| **Feature** | **Implementation** |
|---|---|
| Transparent Data Encryption (TDE) | Enabled for production |
| Connection Encryption | Always encrypted |
| Sensitive Column Encryption | Considered for PII |

### 4.4 Sensitive Data Handling

| **Data Type** | **Handling** |
|---|---|
| Passwords | Never stored in plain text; Argon2id hash only |
| API Keys | Hashed before storage; shown once on creation |
| Tokens | Hashed in database; transmitted securely |
| PII | Encrypted at rest; minimized in logs |

---

## 5. API Security

### 5.1 Security Headers

| **Header** | **Value** | **Purpose** |
|---|---|---|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Force HTTPS |
| `X-Frame-Options` | `DENY` | Prevent clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevent MIME sniffing |
| `X-XSS-Protection` | `1; mode=block` | XSS protection |
| `Content-Security-Policy` | `default-src 'self'` | Content restrictions |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Referrer control |

```csharp
// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

### 5.2 Rate Limiting

| **Limit Type** | **Threshold** | **Window** |
|---|---|---|
| Per User (authenticated) | 100 requests | 1 minute |
| Per IP (unauthenticated) | 20 requests | 1 minute |
| Per API Key | 1000 requests | 1 minute |
| Login Attempts | 5 attempts | 15 minutes |
| Password Reset | 3 requests | 1 hour |

```csharp
// Rate limiting configuration
services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

### 5.3 CORS Configuration

```csharp
// CORS policy
services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://FestConnect.com",
                "https://www.FestConnect.com",
                "https://app.FestConnect.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

### 5.4 Input Validation

| **Validation** | **Implementation** |
|---|---|
| Request Size | Max 10MB |
| String Length | Enforce maximum lengths |
| File Types | Whitelist allowed types |
| SQL Injection | Parameterized queries only |
| XSS | Input encoding/sanitization |

```csharp
// Input validation with FluentValidation
public class CreateFestivalRequestValidator : AbstractValidator<CreateFestivalRequest>
{
    public CreateFestivalRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[\w\s\-\.]+$")
            .WithMessage("Name contains invalid characters");

        RuleFor(x => x.WebsiteUrl)
            .Must(BeAValidHttpsUrl)
            .When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("URL must be a valid HTTPS URL");
    }

    private static bool BeAValidHttpsUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var result) &&
        result.Scheme == Uri.UriSchemeHttps;
}
```

---

## 6. SQL Security

### 6.1 SQL Injection Prevention

```csharp
// ✓ ALWAYS use parameterized queries
const string sql = """
    SELECT FestivalId, Name, Description
    FROM core.Festival
    WHERE FestivalId = @FestivalId
      AND IsDeleted = 0
    """;

await connection.QueryAsync<Festival>(sql, new { FestivalId = id });

// ✗ NEVER concatenate user input
var sql = $"SELECT * FROM Festival WHERE Name = '{userInput}'"; // VULNERABLE!
```

### 6.2 Database Access Control

| **Principle** | **Implementation** |
|---|---|
| Least Privilege | Application uses limited-permission account |
| No sa Account | Never use sa in application |
| Separate Accounts | Different accounts for read/write if needed |
| Connection Encryption | Required |

### 6.3 Stored Procedure Security

```sql
-- Use schema-qualified names
CREATE PROCEDURE [core].[usp_Festival_GetById]
    @FestivalId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Input validation in procedure
    IF @FestivalId IS NULL
        THROW 50001, 'FestivalId cannot be null', 1;
    
    SELECT FestivalId, Name, Description
    FROM core.Festival
    WHERE FestivalId = @FestivalId
      AND IsDeleted = 0;
END;
```

---

## 7. Secret Management

### 7.1 Secret Types

| **Secret** | **Storage** | **Rotation** |
|---|---|---|
| Database Connection String | Environment variable / Vault | Quarterly |
| JWT Signing Key | Environment variable / Vault | Annually |
| Firebase Credentials | Secure file / Vault | On compromise |
| SMTP Credentials | Environment variable / Vault | Quarterly |
| API Keys | Hashed in database | User-controlled |

### 7.2 Secret Storage by Environment

| **Environment** | **Method** |
|---|---|
| Development | User secrets (`dotnet user-secrets`) |
| Staging | Environment variables |
| Production | HashiCorp Vault / Azure Key Vault |

### 7.3 Secret Handling Rules

```csharp
// ✓ Load secrets from configuration
var jwtSecret = _configuration["Jwt:SecretKey"];

// ✗ NEVER hardcode secrets
var jwtSecret = "my-super-secret-key-123"; // VULNERABLE!

// ✗ NEVER log secrets
_logger.LogInformation("Using secret: {Secret}", jwtSecret); // VULNERABLE!

// ✗ NEVER commit secrets to source control
// Add to .gitignore:
// appsettings.Production.json
// *.pfx
// *.pem
```

### 7.4 .gitignore Security Entries

```gitignore
# Secrets
appsettings.Production.json
appsettings.*.local.json
*.pfx
*.pem
*.key
secrets.json

# User secrets
secrets/

# Firebase credentials
firebase-credentials.json
```

---

## 8. Logging Security

### 8.1 What to Log

| **Log** | **Purpose** |
|---|---|
| Authentication attempts | Security monitoring |
| Authorization failures | Access control auditing |
| Data modifications | Audit trail |
| System errors | Troubleshooting |
| API requests (sanitized) | Performance/debugging |

### 8.2 What NOT to Log

| **Never Log** | **Reason** |
|---|---|
| Passwords | Even hashed, creates risk |
| JWT tokens | Can be replayed |
| API keys | Credential exposure |
| Credit card numbers | PCI compliance |
| Social security numbers | PII exposure |
| Full request bodies with PII | Privacy violation |

### 8.3 Log Sanitization

```csharp
// Sanitize sensitive data in logs
public static class LogSanitizer
{
    public static string SanitizeEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        return $"{parts[0][0]}***@{parts[1]}";
    }

    public static string SanitizeToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length < 10) return "***";
        return $"{token[..4]}...{token[^4..]}";
    }
}

// Usage
_logger.LogInformation(
    "Login attempt for user {Email}",
    LogSanitizer.SanitizeEmail(email));
```

---

## 9. GDPR Compliance

### 9.1 Data Subject Rights

| **Right** | **Implementation** | **Endpoint** |
|---|---|---|
| Right to Access | Export all user data | `GET /api/v1/profile/export` |
| Right to Rectification | Update profile | `PUT /api/v1/profile` |
| Right to Erasure | Delete account and data | `DELETE /api/v1/profile` |
| Right to Portability | Export in JSON format | `GET /api/v1/profile/export` |

### 9.2 Data Minimization

| **Principle** | **Implementation** |
|---|---|
| Collect only necessary data | No optional fields without purpose |
| Retention limits | Archived data purged after 3 months (attendee view) |
| Anonymization | Analytics use anonymized data |

### 9.3 Consent Management

```csharp
// Record consent
public class UserConsent
{
    public Guid UserId { get; set; }
    public string ConsentType { get; set; }      // e.g., "marketing", "analytics"
    public bool IsGranted { get; set; }
    public DateTime ConsentedAtUtc { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}
```

### 9.4 Breach Notification

| **Requirement** | **Implementation** |
|---|---|
| Detection | Security monitoring and alerting |
| Assessment | Documented incident response procedure |
| Notification | 72-hour notification to supervisory authority |
| Communication | User notification if high risk |

---

## 10. Security Testing

### 10.1 Security Testing Types

| **Type** | **Frequency** | **Scope** |
|---|---|---|
| Static Analysis (SAST) | Every build | All code |
| Dependency Scanning | Daily | NuGet packages |
| Dynamic Analysis (DAST) | Weekly | Running application |
| Penetration Testing | Annually | Full application |
| Security Code Review | Per PR | Changed code |

### 10.2 Automated Security Checks

```yaml
# GitHub Actions security scanning
- name: Run security scan
  uses: github/codeql-action/analyze@v3
  with:
    languages: csharp

- name: Dependency scan
  run: dotnet list package --vulnerable --include-transitive
```

### 10.3 OWASP Top 10 Checklist

| **Vulnerability** | **Mitigation** | **Status** |
|---|---|---|
| A01:2021 Broken Access Control | Authorization service, resource checks | ✓ |
| A02:2021 Cryptographic Failures | TLS 1.3, AES-256, Argon2id | ✓ |
| A03:2021 Injection | Parameterized queries, input validation | ✓ |
| A04:2021 Insecure Design | Threat modeling, security review | ✓ |
| A05:2021 Security Misconfiguration | Security headers, CORS, minimal exposure | ✓ |
| A06:2021 Vulnerable Components | Dependency scanning, updates | ✓ |
| A07:2021 Authentication Failures | Strong passwords, lockout, JWT | ✓ |
| A08:2021 Integrity Failures | HTTPS, signed tokens, CI/CD security | ✓ |
| A09:2021 Logging Failures | Structured logging, monitoring | ✓ |
| A10:2021 SSRF | URL validation, allowlists | ✓ |

---

## 11. Incident Response

### 11.1 Incident Severity Levels

| **Level** | **Description** | **Response Time** |
|---|---|---|
| Critical | Active breach, data exposure | Immediate |
| High | Vulnerability discovered, potential breach | 4 hours |
| Medium | Security issue, no immediate threat | 24 hours |
| Low | Minor issue, best practice violation | 1 week |

### 11.2 Incident Response Procedure

1. **Detection** - Identify and confirm the incident
2. **Containment** - Limit the damage
3. **Eradication** - Remove the threat
4. **Recovery** - Restore normal operations
5. **Lessons Learned** - Document and improve

### 11.3 Emergency Contacts

| **Role** | **Responsibility** |
|---|---|
| Project Owner | Final decision authority |
| On-Call Developer | Technical response |
| Legal Contact | GDPR notification (if needed) |

---

## 12. Security Checklist

### 12.1 Development Checklist

- [ ] Input validated on all endpoints
- [ ] Parameterized queries used
- [ ] Authorization checked for all resources
- [ ] Secrets not in code or logs
- [ ] Error messages don't expose details
- [ ] Security headers configured

### 12.2 Deployment Checklist

- [ ] TLS configured correctly
- [ ] Secrets in secure storage
- [ ] Database access restricted
- [ ] Logging configured (no secrets)
- [ ] Rate limiting enabled
- [ ] Security scanning passed

### 12.3 Release Checklist

- [ ] Dependency vulnerabilities reviewed
- [ ] Security testing completed
- [ ] Penetration test passed (major releases)
- [ ] GDPR compliance verified
- [ ] Incident response plan updated

---

*This document is a living artifact and will be updated as security requirements evolve.*

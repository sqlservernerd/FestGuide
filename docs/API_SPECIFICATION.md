# 🎵 FestConnect - API Specification

---

## Document Control

| **Document Title** | FestConnect - API Specification |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Introduction

### 1.1 Purpose

This document provides the complete API specification for FestConnect, defining all endpoints, request/response formats, authentication mechanisms, and error handling patterns.

### 1.2 Base URL

| **Environment** | **Base URL** |
|---|---|
| Development | `https://localhost:5001/api` |
| Staging | `https://staging-api.FestConnect.com/api` |
| Production | `https://api.FestConnect.com/api` |

### 1.3 API Versioning

All endpoints are versioned using URL path versioning:
- **Current Version**: `v1`
- **Format**: `/api/v1/{resource}`

---

## 2. Authentication

### 2.1 JWT Bearer Token Authentication

Used for user authentication (attendees and organizers).

**Header Format:**
```
Authorization: Bearer <access_token>
```

| **Token Type** | **Expiry** | **Usage** |
|---|---|---|
| Access Token | 15 minutes | API authentication |
| Refresh Token | 7 days | Obtain new access token |

### 2.2 API Key Authentication

Used for public API access (integrations, widgets).

**Header Format:**
```
X-API-Key: <api_key>
```

| **Scope** | **Access Level** |
|---|---|
| Festival-scoped | Read-only access to published festival data |

---

## 3. Request/Response Standards

### 3.1 Content Type

All requests and responses use JSON:
```
Content-Type: application/json
Accept: application/json
```

### 3.2 Timestamp Format

All timestamps use ISO 8601 UTC format:
```
2026-01-20T12:00:00Z
```

### 3.3 Success Response Format

```json
{
  "data": { ... },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

**Paginated Response:**
```json
{
  "data": [ ... ],
  "pagination": {
    "cursor": "eyJpZCI6MTAwfQ==",
    "hasMore": true,
    "pageSize": 20
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

### 3.4 Error Response Format

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": [
      { "field": "fieldName", "message": "Field-specific error" }
    ]
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z",
    "correlationId": "abc-123-def-456"
  }
}
```

---

## 4. Error Codes

### 4.1 HTTP Status Codes

| **Status** | **Meaning** | **Usage** |
|---|---|---|
| 200 | OK | Successful GET, PUT, DELETE |
| 201 | Created | Successful POST (resource created) |
| 204 | No Content | Successful DELETE (no body) |
| 400 | Bad Request | Validation error, malformed request |
| 401 | Unauthorized | Missing or invalid authentication |
| 403 | Forbidden | Authenticated but insufficient permissions |
| 404 | Not Found | Resource does not exist |
| 409 | Conflict | Resource conflict (e.g., duplicate) |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unexpected server error |

### 4.2 Application Error Codes

| **Code** | **Description** |
|---|---|
| `VALIDATION_ERROR` | One or more fields failed validation |
| `AUTHENTICATION_REQUIRED` | No valid authentication provided |
| `INVALID_CREDENTIALS` | Username or password incorrect |
| `TOKEN_EXPIRED` | Access token has expired |
| `TOKEN_INVALID` | Token is malformed or invalid |
| `PERMISSION_DENIED` | User lacks required permission |
| `RESOURCE_NOT_FOUND` | Requested resource does not exist |
| `RESOURCE_CONFLICT` | Resource already exists or conflict |
| `RATE_LIMIT_EXCEEDED` | Too many requests |
| `INTERNAL_ERROR` | Unexpected server error |

---

## 5. Rate Limiting

### 5.1 Rate Limits

| **Limit Type** | **Threshold** | **Window** |
|---|---|---|
| Per User (authenticated) | 100 requests | 1 minute |
| Per IP (unauthenticated) | 20 requests | 1 minute |
| Per API Key | 1000 requests | 1 minute |

### 5.2 Rate Limit Headers

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1706184000
```

---

## 6. Authentication Endpoints

### 6.1 Register User

**POST** `/api/v1/auth/register`

Creates a new user account.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "displayName": "John Doe",
  "userType": "attendee"
}
```

| **Field** | **Type** | **Required** | **Description** |
|---|---|---|---|
| email | string | Yes | Valid email address |
| password | string | Yes | Minimum 12 characters |
| displayName | string | Yes | User's display name |
| userType | string | Yes | `attendee` or `organizer` |

**Response:** `201 Created`
```json
{
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "displayName": "John Doe",
    "userType": "attendee",
    "emailVerified": false
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 6.2 Login

**POST** `/api/v1/auth/login`

Authenticates user and returns tokens.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:** `200 OK`
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresIn": 900,
    "tokenType": "Bearer"
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 6.3 Refresh Token

**POST** `/api/v1/auth/refresh`

Obtains new access token using refresh token.

**Request Body:**
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response:** `200 OK`
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
    "expiresIn": 900,
    "tokenType": "Bearer"
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 6.4 Logout

**POST** `/api/v1/auth/logout`

Invalidates refresh token.

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

### 6.5 Password Reset Request

**POST** `/api/v1/auth/password-reset`

Initiates password reset flow.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response:** `202 Accepted`
```json
{
  "data": {
    "message": "If the email exists, a reset link has been sent"
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

## 7. Profile Endpoints

### 7.1 Get Profile

**GET** `/api/v1/profile`

Returns current user's profile.

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`
```json
{
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "displayName": "John Doe",
    "userType": "attendee",
    "preferredTimezoneId": "America/New_York",
    "emailVerified": true,
    "createdAtUtc": "2026-01-01T00:00:00Z"
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 7.2 Update Profile

**PUT** `/api/v1/profile`

Updates current user's profile.

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "displayName": "John Smith",
  "preferredTimezoneId": "Europe/London"
}
```

**Response:** `200 OK`

---

### 7.3 Delete Account

**DELETE** `/api/v1/profile`

Permanently deletes user account (GDPR erasure).

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

### 7.4 Export User Data

**GET** `/api/v1/profile/export`

Exports all user data (GDPR portability).

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`
```json
{
  "data": {
    "user": { ... },
    "personalSchedules": [ ... ],
    "notificationPreferences": { ... }
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z",
    "exportFormat": "json"
  }
}
```

---

## 8. Attendee Endpoints

### 8.1 Search Festivals

**GET** `/api/v1/festivals`

Search and list festivals.

**Query Parameters:**

| **Parameter** | **Type** | **Description** |
|---|---|---|
| q | string | Search query (name) |
| startDate | date | Filter by start date |
| endDate | date | Filter by end date |
| location | string | Filter by location |
| cursor | string | Pagination cursor |
| pageSize | int | Results per page (max 50) |

**Response:** `200 OK`
```json
{
  "data": [
    {
      "festivalId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Summer Music Festival",
      "description": "Annual summer music festival...",
      "imageUrl": "https://cdn.FestConnect.com/festivals/smf.jpg",
      "nextEditionDate": "2026-07-15"
    }
  ],
  "pagination": {
    "cursor": "eyJpZCI6MTAwfQ==",
    "hasMore": true,
    "pageSize": 20
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 8.2 Get Festival Details

**GET** `/api/v1/festivals/{festivalId}`

Returns festival details.

**Response:** `200 OK`
```json
{
  "data": {
    "festivalId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Summer Music Festival",
    "description": "Annual summer music festival featuring top artists...",
    "imageUrl": "https://cdn.FestConnect.com/festivals/smf.jpg",
    "websiteUrl": "https://summermusicfest.com",
    "editions": [
      {
        "editionId": "660e8400-e29b-41d4-a716-446655440001",
        "name": "Summer Music Festival 2026",
        "startDateUtc": "2026-07-15T00:00:00Z",
        "endDateUtc": "2026-07-17T23:59:59Z",
        "timezoneId": "America/Los_Angeles",
        "status": "published"
      }
    ]
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 8.3 Get Edition Schedule

**GET** `/api/v1/editions/{editionId}/schedule`

Returns published schedule for an edition.

**Response:** `200 OK`
```json
{
  "data": {
    "editionId": "660e8400-e29b-41d4-a716-446655440001",
    "timezoneId": "America/Los_Angeles",
    "lastPublishedUtc": "2026-07-01T12:00:00Z",
    "venues": [
      {
        "venueId": "770e8400-e29b-41d4-a716-446655440002",
        "name": "Main Grounds",
        "stages": [
          {
            "stageId": "880e8400-e29b-41d4-a716-446655440003",
            "name": "Main Stage",
            "engagements": [
              {
                "engagementId": "990e8400-e29b-41d4-a716-446655440004",
                "artist": {
                  "artistId": "aa0e8400-e29b-41d4-a716-446655440005",
                  "name": "The Headliners",
                  "genre": "Rock",
                  "imageUrl": "https://cdn.FestConnect.com/artists/headliners.jpg"
                },
                "startTimeUtc": "2026-07-15T20:00:00Z",
                "endTimeUtc": "2026-07-15T22:00:00Z"
              }
            ]
          }
        ]
      }
    ]
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 8.4 Personal Schedule Management

#### Create Personal Schedule

**POST** `/api/v1/editions/{editionId}/personal-schedule`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `201 Created`
```json
{
  "data": {
    "personalScheduleId": "bb0e8400-e29b-41d4-a716-446655440006",
    "editionId": "660e8400-e29b-41d4-a716-446655440001",
    "entries": []
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

#### Get Personal Schedule

**GET** `/api/v1/editions/{editionId}/personal-schedule`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`
```json
{
  "data": {
    "personalScheduleId": "bb0e8400-e29b-41d4-a716-446655440006",
    "editionId": "660e8400-e29b-41d4-a716-446655440001",
    "entries": [
      {
        "entryId": "cc0e8400-e29b-41d4-a716-446655440007",
        "engagementId": "990e8400-e29b-41d4-a716-446655440004",
        "artistName": "The Headliners",
        "stageName": "Main Stage",
        "startTimeUtc": "2026-07-15T20:00:00Z",
        "endTimeUtc": "2026-07-15T22:00:00Z"
      }
    ],
    "conflicts": []
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

#### Add Entry to Personal Schedule

**POST** `/api/v1/editions/{editionId}/personal-schedule/entries`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "engagementId": "990e8400-e29b-41d4-a716-446655440004"
}
```

**Response:** `201 Created`

#### Remove Entry from Personal Schedule

**DELETE** `/api/v1/editions/{editionId}/personal-schedule/entries/{entryId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

### 8.5 Notification Management

#### Register Device Token

**POST** `/api/v1/profile/device-token`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "token": "fcm_device_token_here",
  "platform": "ios"
}
```

| **Field** | **Type** | **Values** |
|---|---|---|
| token | string | FCM device token |
| platform | string | `ios`, `android`, `web` |

**Response:** `201 Created`

#### Update Notification Preferences

**PUT** `/api/v1/profile/notifications`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "scheduleChanges": true,
  "remindersBefore": 30
}
```

**Response:** `200 OK`

---

## 9. Organizer Endpoints

### 9.1 Festival Management

#### Create Festival

**POST** `/api/v1/organizer/festivals`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "name": "New Music Festival",
  "description": "An amazing new festival...",
  "websiteUrl": "https://newmusicfest.com"
}
```

**Response:** `201 Created`
```json
{
  "data": {
    "festivalId": "dd0e8400-e29b-41d4-a716-446655440008",
    "name": "New Music Festival",
    "description": "An amazing new festival...",
    "websiteUrl": "https://newmusicfest.com",
    "ownerUserId": "550e8400-e29b-41d4-a716-446655440000"
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

#### Update Festival

**PUT** `/api/v1/organizer/festivals/{festivalId}`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "name": "Updated Festival Name",
  "description": "Updated description...",
  "websiteUrl": "https://updatedfest.com"
}
```

**Response:** `200 OK`

#### Delete Festival

**DELETE** `/api/v1/organizer/festivals/{festivalId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

### 9.2 Edition Management

#### Create Edition

**POST** `/api/v1/organizer/festivals/{festivalId}/editions`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "name": "Summer Festival 2026",
  "startDateUtc": "2026-07-15T00:00:00Z",
  "endDateUtc": "2026-07-17T23:59:59Z",
  "timezoneId": "America/Los_Angeles",
  "ticketUrl": "https://tickets.example.com/summer2026"
}
```

**Response:** `201 Created`

#### Update Edition

**PUT** `/api/v1/organizer/editions/{editionId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`

#### Delete Edition

**DELETE** `/api/v1/organizer/editions/{editionId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

### 9.3 Venue & Stage Management

#### Create Venue

**POST** `/api/v1/organizer/venues`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "festivalId": "dd0e8400-e29b-41d4-a716-446655440008",
  "name": "Main Grounds",
  "description": "The primary festival grounds"
}
```

**Response:** `201 Created`

#### Create Stage

**POST** `/api/v1/organizer/venues/{venueId}/stages`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "name": "Main Stage",
  "description": "The largest stage at the festival"
}
```

**Response:** `201 Created`

---

### 9.4 Artist Management

#### Create Artist

**POST** `/api/v1/organizer/festivals/{festivalId}/artists`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "name": "The Headliners",
  "genre": "Rock",
  "bio": "Award-winning rock band...",
  "imageUrl": "https://example.com/artist.jpg",
  "websiteUrl": "https://theheadliners.com",
  "spotifyUrl": "https://spotify.com/artist/xxx"
}
```

**Response:** `201 Created`

#### Update Artist

**PUT** `/api/v1/organizer/artists/{artistId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`

#### Delete Artist

**DELETE** `/api/v1/organizer/artists/{artistId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

### 9.5 Schedule Management

#### Create Time Slot

**POST** `/api/v1/organizer/stages/{stageId}/timeslots`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "editionId": "660e8400-e29b-41d4-a716-446655440001",
  "startTimeUtc": "2026-07-15T20:00:00Z",
  "endTimeUtc": "2026-07-15T22:00:00Z",
  "slotType": "performance"
}
```

| **slotType** | **Description** |
|---|---|
| performance | Artist performance |
| changeover | Stage changeover/break |

**Response:** `201 Created`

#### Create Engagement

**POST** `/api/v1/organizer/timeslots/{timeslotId}/engagement`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "artistId": "aa0e8400-e29b-41d4-a716-446655440005"
}
```

**Response:** `201 Created`

#### Publish Schedule

**POST** `/api/v1/organizer/editions/{editionId}/schedule/publish`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`
```json
{
  "data": {
    "editionId": "660e8400-e29b-41d4-a716-446655440001",
    "publishedAtUtc": "2026-07-01T12:00:00Z",
    "notificationsTriggered": 150
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

---

### 9.6 Permissions Management

#### List Permissions

**GET** `/api/v1/organizer/festivals/{festivalId}/permissions`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `200 OK`
```json
{
  "data": [
    {
      "permissionId": "ee0e8400-e29b-41d4-a716-446655440009",
      "userId": "ff0e8400-e29b-41d4-a716-446655440010",
      "userEmail": "team@example.com",
      "role": "manager",
      "scopes": ["venues", "schedule"]
    }
  ],
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z"
  }
}
```

#### Invite User

**POST** `/api/v1/organizer/festivals/{festivalId}/permissions/invite`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "email": "newuser@example.com",
  "role": "manager",
  "scopes": ["venues", "schedule"]
}
```

| **Role** | **Description** |
|---|---|
| administrator | Full control except ownership transfer |
| manager | Scoped control over specific areas |
| viewer | Read-only scoped access |

| **Scope** | **Description** |
|---|---|
| venues | Manage venues and stages |
| schedule | Manage time slots and engagements |
| artists | Manage artists |
| editions | Manage editions |
| integrations | Manage API keys and webhooks |
| all | All scopes |

**Response:** `201 Created`

#### Revoke Permission

**DELETE** `/api/v1/organizer/festivals/{festivalId}/permissions/{permissionId}`

**Headers:** `Authorization: Bearer <access_token>`

**Response:** `204 No Content`

---

## 10. Integration Endpoints

### 10.1 API Key Management

#### Generate API Key

**POST** `/api/v1/organizer/festivals/{festivalId}/api-keys`

**Headers:** `Authorization: Bearer <access_token>`

**Request Body:**
```json
{
  "name": "Website Integration",
  "expiresAtUtc": "2027-01-01T00:00:00Z"
}
```

**Response:** `201 Created`
```json
{
  "data": {
    "apiKeyId": "gg0e8400-e29b-41d4-a716-446655440011",
    "name": "Website Integration",
    "key": "fg_live_xxxxxxxxxxxxxxxxxxxx",
    "expiresAtUtc": "2027-01-01T00:00:00Z"
  },
  "meta": {
    "timestamp": "2026-01-20T12:00:00Z",
    "warning": "Store this key securely. It will not be shown again."
  }
}
```

### 10.2 Public API (API Key Auth)

#### Get Published Schedule

**GET** `/api/v1/public/editions/{editionId}/schedule`

**Headers:** `X-API-Key: <api_key>`

**Response:** `200 OK` (same format as attendee schedule endpoint)

---

## 11. Webhook Events (Future)

| **Event** | **Description** |
|---|---|
| `schedule.published` | Schedule has been published |
| `schedule.updated` | Published schedule was modified |
| `edition.created` | New edition created |
| `edition.updated` | Edition details updated |

---

## 12. SDK Support

| **Platform** | **Status** |
|---|---|
| .NET | Included (MAUI client) |
| JavaScript/TypeScript | Future |
| Python | Future |

---

*This document is a living artifact and will be updated as the API evolves.*

# 🎵 FestConnect - Requirements Document

---

## Document Control

| **Document Title** | FestConnect - Requirements Document |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Introduction

### 1.1 Purpose

This document defines the functional and non-functional requirements for FestConnect, a mobile-first platform designed to deliver an exceptional experience for music festival attendees. It serves as the authoritative source for what the system must do and how it must perform.

### 1.2 Scope

FestConnect enables:
- **Attendees** to discover festivals, build personalized schedules, and receive real-time notifications
- **Organizers** to publish festival data and integrate with their websites and social media

This document covers requirements for the initial launch (1 festival, 2,000-5,000 attendees) through full scale (400,000 concurrent users).

### 1.3 Audience

- Development team
- QA/Testing
- UX/UI designers
- Project stakeholders

---

## 2. User Types & Personas

### 2.1 Primary User: Attendee

| **Attribute** | **Description** |
|---|---|
| **Role** | Festival goer who wants to plan and navigate their festival experience |
| **Goals** | Discover festivals, build personal schedule, receive schedule change alerts, access info offline |
| **Pain Points** | Schedule complexity, last-minute changes, poor connectivity, fragmented information |
| **Technical Proficiency** | Varies; app must be intuitive for all skill levels |
| **Devices** | Smartphone (iOS/Android), occasionally web |

### 2.2 Secondary User: Organizer

| **Attribute** | **Description** |
|---|---|
| **Role** | Festival organizer or team member who publishes and manages festival data |
| **Goals** | Publish accurate schedules, delegate data entry, integrate with website/social media |
| **Pain Points** | Manual schedule distribution, coordinating team access, keeping data in sync |
| **Technical Proficiency** | Moderate; comfortable with web applications |
| **Devices** | Desktop/laptop for data entry; mobile for review |

### 2.3 Organizer Role Hierarchy

| **Role** | **Description** | **Capabilities** |
|---|---|---|
| **Owner** | Festival creator; auto-assigned on creation | Full control; can transfer ownership; manages all roles |
| **Administrator** | Invited by Owner/Admin | Full control except ownership transfer |
| **Manager** | Invited by Owner/Admin/Manager | Scoped control over specific areas |
| **Viewer** | Invited by any higher role | Read-only access to assigned scope |

---

## 3. Functional Requirements

### 3.1 Authentication & User Management

#### 3.1.1 Registration

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| AUTH-001 | Users can register with email and password | Must Have | ✓ |
| AUTH-002 | Email verification required before full access | Must Have | ✓ |
| AUTH-003 | Users select role type during registration (Attendee or Organizer) | Must Have | ✓ |
| AUTH-004 | Password must meet security requirements (12+ characters) | Must Have | ✓ |
| AUTH-005 | Social login (Google, Apple, Facebook) | Future | - |

#### 3.1.2 Authentication

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| AUTH-010 | Users can login with email and password | Must Have | ✓ |
| AUTH-011 | JWT-based authentication with short-lived access tokens (15 min) | Must Have | ✓ |
| AUTH-012 | Refresh tokens with 7-day expiry and rotation | Must Have | ✓ |
| AUTH-013 | Users can logout (invalidates refresh token) | Must Have | ✓ |
| AUTH-014 | Password reset via email | Must Have | ✓ |

#### 3.1.3 Profile Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| AUTH-020 | Users can view their profile | Must Have | ✓ |
| AUTH-021 | Users can update profile information | Must Have | ✓ |
| AUTH-022 | Users can set preferred timezone for display | Should Have | ✓ |
| AUTH-023 | Users can delete their account (GDPR erasure) | Must Have | ✓ |
| AUTH-024 | Users can export their data (GDPR portability) | Must Have | ✓ |

### 3.2 Attendee Features

#### 3.2.1 Festival Discovery

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ATT-001 | Attendees can search festivals by name | Must Have | ✓ |
| ATT-002 | Attendees can browse festivals with filtering (date, location) | Should Have | Partial |
| ATT-003 | Attendees can view festival details (description, dates, location) | Must Have | ✓ |
| ATT-004 | Attendees can view current and archived editions (up to 3 months back) | Must Have | ✓ |
| ATT-005 | Attendees can view published schedule for an edition | Must Have | ✓ |

#### 3.2.2 Personal Schedule

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ATT-010 | Attendees can create a personal schedule for a festival edition | Must Have | ✓ |
| ATT-011 | Attendees can add performances (engagements) to personal schedule | Must Have | ✓ |
| ATT-012 | Attendees can remove performances from personal schedule | Must Have | ✓ |
| ATT-013 | Attendees can view their personal schedule | Must Have | ✓ |
| ATT-014 | Personal schedule highlights time conflicts | Should Have | ✓ |
| ATT-015 | Personal schedule displays times in festival timezone or user preference | Must Have | ✓ |

#### 3.2.3 Offline Access

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ATT-020 | Attendees can view personal schedule offline | Must Have | ✓ |
| ATT-021 | Attendees can view cached festival/edition data offline | Must Have | ✓ |
| ATT-022 | App syncs data when connectivity returns | Must Have | ✓ |
| ATT-023 | App displays clear sync status indicator | Must Have | ✓ |
| ATT-024 | Offline changes queued and synced when online | Must Have | ✓ |

#### 3.2.4 Notifications

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ATT-030 | Attendees can register device for push notifications | Must Have | ✓ |
| ATT-031 | Attendees receive notification when artist in schedule changes time/stage | Must Have | ✓ |
| ATT-032 | Attendees can configure notification preferences | Should Have | ✓ |
| ATT-033 | Notifications delivered within 5 minutes (95th percentile) | Must Have | ✓ |

#### 3.2.5 External Links

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ATT-040 | Attendees can access ticket purchase link (if configured by organizer) | Should Have | ✓ |

### 3.3 Organizer Features

#### 3.3.1 Festival Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ORG-001 | Organizers can create a festival | Must Have | ✓ |
| ORG-002 | Creator automatically becomes Owner | Must Have | ✓ |
| ORG-003 | Organizers can update festival details | Must Have | ✓ |
| ORG-004 | Owners can delete a festival | Must Have | ✓ |
| ORG-005 | Owners can transfer ownership to another organizer | Should Have | ✓ |

#### 3.3.2 Festival Edition Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ORG-010 | Organizers can create festival editions | Must Have | ✓ |
| ORG-011 | Organizers can set edition dates and timezone | Must Have | ✓ |
| ORG-012 | Organizers can update edition details | Must Have | ✓ |
| ORG-013 | Organizers can delete editions | Must Have | ✓ |
| ORG-014 | Organizers can configure external ticket purchase URL | Should Have | ✓ |

#### 3.3.3 Venue & Stage Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ORG-020 | Organizers can create venues | Must Have | ✓ |
| ORG-021 | Organizers can update venues | Must Have | ✓ |
| ORG-022 | Organizers can delete venues | Must Have | ✓ |
| ORG-023 | Organizers can associate venues with editions | Must Have | ✓ |
| ORG-024 | Organizers can create stages within venues | Must Have | ✓ |
| ORG-025 | Organizers can update stages | Must Have | ✓ |
| ORG-026 | Organizers can delete stages | Must Have | ✓ |

#### 3.3.4 Artist Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ORG-030 | Organizers can create artists (scoped to festival) | Must Have | ✓ |
| ORG-031 | Organizers can update artist details | Must Have | ✓ |
| ORG-032 | Organizers can delete artists | Must Have | ✓ |
| ORG-033 | Artists are reusable across editions within a festival | Must Have | ✓ |

#### 3.3.5 Schedule Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ORG-040 | Organizers can create time slots on stages | Must Have | ✓ |
| ORG-041 | Organizers can update time slots | Must Have | ✓ |
| ORG-042 | Organizers can delete time slots | Must Have | ✓ |
| ORG-043 | Organizers can assign artists to time slots (create engagements) | Must Have | ✓ |
| ORG-044 | Organizers can update engagements | Must Have | ✓ |
| ORG-045 | Organizers can remove engagements | Must Have | ✓ |
| ORG-046 | Schedule has draft and published states | Must Have | ✓ |
| ORG-047 | Organizers can publish schedule (makes visible to attendees) | Must Have | ✓ |
| ORG-048 | Publishing triggers notifications to affected attendees | Must Have | ✓ |

#### 3.3.6 Permissions Management

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| ORG-050 | Organizers can view all permissions for their festival | Must Have | ✓ |
| ORG-051 | Organizers can invite users via email with role and scope | Must Have | ✓ |
| ORG-052 | Organizers can update permissions (change role/scope) | Must Have | ✓ |
| ORG-053 | Organizers can revoke permissions | Must Have | ✓ |
| ORG-054 | Permission scopes: venues, schedule, artists, editions, integrations, all | Must Have | ✓ |

### 3.4 Integration Features

#### 3.4.1 Public API

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| INT-001 | Organizers can generate API keys | Must Have | ✓ |
| INT-002 | API keys are scoped to specific festivals | Must Have | ✓ |
| INT-003 | Public API provides read-only access to published schedule data | Must Have | ✓ |
| INT-004 | API responses include timezone information | Must Have | ✓ |

#### 3.4.2 Embeddable Widgets

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| INT-010 | Organizers can embed lineup widget on websites | Should Have | Basic |
| INT-011 | Organizers can embed schedule widget on websites | Should Have | Basic |
| INT-012 | Organizers can embed countdown widget on websites | Could Have | - |
| INT-013 | Widgets are WCAG 2.1 AA compliant | Must Have | ✓ |

#### 3.4.3 Social Sharing

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| INT-020 | Festival pages include Open Graph metadata | Should Have | ✓ |
| INT-021 | Festival pages include Twitter Card metadata | Should Have | ✓ |
| INT-022 | Deep links to app from shared content | Should Have | ✓ |

#### 3.4.4 Webhooks (Deferred)

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| INT-030 | Organizers can configure webhook URLs | Could Have | - |
| INT-031 | Webhooks fire when schedules are published | Could Have | - |
| INT-032 | Webhooks include HMAC signature for verification | Could Have | - |

#### 3.4.5 Feeds (Deferred)

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| INT-040 | RSS feed for schedule updates | Could Have | - |
| INT-041 | Atom feed for schedule updates | Could Have | - |

---

## 4. Non-Functional Requirements

### 4.1 Performance

| **ID** | **Requirement** | **Initial Launch** | **Full Scale** |
|---|---|---|---|
| PERF-001 | Concurrent users supported | 2,000 - 5,000 | 400,000 |
| PERF-002 | API response time (P95) | < 2 seconds | < 2 seconds |
| PERF-003 | API response time (P50) | < 500ms | < 500ms |
| PERF-004 | Push notification delivery (P95) | < 5 minutes | < 5 minutes |
| PERF-005 | Widget load time | < 500ms | < 500ms |
| PERF-006 | Offline sync time | < 10 seconds | < 10 seconds |
| PERF-007 | Database query time (P95) | < 200ms | < 200ms |

### 4.2 Availability

| **ID** | **Requirement** | **Target** |
|---|---|---|
| AVAIL-001 | Uptime SLA | 99.9% |
| AVAIL-002 | Disaster Recovery RTO | 4 hours |
| AVAIL-003 | Disaster Recovery RPO | 1 hour |
| AVAIL-004 | Scheduled maintenance windows | Off-peak; 48-hour advance notice |

### 4.3 Security

| **ID** | **Requirement** | **Priority** |
|---|---|---|
| SEC-001 | JWT authentication with short-lived tokens | Must Have |
| SEC-002 | Password hashing using Argon2id | Must Have |
| SEC-003 | Minimum 12-character passwords | Must Have |
| SEC-004 | TLS 1.3 preferred, TLS 1.2 minimum | Must Have |
| SEC-005 | HSTS enabled | Must Have |
| SEC-006 | Data encrypted at rest (AES-256) | Must Have |
| SEC-007 | Parameterized queries (SQL injection prevention) | Must Have |
| SEC-008 | Rate limiting (per-user, per-IP, per-API-key) | Must Have |
| SEC-009 | CORS whitelist | Must Have |
| SEC-010 | No secrets in code or config files | Must Have |
| SEC-011 | Audit logging for authentication and data changes | Must Have |
| SEC-012 | Security headers (CSP, X-Frame-Options, etc.) | Must Have |
| SEC-013 | API keys hashed in storage | Must Have |

### 4.4 Compliance

| **ID** | **Requirement** | **Priority** |
|---|---|---|
| COMP-001 | GDPR: Right to access (data export) | Must Have |
| COMP-002 | GDPR: Right to rectification (profile update) | Must Have |
| COMP-003 | GDPR: Right to erasure (account deletion) | Must Have |
| COMP-004 | GDPR: Right to data portability (JSON export) | Must Have |
| COMP-005 | GDPR: Consent management | Must Have |
| COMP-006 | GDPR: 72-hour breach notification capability | Must Have |
| COMP-007 | WCAG 2.1 AA accessibility compliance | Must Have |

### 4.5 Usability

| **ID** | **Requirement** | **Priority** |
|---|---|---|
| USE-001 | Mobile-first responsive design | Must Have |
| USE-002 | Intuitive navigation (< 3 taps to key actions) | Should Have |
| USE-003 | Screen reader compatibility | Must Have |
| USE-004 | Keyboard navigation support | Must Have |
| USE-005 | Color contrast minimum 4.5:1 | Must Have |
| USE-006 | Touch targets minimum 44x44 CSS pixels | Must Have |
| USE-007 | Support for 200% text scaling | Must Have |
| USE-008 | Respect prefers-reduced-motion | Should Have |

### 4.6 Internationalization

| **ID** | **Requirement** | **Priority** | **Initial Launch** |
|---|---|---|---|
| I18N-001 | All timestamps stored in UTC | Must Have | ✓ |
| I18N-002 | API returns timestamps in ISO 8601 UTC format | Must Have | ✓ |
| I18N-003 | Display times in festival timezone or user preference | Must Have | ✓ |
| I18N-004 | Support for IANA timezone identifiers | Must Have | ✓ |
| I18N-005 | Correct DST handling | Must Have | ✓ |
| I18N-006 | Multi-language UI | Future | - |

---

## 5. Data Requirements

### 5.1 Data Retention

| **Data Type** | **Retention Policy** |
|---|---|
| Attendee personal schedules | Until user deletes or account deleted |
| Archived editions (attendee view) | 3 months |
| Archived editions (organizer view) | Unlimited |
| Audit logs | 2 years |
| User accounts | Until deleted by user |

### 5.2 Data Ownership

| **Data Type** | **Owner** |
|---|---|
| User account data | User |
| Festival/schedule data | Organizer |
| Personal schedules | Attendee |
| Platform analytics | Platform |

---

## 6. Constraints

### 6.1 Technical Constraints

| **Constraint** | **Description** |
|---|---|
| Technology stack | .NET 10, SQL Server 2022, .NET MAUI |
| Database portability | Architecture must support migration to PostgreSQL |
| Infrastructure portability | No cloud vendor lock-in |
| Offline support | Mobile app must function without connectivity |

### 6.2 Business Constraints

| **Constraint** | **Description** |
|---|---|
| Solo development | Single developer for initial phases |
| Budget | Personal project; cost-conscious decisions |
| Timeline | Must align with partner festival schedule |

### 6.3 Regulatory Constraints

| **Constraint** | **Description** |
|---|---|
| GDPR | Must comply for EU users |
| CCPA | Must comply for California users |
| App Store guidelines | Must comply with Apple and Google policies |

---

## 7. Out of Scope

| **Item** | **Rationale** |
|---|---|
| Ticket sales / payment processing | Support external links only |
| Live streaming | Different infrastructure requirements |
| Social features (chat, forums, friends) | Not planned |
| Artist self-service portal | Not planned |
| Festival operations management | Organizers use separate systems |
| Multi-language UI | Future consideration |
| Social login | Future consideration |
| Advanced branding | Future consideration |
| Analytics dashboard | Future consideration |
| Calendar app integration | Future consideration |

---

## 8. Acceptance Criteria

### 8.1 Definition of Done

A requirement is considered complete when:

- [ ] Functionality implemented as specified
- [ ] Unit tests written and passing (coverage targets met)
- [ ] Integration tests passing
- [ ] Accessibility requirements met
- [ ] Documentation updated
- [ ] No known critical or high bugs
- [ ] Code reviewed (when team expands)

### 8.2 Acceptance Testing

| **Phase** | **Testing Approach** |
|---|---|
| Per Sprint | Validate against acceptance criteria |
| Per Phase | End-to-end testing of phase deliverables |
| Pre-Launch | Partner organizer UAT |
| Launch | Real-world validation with attendees |

---

## Appendix A: API Endpoint Summary

### Global Endpoints

| **Method** | **Endpoint** | **Description** |
|---|---|---|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Authenticate and receive JWT |
| POST | `/api/auth/logout` | Invalidate refresh token |
| POST | `/api/auth/refresh` | Refresh access token |
| GET | `/api/profile` | Get current user profile |
| PUT | `/api/profile` | Update current user profile |
| DELETE | `/api/profile` | Delete account |
| GET | `/api/profile/export` | Export user data |

### Attendee Endpoints

| **Method** | **Endpoint** | **Description** |
|---|---|---|
| GET | `/api/festivals` | Search/list festivals |
| GET | `/api/festivals/{id}` | Get festival details |
| GET | `/api/festivals/{id}/editions` | List editions |
| GET | `/api/editions/{id}` | Get edition details |
| GET | `/api/editions/{id}/schedule` | Get published schedule |
| GET | `/api/editions/{id}/ticket-link` | Get ticket purchase URL |
| POST | `/api/editions/{id}/personal-schedule` | Create personal schedule |
| GET | `/api/editions/{id}/personal-schedule` | Get personal schedule |
| PUT | `/api/editions/{id}/personal-schedule` | Update personal schedule |
| POST | `/api/editions/{id}/personal-schedule/entries` | Add to personal schedule |
| DELETE | `/api/editions/{id}/personal-schedule/entries/{entryId}` | Remove from personal schedule |
| PUT | `/api/profile/notifications` | Configure notification preferences |
| POST | `/api/profile/device-token` | Register device for push |

### Organizer Endpoints

| **Method** | **Endpoint** | **Description** |
|---|---|---|
| POST | `/api/organizer/festivals` | Create festival |
| PUT | `/api/organizer/festivals/{id}` | Update festival |
| DELETE | `/api/organizer/festivals/{id}` | Delete festival |
| POST | `/api/organizer/festivals/{id}/editions` | Create edition |
| PUT | `/api/organizer/editions/{id}` | Update edition |
| DELETE | `/api/organizer/editions/{id}` | Delete edition |
| POST | `/api/organizer/venues` | Create venue |
| PUT | `/api/organizer/venues/{id}` | Update venue |
| DELETE | `/api/organizer/venues/{id}` | Delete venue |
| POST | `/api/organizer/venues/{id}/stages` | Create stage |
| PUT | `/api/organizer/stages/{id}` | Update stage |
| DELETE | `/api/organizer/stages/{id}` | Delete stage |
| POST | `/api/organizer/festivals/{id}/artists` | Create artist |
| PUT | `/api/organizer/artists/{id}` | Update artist |
| DELETE | `/api/organizer/artists/{id}` | Delete artist |
| POST | `/api/organizer/stages/{id}/timeslots` | Create time slot |
| PUT | `/api/organizer/timeslots/{id}` | Update time slot |
| DELETE | `/api/organizer/timeslots/{id}` | Delete time slot |
| POST | `/api/organizer/timeslots/{id}/engagement` | Create engagement |
| PUT | `/api/organizer/engagements/{id}` | Update engagement |
| DELETE | `/api/organizer/engagements/{id}` | Delete engagement |
| POST | `/api/organizer/editions/{id}/schedule/publish` | Publish schedule |
| GET | `/api/organizer/festivals/{id}/permissions` | List permissions |
| POST | `/api/organizer/festivals/{id}/permissions/invite` | Invite user |
| PUT | `/api/organizer/permissions/{id}` | Update permission |
| DELETE | `/api/organizer/permissions/{id}` | Revoke permission |

### Integration Endpoints

| **Method** | **Endpoint** | **Description** |
|---|---|---|
| GET | `/api/organizer/festivals/{id}/api-keys` | List API keys |
| POST | `/api/organizer/festivals/{id}/api-keys` | Create API key |
| DELETE | `/api/organizer/api-keys/{id}` | Revoke API key |
| GET | `/api/public/editions/{id}` | Public API: edition details |
| GET | `/api/public/editions/{id}/schedule` | Public API: schedule |
| GET | `/api/embed/editions/{id}/lineup` | Lineup widget data |
| GET | `/api/embed/editions/{id}/schedule` | Schedule widget data |
| GET | `/api/share/editions/{id}` | Share metadata |

---

*This document is a living artifact and will be updated as requirements evolve.*
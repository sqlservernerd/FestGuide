# 🎵 FestConnect - UI/UX Guidelines

---

## Document Control

| **Document Title** | FestConnect - UI/UX Guidelines |
|---|---|
| **Version** | 1.0 |
| **Date** | 2026-01-20 |
| **Status** | Draft |
| **Author** | Project Team |
| **Document Owner** | sqlservernerd |

---

## 1. Overview

### 1.1 Purpose

This document establishes UI/UX guidelines for FestConnect to ensure a consistent, accessible, and user-friendly experience across all platforms (iOS, Android, Web).

### 1.2 Design Principles

| **Principle** | **Description** |
|---|---|
| Mobile-First | Design for mobile, scale up for larger screens |
| Attendee-First | Prioritize the attendee experience |
| Offline-First | Design for limited or no connectivity |
| Accessibility | WCAG 2.1 AA compliance for all users |
| Simplicity | Reduce complexity, increase clarity |
| Consistency | Unified experience across platforms |

### 1.3 Target Platforms

| **Platform** | **Technology** | **Priority** |
|---|---|---|
| iOS | .NET MAUI (Native) | Primary |
| Android | .NET MAUI (Native) | Primary |
| Web | .NET MAUI (Blazor Hybrid) | Secondary |
| Desktop | Responsive Web | Tertiary |

---

## 2. Design System

### 2.1 Color Palette

#### Primary Colors

| **Color** | **Hex** | **Usage** |
|---|---|---|
| Primary | `#6366F1` | Primary actions, links, focus states |
| Primary Dark | `#4F46E5` | Hover states, emphasis |
| Primary Light | `#A5B4FC` | Backgrounds, subtle accents |

#### Secondary Colors

| **Color** | **Hex** | **Usage** |
|---|---|---|
| Secondary | `#EC4899` | Secondary actions, highlights |
| Secondary Dark | `#DB2777` | Hover states |
| Secondary Light | `#F9A8D4` | Backgrounds |

#### Neutral Colors

| **Color** | **Hex** | **Usage** |
|---|---|---|
| Gray 900 | `#111827` | Primary text |
| Gray 700 | `#374151` | Secondary text |
| Gray 500 | `#6B7280` | Placeholder text |
| Gray 300 | `#D1D5DB` | Borders |
| Gray 100 | `#F3F4F6` | Backgrounds |
| White | `#FFFFFF` | Cards, surfaces |

#### Semantic Colors

| **Color** | **Hex** | **Usage** |
|---|---|---|
| Success | `#10B981` | Success states, confirmations |
| Warning | `#F59E0B` | Warnings, schedule conflicts |
| Error | `#EF4444` | Errors, destructive actions |
| Info | `#3B82F6` | Information, tips |

### 2.2 Color Contrast

All color combinations must meet WCAG 2.1 AA contrast requirements:

| **Text Type** | **Minimum Contrast Ratio** |
|---|---|
| Normal Text | 4.5:1 |
| Large Text (18px+ or 14px+ bold) | 3:1 |
| UI Components | 3:1 |

### 2.3 Dark Mode

| **Element** | **Light Mode** | **Dark Mode** |
|---|---|---|
| Background | `#FFFFFF` | `#111827` |
| Surface | `#F3F4F6` | `#1F2937` |
| Primary Text | `#111827` | `#F9FAFB` |
| Secondary Text | `#6B7280` | `#9CA3AF` |
| Border | `#D1D5DB` | `#374151` |

---

## 3. Typography

### 3.1 Font Family

| **Platform** | **Primary Font** | **Fallback** |
|---|---|---|
| iOS | SF Pro | System |
| Android | Roboto | System |
| Web | Inter | System UI |

### 3.2 Type Scale

| **Style** | **Size** | **Weight** | **Line Height** | **Usage** |
|---|---|---|---|---|
| Display | 32px | Bold (700) | 40px | Festival names |
| Heading 1 | 24px | Bold (700) | 32px | Page titles |
| Heading 2 | 20px | Semibold (600) | 28px | Section headers |
| Heading 3 | 18px | Semibold (600) | 24px | Card headers |
| Body | 16px | Regular (400) | 24px | Default text |
| Body Small | 14px | Regular (400) | 20px | Secondary text |
| Caption | 12px | Regular (400) | 16px | Labels, timestamps |

### 3.3 Text Accessibility

| **Requirement** | **Value** |
|---|---|
| Minimum Font Size | 14px (mobile), 16px (desktop) |
| Support Text Scaling | Up to 200% |
| Maximum Line Length | 80 characters |
| Paragraph Spacing | 1.5x line height |

---

## 4. Layout & Spacing

### 4.1 Spacing Scale

| **Token** | **Value** | **Usage** |
|---|---|---|
| `space-1` | 4px | Tight spacing |
| `space-2` | 8px | Component padding |
| `space-3` | 12px | Item gaps |
| `space-4` | 16px | Section spacing |
| `space-5` | 20px | Card padding |
| `space-6` | 24px | Large gaps |
| `space-8` | 32px | Section separation |
| `space-10` | 40px | Page margins |

### 4.2 Grid System

| **Breakpoint** | **Width** | **Columns** | **Margin** | **Gutter** |
|---|---|---|---|---|
| Mobile | < 640px | 4 | 16px | 16px |
| Tablet | 640-1024px | 8 | 24px | 24px |
| Desktop | > 1024px | 12 | 32px | 24px |

### 4.3 Safe Areas

| **Platform** | **Consideration** |
|---|---|
| iOS | Notch, Home Indicator |
| Android | Status bar, Navigation bar |
| Web | Scrollbar, Browser chrome |

---

## 5. Components

### 5.1 Buttons

#### Button Types

| **Type** | **Usage** | **Example** |
|---|---|---|
| Primary | Main actions | "Add to Schedule" |
| Secondary | Alternative actions | "View Details" |
| Tertiary | Low-emphasis actions | "Cancel" |
| Destructive | Dangerous actions | "Delete" |

#### Button States

| **State** | **Visual Change** |
|---|---|
| Default | Normal appearance |
| Hover | Slight darkening |
| Pressed | Further darkening |
| Focused | Focus ring (accessibility) |
| Disabled | 50% opacity |
| Loading | Spinner, disabled |

#### Button Sizes

| **Size** | **Height** | **Padding** | **Font** |
|---|---|---|---|
| Small | 32px | 12px | 14px |
| Medium | 40px | 16px | 16px |
| Large | 48px | 20px | 16px |

### 5.2 Touch Targets

| **Requirement** | **Value** |
|---|---|
| Minimum Size | 44 × 44 CSS pixels |
| Minimum Spacing | 8px between targets |
| Recommended Size | 48 × 48 CSS pixels |

### 5.3 Cards

```
┌────────────────────────────────────┐
│ ┌──────────────────────────────┐   │
│ │         Image Area           │   │
│ │        (16:9 ratio)          │   │
│ └──────────────────────────────┘   │
│                                    │
│  Festival Name                     │  ← Heading 3
│  July 15-17, 2026                  │  ← Caption
│                                    │
│  Brief description text that       │  ← Body Small
│  may span multiple lines...        │
│                                    │
│  ┌──────────────┐  ┌──────────┐   │
│  │ Primary CTA  │  │ Secondary│   │
│  └──────────────┘  └──────────┘   │
│                                    │
└────────────────────────────────────┘
```

**Card Specifications:**

| **Property** | **Value** |
|---|---|
| Border Radius | 12px |
| Shadow | 0 1px 3px rgba(0,0,0,0.1) |
| Padding | 16px |
| Image Aspect Ratio | 16:9 |

### 5.4 Schedule Grid

```
┌────────────────────────────────────────────────────────┐
│  10:00 AM    │ Main Stage    │ Second Stage           │
├──────────────┼───────────────┼────────────────────────┤
│              │ ┌───────────┐ │                        │
│              │ │ Artist A  │ │                        │
│              │ │ 10:00-11:30│ │                        │
│              │ └───────────┘ │                        │
│  11:00 AM    │               │ ┌──────────────────┐   │
│              │               │ │ Artist B         │   │
│              │               │ │ 11:00-12:00      │   │
│              │               │ └──────────────────┘   │
│  12:00 PM    │ ┌───────────┐ │                        │
│              │ │ Artist C  │ │                        │
│              │ │ 12:00-1:30 │ │                        │
│              │ └───────────┘ │                        │
└──────────────┴───────────────┴────────────────────────┘
```

**Schedule Specifications:**

| **Element** | **Value** |
|---|---|
| Time Column Width | 80px |
| Stage Column Min Width | 150px |
| Performance Block Height | Based on duration |
| Conflict Indicator | Orange border |
| Saved Indicator | Primary color fill |

### 5.5 Form Elements

#### Text Input

| **Property** | **Value** |
|---|---|
| Height | 44px minimum |
| Padding | 12px horizontal |
| Border | 1px solid Gray 300 |
| Border Radius | 8px |
| Focus Border | 2px solid Primary |

#### Labels

| **Property** | **Value** |
|---|---|
| Position | Above input |
| Spacing | 4px below |
| Font | Body Small, Gray 700 |

#### Error States

| **Element** | **Style** |
|---|---|
| Border | Error color |
| Message | Error color, below input |
| Icon | Error icon (optional) |

---

## 6. Navigation

### 6.1 Mobile Navigation

**Bottom Tab Bar (Primary Navigation):**

```
┌────────────────────────────────────────────────────┐
│                                                    │
│                  Content Area                      │
│                                                    │
├────────────────────────────────────────────────────┤
│   🏠        📅        🔔        👤                 │
│  Home    Schedule  Alerts    Profile              │
└────────────────────────────────────────────────────┘
```

| **Tab** | **Icon** | **Label** | **Screen** |
|---|---|---|---|
| Home | House | Home | Festival discovery |
| Schedule | Calendar | Schedule | Personal schedule |
| Alerts | Bell | Alerts | Notifications |
| Profile | Person | Profile | User settings |

### 6.2 Navigation Hierarchy

```
Home
├── Festival List
│   └── Festival Detail
│       ├── Edition List
│       │   └── Edition Detail
│       │       ├── Schedule View
│       │       └── Artist List
│       │           └── Artist Detail
│       └── Add to Schedule
└── Search

Schedule
├── My Schedule (by day)
│   └── Engagement Detail
└── Conflicts

Profile
├── Account Settings
├── Notification Preferences
├── Timezone Settings
└── Logout
```

### 6.3 Gestures

| **Gesture** | **Action** |
|---|---|
| Swipe left | Delete from schedule |
| Swipe right | Add to schedule |
| Pull to refresh | Sync data |
| Long press | Context menu |
| Pinch | Zoom schedule grid |

---

## 7. Accessibility (WCAG 2.1 AA)

### 7.1 Perceivable

| **Requirement** | **Implementation** |
|---|---|
| Text Alternatives | Alt text for all images |
| Captions | Video captions when applicable |
| Adaptable | Responsive, logical structure |
| Distinguishable | 4.5:1 contrast, resizable text |

### 7.2 Operable

| **Requirement** | **Implementation** |
|---|---|
| Keyboard Accessible | All functions via keyboard |
| Enough Time | No time limits (or adjustable) |
| Seizures | No flashing content |
| Navigable | Skip links, focus order, headings |

### 7.3 Understandable

| **Requirement** | **Implementation** |
|---|---|
| Readable | Clear language, no jargon |
| Predictable | Consistent navigation |
| Input Assistance | Error identification, suggestions |

### 7.4 Robust

| **Requirement** | **Implementation** |
|---|---|
| Compatible | Valid markup, ARIA labels |
| Screen Reader | VoiceOver (iOS), TalkBack (Android) |
| Assistive Tech | Works with assistive technologies |

### 7.5 Accessibility Checklist

| **Component** | **Requirements** |
|---|---|
| Images | Alt text, decorative marked |
| Buttons | Accessible name, role |
| Forms | Labels, error messages, focus |
| Navigation | Skip links, landmarks |
| Color | Not sole indicator |
| Motion | Respect prefers-reduced-motion |

### 7.6 Screen Reader Considerations

```csharp
// MAUI Semantic Properties
<Button 
    Text="Add to Schedule"
    SemanticProperties.Description="Add The Headliners to your personal schedule for Sunday at 8 PM"
    SemanticProperties.Hint="Double-tap to add" />

// Announcements for dynamic content
SemanticScreenReader.Announce("Artist added to your schedule");
```

---

## 8. Offline Experience

### 8.1 Sync Status Indicator

```
┌────────────────────────────────────┐
│  ☁️ Last synced: 2 minutes ago     │  ← Online, synced
│  ⚡ Syncing...                     │  ← Online, syncing
│  📴 Offline - Using cached data    │  ← Offline
│  ⚠️ Sync failed - Retry            │  ← Error
└────────────────────────────────────┘
```

### 8.2 Offline Indicators

| **State** | **Visual Indicator** | **User Message** |
|---|---|---|
| Online | None | Normal operation |
| Syncing | Progress indicator | "Syncing..." |
| Offline | Banner, subtle styling | "You're offline" |
| Sync Error | Warning banner | "Sync failed. Retry?" |

### 8.3 Offline Capabilities

| **Feature** | **Offline Behavior** |
|---|---|
| View Schedule | ✓ Fully available |
| View Personal Schedule | ✓ Fully available |
| Add to Schedule | ✓ Queued for sync |
| Search | ✓ Local only |
| Notifications | ✗ Unavailable |
| New Registration | ✗ Unavailable |

---

## 9. Loading States

### 9.1 Loading Patterns

| **Pattern** | **Usage** |
|---|---|
| Skeleton | Content areas (cards, lists) |
| Spinner | Actions (button clicks) |
| Progress Bar | File uploads, long operations |
| Shimmer | Image loading |

### 9.2 Skeleton Screens

```
┌────────────────────────────────────┐
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   │  ← Image placeholder
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   │
│                                    │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓                   │  ← Title
│  ▓▓▓▓▓▓▓▓                          │  ← Date
│                                    │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓           │  ← Description
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓                 │
└────────────────────────────────────┘
```

### 9.3 Empty States

| **State** | **Message** | **Action** |
|---|---|---|
| No Festivals | "No festivals found" | "Explore popular festivals" |
| Empty Schedule | "Your schedule is empty" | "Browse the lineup" |
| No Notifications | "No notifications yet" | "Add artists to get notified" |
| Search No Results | "No results for 'query'" | "Try a different search" |

---

## 10. Feedback & Notifications

### 10.1 In-App Feedback

| **Type** | **Duration** | **Position** |
|---|---|---|
| Toast (Success) | 3 seconds | Bottom |
| Toast (Error) | 5 seconds | Bottom |
| Snackbar | Until dismissed | Bottom |
| Alert | Until dismissed | Center |

### 10.2 Toast Messages

```
┌────────────────────────────────────────────────────┐
│                                                    │
│                  Content Area                      │
│                                                    │
│                                                    │
├────────────────────────────────────────────────────┤
│  ✓ Added to your schedule                         │
└────────────────────────────────────────────────────┘
```

### 10.3 Push Notification Format

```
┌────────────────────────────────────────────────────┐
│ 📱 FestConnect                                       │
│                                                    │
│ Schedule Change Alert                              │
│ The Headliners moved to Main Stage, 9 PM          │
│                                                    │
│ [View Schedule]  [Dismiss]                         │
└────────────────────────────────────────────────────┘
```

---

## 11. Timezone Display

### 11.1 Time Display Rules

| **Context** | **Display Format** | **Example** |
|---|---|---|
| Schedule Grid | Festival timezone | "8:00 PM" |
| Detailed View | Festival TZ + User TZ | "8:00 PM PDT (11:00 PM EDT)" |
| Notifications | User timezone | "Starts at 11:00 PM" |
| Settings | IANA identifier | "America/New_York" |

### 11.2 Timezone Indicator

```
┌────────────────────────────────────┐
│  🌐 All times in Pacific Time (PDT) │
│     Your time: Eastern (EDT)       │
└────────────────────────────────────┘
```

---

## 12. Motion & Animation

### 12.1 Animation Principles

| **Principle** | **Implementation** |
|---|---|
| Purposeful | Animations serve a function |
| Natural | Follow physical properties |
| Fast | 200-300ms for most transitions |
| Consistent | Same action = same animation |

### 12.2 Animation Durations

| **Type** | **Duration** | **Easing** |
|---|---|---|
| Micro-interactions | 100-200ms | Ease-out |
| Page Transitions | 200-300ms | Ease-in-out |
| Modals | 200ms | Ease-out |
| Loading indicators | Continuous | Linear |

### 12.3 Reduced Motion

```css
/* Respect user preference */
@media (prefers-reduced-motion: reduce) {
    * {
        animation-duration: 0.01ms !important;
        transition-duration: 0.01ms !important;
    }
}
```

```csharp
// MAUI
if (MediaQuery.Current.PrefersReducedMotion)
{
    // Use instant transitions
}
```

---

## 13. Platform-Specific Guidelines

### 13.1 iOS Guidelines

| **Aspect** | **Guideline** |
|---|---|
| Navigation | Use native navigation patterns |
| Tab Bar | Bottom, 4-5 items max |
| Gestures | Support swipe-to-go-back |
| Typography | SF Pro system font |
| Safe Areas | Respect notch, home indicator |

### 13.2 Android Guidelines

| **Aspect** | **Guideline** |
|---|---|
| Navigation | Material Design patterns |
| Bottom Navigation | 3-5 destinations |
| Gestures | Support gesture navigation |
| Typography | Roboto system font |
| Edge-to-edge | Handle system bars |

### 13.3 Web Guidelines

| **Aspect** | **Guideline** |
|---|---|
| Responsive | Fluid layouts |
| Keyboard | Full keyboard navigation |
| Focus | Visible focus indicators |
| Browser | Support major browsers |

---

## 14. Design Review Checklist

### 14.1 Visual Design

- [ ] Color contrast meets 4.5:1 minimum
- [ ] Touch targets are 44×44px minimum
- [ ] Typography follows scale
- [ ] Spacing is consistent
- [ ] Dark mode works correctly

### 14.2 Interaction Design

- [ ] Loading states defined
- [ ] Empty states designed
- [ ] Error states handled
- [ ] Offline states designed
- [ ] Animations respect reduced motion

### 14.3 Accessibility

- [ ] Screen reader tested
- [ ] Keyboard navigation works
- [ ] Focus order is logical
- [ ] Images have alt text
- [ ] Forms have labels

### 14.4 Consistency

- [ ] Patterns match design system
- [ ] Component usage is consistent
- [ ] Copy follows voice and tone
- [ ] Platform conventions followed

---

*This document is a living artifact and will be updated as design standards evolve.*

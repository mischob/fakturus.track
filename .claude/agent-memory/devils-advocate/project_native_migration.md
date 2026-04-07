---
name: Native App Migration Project
description: Fakturus Track is migrating from MAUI/Blazor Hybrid to native iOS (Swift/SwiftUI) and Android (Kotlin/Compose) apps. Key architectural decisions favor simplicity over abstraction.
type: project
---

Fakturus Track is replacing its MAUI/Blazor Hybrid mobile app with native iOS and Android apps.

**Why:** The MAUI app works but feels unprofessional (WebView-based UI). Native apps provide better UX and match the sister app fakturus.poi (already native in App Stores).

**How to apply:**
- Architecture deliberately avoids Clean Architecture, Hilt, UseCases, Repository pattern -- validated as correct for 4 screens / 8 endpoints / 3 entities
- Backend is ASP.NET Core with FastEndpoints, PostgreSQL, Azure AD B2C auth -- no backend changes needed FOR WORKSESSIONS AND VACATIONDAYS, but PauseMinutes and SickDays require backend updates
- Sync-Engine reuses proven MAUI SyncService algorithm (8-step merge, server-wins)
- Key discrepancy found: Overtime endpoint is `/v1/overtime-summary`, NOT `/v1/settings/overtime` as documented
- VacationDay sync sends ALL local days (not just pending) -- different from WorkSession sync
- SwiftData chosen for iOS persistence but has risk for batch sync operations -- must use @ModelActor for SyncEngine
- Phase 1 review findings (2026-03-29):
  - CRITICAL: PauseMinutes does NOT exist in current MAUI Entity/Model/API -- backend change needed before EPIC 08
  - CRITICAL: SickDays defined in arch docs but NOT in Phase 1 stories -- scope ambiguity
  - Offline delete creates zombie sessions (server re-syncs deleted items) -- needs soft-delete or pending-deletes approach
  - E01-S01 folder structure (ViewModels/Views/) contradicts tech-blueprint (Features/Shared/)
  - iOS 401 retry codesketch missing forceRefresh parameter
  - Android MSAL callback-in-suspend pattern is broken -- needs suspendCancellableCoroutine
  - Manual session creation (past dates) not specified as a story

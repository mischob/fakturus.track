---
name: Native App Migration Project
description: Fakturus Track is migrating from MAUI/Blazor Hybrid to native iOS (Swift/SwiftUI) and Android (Kotlin/Compose) apps. Key architectural decisions favor simplicity over abstraction.
type: project
---

Fakturus Track is replacing its MAUI/Blazor Hybrid mobile app with native iOS and Android apps.

**Why:** The MAUI app works but feels unprofessional (WebView-based UI). Native apps provide better UX and match the sister app fakturus.poi (already native in App Stores).

**How to apply:**
- Architecture deliberately avoids Clean Architecture, Hilt, UseCases, Repository pattern -- validated as correct for 4 screens / 8 endpoints / 3 entities
- Backend is ASP.NET Core with FastEndpoints, PostgreSQL, Azure AD B2C auth -- no backend changes needed
- Sync-Engine reuses proven MAUI SyncService algorithm (8-step merge, server-wins)
- Key discrepancy found: Overtime endpoint is `/v1/overtime-summary`, NOT `/v1/settings/overtime` as documented
- VacationDay sync sends ALL local days (not just pending) -- different from WorkSession sync
- SwiftData chosen for iOS persistence but has risk for batch sync operations
- Marketing integration (2026-03-29): 3 new features added (Pausen, Export, Krankheitstage). Backend docs NOT yet updated for PauseMinutes field or SickDay entity/endpoints.
- Timeline risk: +2 weeks estimated but realistically +4 weeks for 3 features on 2 platforms
- Pausenerfassung marked as STARTER tier but is legally required -- should be FREE
- Previous review.md partially addressed (VacationDay sync fixed, User-Agent added, Settings sync defined) but overtime endpoint and CalendarEventId still unfixed

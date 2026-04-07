---
name: Current Feature Inventory
description: Complete feature inventory of the existing Fakturus Track backend and MAUI mobile app as of March 2026
type: project
---

Existing backend features (all production-ready at api.track.fakturus.com):
- WorkSessions CRUD + bulk sync (/v1/work-sessions)
- VacationDays CRUD + sync (/v1/vacation-days)
- UserSettings with WorkHoursPerWeek, WorkDays bitmask, Bundesland, VacationDaysPerYear
- OvertimeCalculation (monthly/yearly, holiday-aware, school-holiday-aware)
- HolidayService (via Nager.Date, all 16 Bundeslaender)
- SchoolHolidayPeriods CRUD
- Calendar iCal feed import
- Azure B2C JWT auth with user auto-creation

MAUI app status:
- Zeiten tab: Fully functional (Start/Stop/Finish, history, sync)
- Urlaub tab: Placeholder only ("wird in Kuerze implementiert")
- Gesamt tab: Functional (overtime dashboard with monthly table)
- Settings tab: Placeholder only
- Offline-first with SQLite, Refit API clients, 30s sync interval

**Why:** This inventory establishes the baseline for native app development. Phase 1 focuses on matching Zeiten + Sync. Phase 2 completes Gesamt + Urlaub + Settings.

**How to apply:** When prioritizing features, check against this list. Features marked as "Placeholder" in MAUI need full implementation in native apps.

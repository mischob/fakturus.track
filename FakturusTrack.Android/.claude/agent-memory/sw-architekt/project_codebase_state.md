---
name: Codebase State Phase 1
description: Aktueller Zustand der Codebasis nach Phase 1 - Room DB v2, SyncEngine-Muster, bestehende Dateien
type: project
---

Android Room DB ist auf Version 2 (MIGRATION_1_2 fuer pending_deletes). Phase 2 braucht MIGRATION_2_3 fuer SickDay + SchoolHolidayPeriod + UserSettings.updatedAt.

**Why:** Die DB-Versionierung muss korrekt weitergefahren werden. Phase 1 hat bereits eine Migration.

**How to apply:** Naechste Migration ist MIGRATION_2_3 in ServiceContainer.kt. AppDatabase Schema-Version von 2 auf 3 erhoehen. SyncEngine.syncUserSettings() ist aktuell Server-wins und muss auf Last-Write-Wins umgestellt werden.

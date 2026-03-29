# Fakturus Track -- Native Mobile App Strategie

## Executive Summary

Fakturus Track ist eine Zeiterfassungsloesung fuer den deutschen Markt. Die bestehende MAUI-basierte Mobile App (Blazor Hybrid) funktioniert, wirkt aber nicht professionell genug fuer den produktiven Einsatz. Wir entwickeln native iOS- und Android-Apps, um eine erstklassige Nutzererfahrung zu bieten.

### Ausgangslage

**Bestehendes Backend (produktionsreif):**
- ASP.NET Core API mit FastEndpoints, API-Versionierung (v1), Swagger
- PostgreSQL-Datenbank mit Entity Framework Core
- Azure AD B2C Authentifizierung
- Azure Key Vault fuer Secrets in Produktion
- Gehostet unter `api.track.fakturus.com`

**Bestehende Features:**
- Arbeitszeiterfassung (Start/Stop/Finish-Workflow)
- Offline-first mit Background-Sync (30s Intervall)
- Ueberstundenberechnung (monatlich, jaehrlich, mit Schulferien-Beruecksichtigung)
- Urlaubsverwaltung (Anlage/Loeschung, Sync)
- Feiertag-Berechnung nach Bundesland (via Nager.Date)
- Schulferien-Verwaltung
- Kalender-Integration (iCal-Feed Import)
- Benutzereinstellungen (Wochenstunden, Arbeitstage-Bitmask, Bundesland, Urlaubstage)

**Bestehende Mobile App (MAUI/Blazor Hybrid):**
- 4 Tabs: Zeiten, Urlaub (Platzhalter), Gesamt, Settings (Platzhalter)
- SQLite lokale Datenbank mit Offline-Support
- MSAL-basierte Azure B2C Authentifizierung
- Refit API-Clients
- Funktioniert, aber: WebView-basierte UI (nicht nativ), unvollstaendige Seiten (Urlaub, Settings nur Platzhalter)

**Schwester-App fakturus.poi (erfolgreich im Store):**
- Native iOS (Swift/SwiftUI) und Android (Kotlin/Jetpack Compose)
- Bewaehrtes Azure B2C Login-System mit Social Login (Apple, Google, Microsoft, Amazon, E-Mail)
- Saubere MVVM-Architektur mit Service-Layer
- APIClient mit automatischem Token-Management
- Kann als Architektur-Vorlage dienen

### Strategische Entscheidung

Wir entwickeln **native Apps** (nicht MAUI, nicht Cross-Platform) aus folgenden Gruenden:

1. **Performance**: Native UI ist spuerbar schneller als Blazor Hybrid im WebView
2. **UX-Qualitaet**: Plattform-native Patterns (Swipe, Haptics, Animationen) werden nativ umgesetzt
3. **Bewaehrte Grundlage**: fakturus.poi zeigt, dass wir native Apps erfolgreich entwickeln und maintainen koennen
4. **Wiederverwendung**: Auth-System, API-Client-Pattern, CI/CD-Pipeline von fakturus.poi koennen uebernommen werden
5. **App Store Akzeptanz**: Native Apps haben bessere Store-Bewertungen und niedrigere Ablehnungsraten

### Umfang

| Bereich | Details |
|---------|---------|
| iOS | Swift 6, SwiftUI, Minimum iOS 17 |
| Android | Kotlin, Jetpack Compose, Minimum Android 13 (API 33) |
| Backend | Bestehendes ASP.NET Core API -- keine Aenderungen in Phase 1 |
| Auth | Azure AD B2C (gleicher Tenant wie fakturus.poi, eigene App-Registration) |
| Offline | SQLite + strukturierte Sync-Logik (bewahrt von MAUI-App) |

### Zeitplan (Ueberblick)

| Phase | Zeitraum | Fokus |
|-------|----------|-------|
| Phase 1 | Q2 2026 (8 Wochen) | Kern-Zeiterfassung + Auth + Sync |
| Phase 2 | Q3 2026 (6 Wochen) | Gesamt-Uebersicht + Urlaub + Settings |
| Phase 3 | Q3/Q4 2026 (4 Wochen) | Polish, Widgets, Apple Watch |
| Phase 4 | Q4 2026 (4 Wochen) | Store-Launch + MAUI-Abschaltung |

**Gesamtdauer: ca. 22 Wochen (5,5 Monate)**

### Risiken

| Risiko | Wahrscheinlichkeit | Mitigation |
|--------|--------------------|-----------|
| Azure B2C Migration bestehender Nutzer | Mittel | Gleicher B2C-Tenant, Token-Migration testen |
| Offline-Sync Komplexitaet | Hoch | Bewährte Sync-Logik aus MAUI-App als Referenz |
| Parallelbetrieb MAUI + Nativ | Niedrig | Backend ist App-agnostisch, kein Breaking Change |

### Dokument-Uebersicht

- [features.md](features.md) -- Vollstaendige Feature-Liste
- [ios-plan.md](ios-plan.md) -- iOS-Entwicklungsplan
- [android-plan.md](android-plan.md) -- Android-Entwicklungsplan
- [backend-integration.md](backend-integration.md) -- API-Integration
- [auth-concept.md](auth-concept.md) -- Authentifizierungskonzept
- [roadmap.md](roadmap.md) -- Phasenweise Roadmap
- [migration.md](migration.md) -- Migrationsstrategie

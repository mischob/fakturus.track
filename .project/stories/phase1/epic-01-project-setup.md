# EPIC 01: Projekt-Setup & Infrastruktur

## Ziel

Lauffaehige Xcode- und Android-Studio-Projekte mit korrekter Konfiguration, Theme und grundlegender Projektstruktur. Beide Projekte koennen kompiliert und auf Simulator/Emulator gestartet werden.

## Abhaengigkeiten

- Keine (Startpunkt)

## Voraussetzungen

- Xcode 16+ installiert
- Android Studio Ladybug+ installiert
- Apple Developer Account aktiv
- Google Play Developer Account aktiv

---

## Stories

### P1-E01-S01: iOS Xcode-Projekt erstellen

**Als** Entwickler
**moechte ich** ein korrekt konfiguriertes Xcode-Projekt,
**damit** ich mit der iOS-Entwicklung beginnen kann.

**Plattform**: iOS
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: P1-E01-S02 (Android-Projekt)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Xcode-Projekt "FakturusTrack" erstellt mit SwiftUI App Lifecycle
- [ ] Bundle ID: `com.fakturus.track`
- [ ] Minimum Deployment Target: iOS 17.0
- [ ] Swift 6.0 Strict Concurrency aktiviert
- [ ] Ordnerstruktur angelegt (konsistent mit tech-blueprint.md):
  ```
  FakturusTrack/
    App/
    Models/
    Services/
    Features/
    Shared/
    Extensions/
    Resources/
  ```
- [ ] `Configuration.swift` mit API-URLs und B2C-Konfiguration (als Enum mit static lets):
  - `apiBaseUrl`: `https://api.track.fakturus.com`
  - `b2cTenant`: `fakturus.onmicrosoft.com`
  - `b2cClientId`: `3fb35bc6-8825-495e-b0a2-18e00352f968`
  - `b2cPolicy`: `B2C_1_BetaSignInOnly`
  - `b2cScopes`: `["https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"]`
- [ ] Entitlements konfiguriert: Keychain Sharing, Background Modes (fetch)
- [ ] App kompiliert und laeuft im Simulator (leerer weisser Screen ist OK)
- [ ] NSAppTransportSecurity in Info.plist konfiguriert fuer localhost Debug-HTTPS (erlaubt self-signed Zertifikate fuer `localhost` im Debug-Build)
- [ ] MSAL iOS SDK als SPM-Dependency hinzugefuegt

**Technische Hinweise**:
- Orientierung an fakturus.poi iOS-Projektstruktur
- MSAL SPM Package URL: `https://github.com/AzureAD/microsoft-authentication-library-for-objc`
- Keychain Sharing Group: `com.fakturus.track`
- Info.plist: `CFBundleURLSchemes` mit `msauth.com.fakturus.track` fuer MSAL Redirect

---

### P1-E01-S02: Android-Studio-Projekt erstellen

**Als** Entwickler
**moechte ich** ein korrekt konfiguriertes Android-Studio-Projekt,
**damit** ich mit der Android-Entwicklung beginnen kann.

**Plattform**: Android
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: P1-E01-S01 (iOS-Projekt)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Android-Studio-Projekt "FakturusTrack" mit Jetpack Compose
- [ ] Package: `com.fakturus.track`
- [ ] Minimum SDK: API 33 (Android 13)
- [ ] Target SDK: aktuellstes stabiles API-Level
- [ ] Kotlin 2.0+ mit KSP (fuer Room)
- [ ] Ordnerstruktur angelegt:
  ```
  app/src/main/java/com/fakturus/track/
    FakturusTrackApp.kt
    MainActivity.kt
    ServiceContainer.kt
    services/
    features/
    models/
    ui/
    util/
  ```
- [ ] `FakturusTrackApp.kt` als Application-Klasse mit ServiceContainer-Initialisierung
- [ ] `Configuration.kt` object mit API-URLs und B2C-Konfiguration (gleiche Werte wie iOS)
- [ ] Dependencies in build.gradle.kts:
  - Jetpack Compose BOM (aktuell)
  - Material 3
  - Navigation Compose
  - Room (runtime, compiler via KSP)
  - Ktor Client (CIO + ContentNegotiation + Serialization)
  - MSAL Android
  - kotlinx-serialization
- [ ] App kompiliert und laeuft im Emulator (leerer Compose-Screen ist OK)
- [ ] `network_security_config.xml` erlaubt Klartext/self-signed Zertifikate fuer `10.0.2.2` (Emulator-localhost) im Debug-Build
- [ ] MSAL `auth_config.json` in `res/raw/` mit B2C-Konfiguration
- [ ] AndroidManifest: BrowserTabActivity fuer MSAL Redirect konfiguriert

**Technische Hinweise**:
- Orientierung an fakturus.poi Android-Projektstruktur
- Kein Hilt -- manuelle Konstruktor-Injection via ServiceContainer
- MSAL Android: `com.microsoft.identity.client:msal:5+`
- Ktor statt Retrofit (ADR-007)
- `enableEdgeToEdge()` in MainActivity

---

### P1-E01-S03: iOS Material-Theme & Farben

**Als** Entwickler
**moechte ich** ein konsistentes visuelles Theme definiert haben,
**damit** alle UI-Komponenten einheitlich aussehen.

**Plattform**: iOS
**Abhaengigkeiten**: P1-E01-S01
**Parallelisierbar mit**: P1-E01-S04 (Android Theme)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `Assets.xcassets` mit Farbdefinitionen (Light + Dark):
  - Primary: Fakturus-Blau
  - Secondary/Accent
  - Background, Surface
  - Error (Rot), Warning (Orange), Success (Gruen)
  - `timer-running`: Gruen (laufender Timer)
  - `timer-paused`: Gelb/Orange (pausierter Timer)
  - `timer-stopped`: Orange (gestoppter Timer)
  - `pause`: Farbe fuer Pausenanzeige
  - `sync-pending`: Gelb
  - `sync-done`: Gruen
  - `offline-banner`: Gelb/Orange
- [ ] `Date+Formatting.swift` Extension:
  - `formatted(as:)` mit deutschen Formaten: `dd.MM.yyyy`, `HH:mm`, `EEEE dd.MM.`
  - `monthYearString` Property: "Maerz 2026"
  - `weekdayShort` Property: "Fr"
- [ ] `TimeInterval+Display.swift` Extension:
  - `formattedHHMMSS`: "03:42:18" (fuer Timer)
  - `formattedHHMM`: "8:30h" (fuer Dauer-Anzeige)
- [ ] App-Icon Placeholder in Assets (kann spaeter ersetzt werden)

**Technische Hinweise**:
- Deutsche Locale (`Locale(identifier: "de_DE")`) als Default
- Monospaced-Fonts fuer Timer und Dauer-Anzeigen: `.monospacedDigit()`

---

### P1-E01-S04: Android Material-Theme & Farben

**Als** Entwickler
**moechte ich** ein Material 3 Theme konfiguriert haben,
**damit** alle UI-Komponenten einheitlich aussehen.

**Plattform**: Android
**Abhaengigkeiten**: P1-E01-S02
**Parallelisierbar mit**: P1-E01-S03 (iOS Theme)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `Theme.kt` mit Material 3 Color Scheme (Light + Dark):
  - Gleiche Farbpalette wie iOS (visuell konsistent)
  - Dynamic Color deaktiviert (eigenes Branding)
- [ ] `Color.kt` mit Named Colors:
  - `TimerRunning`, `TimerPaused`, `TimerStopped`
  - `PauseColor`
  - `SyncPending`, `SyncDone`
  - `OfflineBanner`
- [ ] `Type.kt` mit Typography:
  - Standard Material 3 Type Scale
  - Timer-Font: monospaced (`FontFamily.Monospace`)
- [ ] `DateFormatting.kt` Utility:
  - `formatDate(localDate)`: "29.03.2026"
  - `formatTime(instant)`: "08:30"
  - `formatMonthYear(localDate)`: "Maerz 2026"
  - `formatWeekdayShort(localDate)`: "Fr"
  - `formatDurationHHMMSS(durationMillis)`: "03:42:18"
  - `formatDurationHHMM(durationMinutes)`: "8:30h"
- [ ] App-Icon Placeholder (Adaptive Icon Struktur)

**Technische Hinweise**:
- Deutsche Locale: `Locale.GERMAN` fuer DateTimeFormatter
- `java.time` API (LocalDate, Instant, Duration)

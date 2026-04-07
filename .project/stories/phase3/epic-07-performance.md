# EPIC 07: Performance-Optimierung

## Ziel

Die App erreicht die definierten Performance-Ziele: Cold Start unter 1 Sekunde, 60fps Scrolling in allen Listen, kein UI-Blocking durch Sync oder Datenbankoperationen. Memory-Leaks werden identifiziert und behoben. Die App ist bereit fuer Store-Review (Apple lehnt Apps mit schlechter Performance ab).

## Abhaengigkeiten

- **Phase 2 abgeschlossen**: Alle Features muessen stehen, damit Performance sinnvoll gemessen wird
- **Keine Abhaengigkeit zu E01**: Performance kann parallel zu UI Polish laufen

---

## Stories

### P3-E07-S01: iOS Performance-Profiling

**Als** Entwickler
**moechte ich** die Performance-Engpaesse der iOS-App identifizieren,
**damit** ich gezielt optimieren kann.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E07-S02, P3-E01-*, P3-E02-*, P3-E03-*, P3-E04-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Cold Start gemessen (Xcode Instruments > App Launch):
  - Ziel: < 1 Sekunde bis interaktives UI
  - Gemessen auf: iPhone 12 (oder aeltestes unterstuetztes Geraet)
- [ ] Scroll-Performance gemessen (Instruments > Core Animation):
  - History-Liste: 60fps bei 100+ Eintraegen
  - Kalender-Monatsnavigation: Kein Stottern
  - Gesamt-Tab Monatstabelle: Smooth Scrolling
- [ ] Memory-Profiling (Instruments > Leaks + Allocations):
  - Kein Memory Leak bei 10-minuetigem Nutzen
  - Memory-Footprint < 50MB bei normaler Nutzung
- [ ] Sync blockiert UI NICHT (Instruments > Time Profiler: Main Thread < 16ms pro Frame)
- [ ] Dokumentation der Ergebnisse in einer Performance-Baseline

**Technische Hinweise**:
- Xcode Instruments: App Launch, Time Profiler, Core Animation, Leaks, Allocations
- Release Build testen (nicht Debug!)
- `os_signpost` fuer Custom-Messpunkte (optional)
- `MetricKit` fuer Langzeit-Performance-Daten (optional, fuer Post-Launch)

---

### P3-E07-S02: Android Performance-Profiling

**Als** Entwickler
**moechte ich** die Performance-Engpaesse der Android-App identifizieren,
**damit** ich gezielt optimieren kann.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E07-S01, P3-E01-*, P3-E02-*, P3-E05-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Cold Start gemessen (Android Studio Profiler):
  - Ziel: < 1 Sekunde bis interaktives UI
  - Gemessen auf: Pixel 6a oder aeltestes unterstuetztes Geraet
- [ ] Scroll-Performance (Compose Metrics / Android GPU Inspector):
  - History-Liste: Kein Frame-Drop bei 100+ Eintraegen
  - Kalender-Navigation: Smooth
- [ ] Memory-Profiling (Android Studio Memory Profiler):
  - Kein Memory Leak
  - Memory-Footprint < 80MB bei normaler Nutzung
- [ ] Sync blockiert UI NICHT (StrictMode aktivieren, Main Thread Checks)
- [ ] Compose Recomposition Profiling: Keine unnoetige Recomposition

**Technische Hinweise**:
- Android Studio Profiler: CPU, Memory, Network
- Compose Compiler Metrics: `composeCompilerMetrics = true` in gradle
- Layout Inspector fuer Recomposition-Zaehler
- `Baseline Profiles` generieren fuer schnelleren App-Start

---

### P3-E07-S03: iOS Performance-Optimierung

**Als** Nutzer
**moechte ich** eine schnelle, fluessige App erleben,
**damit** die Zeiterfassung keinen Frust verursacht.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E07-S01 (Profiling-Ergebnisse)
**Parallelisierbar mit**: P3-E07-S04, P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Cold Start < 1s (nach Optimierung)
- [ ] History-Scrolling: 60fps mit 200 Sessions
- [ ] Kalender-Monatswechsel: < 100ms
- [ ] Tab-Wechsel: < 200ms
- [ ] SwiftData Queries auf Background-Thread (nicht Main Thread)
- [ ] Timer-Update blockiert nicht die UI (1-Sekunden-Timer auf Background)
- [ ] Given die App wird cold gestartet
  When der Splash-Screen erscheint
  Then ist das UI innerhalb von 1 Sekunde interaktiv

**Technische Hinweise**:
- Lazy Loading: History erst laden wenn sichtbar
- `@Query` mit `fetchLimit` fuer initiale Anzeige
- Background SwiftData Context fuer schwere Operationen
- `List` mit `.id()` statt `.onAppear` fuer effizientes Scrolling
- App-Start: Nur Auth-Check + aktuellen Timer laden, Rest lazy

---

### P3-E07-S04: Android Performance-Optimierung

**Als** Nutzer
**moechte ich** eine schnelle, fluessige App erleben,
**damit** die Zeiterfassung keinen Frust verursacht.

**Plattform**: Android
**Abhaengigkeiten**: P3-E07-S02 (Profiling-Ergebnisse)
**Parallelisierbar mit**: P3-E07-S03, P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Cold Start < 1s (nach Optimierung)
- [ ] History-Scrolling: Kein Jank bei 200 Sessions
- [ ] Baseline Profile generiert und in App eingebettet
- [ ] R8 / ProGuard korrekt konfiguriert (Code-Shrinking, kein Stripping von benoetigtem Code)
- [ ] Room Queries auf IO-Dispatcher (nicht Main Thread)
- [ ] Given die App wird cold gestartet
  When der Splash-Screen erscheint
  Then ist das UI innerhalb von 1 Sekunde interaktiv

**Technische Hinweise**:
- Baseline Profiles: `ProfileVerifier` + `ProfileInstaller`
- Compose: `remember {}` und `derivedStateOf {}` fuer teure Berechnungen
- `LazyColumn` Keys korrekt setzen: `items(key = { it.id })` fuer stabile Recomposition
- Room: `@RawQuery` mit `SupportSQLiteQuery` fuer komplexe Queries (falls noetig)
- Startup Tracing: `androidx.startup:startup-runtime` fuer Initialisierungs-Reihenfolge

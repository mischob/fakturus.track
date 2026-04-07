# Phase 3: Polish & Erweiterungen -- Detailplanung

## Scope-Zusammenfassung

Phase 3 macht die App store-reif und fuegt Premium-Features hinzu, die sich von der Konkurrenz abheben. Der Fokus liegt auf Qualitaet, Barrierefreiheit und plattform-nativen Erweiterungen:

- **UI/UX Polish**: Dark Mode, Haptic Feedback, Animationen, Loading States, Error Handling
- **iOS Widgets**: Home Screen Widget mit Timer-Status + Quick Actions
- **Android Widget**: Home Screen Widget analog iOS
- **Barrierefreiheit**: VoiceOver/TalkBack Audit, Dynamic Type, Kontrast-Pruefung
- **Performance**: Cold Start < 1s, Scroll-Performance 60fps, Memory-Optimierung
- **Lokalisierung**: Deutsch (primaer) + Englisch (Fallback), vollstaendige Strings-Extraktion
- **DATEV-Export**: Steuerberater-taugliches Format (PRO-Feature, Differenzierungsmerkmal)
- **App-Einstellungen**: Dark Mode Toggle, Benachrichtigungen, App-Info, Rechtliches

**Post-Launch** (nicht im Phase-3-Scope):
- **Apple Watch Companion** (E02): Minimale App fuer Timer Start/Stop/Pause vom Handgelenk
- **iOS Live Activity** (E04): Dynamic Island + Lock Screen waehrend laufender Session

**Zeitraum**: ~3 Wochen (Mitte August -- Anfang September 2026, Wochen 20-22)
**Ergebnis**: Store-reife App, die Apple/Google Store-Anforderungen erfuellt

---

## EPIC-Uebersicht

| EPIC | Titel | Geschaetzte Dauer | Abhaengigkeiten |
|------|-------|-------------------|-----------------|
| E01 | UI/UX Polish (Dark Mode, Haptics, Animationen) | 1.5 Wochen | Phase 2 abgeschlossen |
| ~~E02~~ | ~~Apple Watch Companion App~~ | ~~1.5 Wochen~~ | **Post-Launch** |
| E03 | iOS Widgets (WidgetKit) | 1 Woche | Phase 2 (WorkSession-Daten) |
| ~~E04~~ | ~~iOS Live Activity & Dynamic Island~~ | ~~1 Woche~~ | **Post-Launch** |
| E05 | Android Widget (Glance) | 1 Woche | Phase 2 (WorkSession-Daten) |
| E06 | Barrierefreiheit (Accessibility) | 1 Woche | E01 (finale UI) |
| E07 | Performance-Optimierung | 1 Woche | Phase 2 abgeschlossen |
| E08 | Lokalisierung (DE + EN) | 1 Woche | E01 (alle UI-Strings final) |
| E09 | DATEV-Export | 1 Woche | Phase 2 E06 (Export-Infrastruktur), E10 (Settings) |
| E10 | App-Einstellungen & Rechtliches | 0.5 Wochen | E01 (Dark Mode) |

---

## Abhaengigkeitsdiagramm

```
                         +------------------+
                         |   Phase 2        |
                         |  abgeschlossen   |
                         +--------+---------+
                                  |
          +-----------+-----------+-----------+----------+
          |           |           |           |          |
    +-----v-----+ +--v------+ +-v-------+ +-v------+   |
    |   E01     | |  E03    | |  E05    | |  E07   |   |
    | UI/UX     | |iOS      | |Android  | |Perform.|   |
    | Polish    | |Widgets  | |Widget   | |Optim.  |   |
    +-----+-----+ +---------+ +---------+ +--------+   |
          |                                             |
    +-----+-----+-----------+                           |
    |           |           |                           |
+---v----+ +---v----+ +----v----+                       |
|  E06   | |  E08   | |  E10    |                       |
|A11y    | |Lokalis.| |App-     |                       |
|Audit   | |DE + EN | |Settings |                       |
+--------+ +--------+ +----+----+                       |
                            |                           |
                      +-----v-----+                     |
                      |   E09     |<--------------------+
                      | DATEV-    |
                      | Export    |
                      +-----------+

  Post-Launch (nicht im Diagramm):
  E02 (Apple Watch), E04 (Live Activity)
```

**Erlaeuterung**:
- **E01 (Polish)** ist Voraussetzung fuer E06 (A11y), E08 (Lokalisierung) und E10 (App-Settings), da die UI erst finalisiert sein sollte
- **E03 (iOS Widget), E05 (Android Widget), E07 (Performance)** sind komplett unabhaengig voneinander und von E01
- **E09 (DATEV)** baut auf der bestehenden Export-Infrastruktur aus Phase 2 auf und braucht E10 fuer das Personalnummer-Feld in den Settings
- **E02 (Apple Watch) und E04 (Live Activity)** werden als Post-Launch-Update nachgeliefert

---

## Parallelitaets-Matrix

**Legende**: P = Parallel moeglich, S = Sequentiell (Abhaengigkeit)

|       | E01 | E03 | E05 | E06 | E07 | E08 | E09 | E10 |
|-------|-----|-----|-----|-----|-----|-----|-----|-----|
| E01   | -   | P   | P   | S   | P   | S   | P   | S   |
| E03   |     | -   | P   | P   | P   | P   | P   | P   |
| E05   |     |     | -   | P   | P   | P   | P   | P   |
| E06   |     |     |     | -   | P   | P   | P   | P   |
| E07   |     |     |     |     | -   | P   | P   | P   |
| E08   |     |     |     |     |     | -   | P   | P   |
| E09   |     |     |     |     |     |     | -   | S   |
| E10   |     |     |     |     |     |     |     | -   |

**Maximale Parallelitaet:**
- Bis zu **4 EPICs** koennen gleichzeitig in Arbeit sein (E01+E03+E05+E07)
- iOS-Agent und Android-Agent arbeiten IMMER parallel am gleichen Feature
- E02 (Apple Watch) und E04 (Live Activity) sind Post-Launch und nicht im Scope

---

## Kritischer Pfad

```
Pfad A:
E01 Polish (1.5 Wo) -> E06 A11y (1 Wo) -> Integration (0.5 Wo) = 3 Wochen

Pfad B:
E01 Polish (1.5 Wo) -> E08 Lokalisierung (1 Wo) -> Integration (0.5 Wo) = 3 Wochen

Pfad C:
E01 Polish (1.5 Wo) -> E10 App-Settings (0.5 Wo) -> E09 DATEV (1 Wo) = 3 Wochen

Pfad D (parallel zu A/B/C, kein Blocker):
E03 Widget (1 Wo) + E05 Android Widget (1 Wo) + E07 Performance (1 Wo) = max 1 Wo (parallel)
```

**Kritischer Pfad: 3 Wochen** bei optimaler Parallelisierung. Kein Puffer eingeplant.
- WidgetKit-Debugging (Widgets haben eigene Lifecycle-Probleme)
- VoiceOver/TalkBack-Fixes die bei Audit auftauchen
- Performance-Bottlenecks die tiefer gehen als erwartet

---

## Dateien in diesem Ordner

| Datei | Inhalt |
|-------|--------|
| [epic-01-ui-polish.md](epic-01-ui-polish.md) | UI/UX Polish (Dark Mode, Haptics, Animationen) |
| [epic-02-apple-watch.md](epic-02-apple-watch.md) | Apple Watch Companion App **(Post-Launch)** |
| [epic-03-ios-widgets.md](epic-03-ios-widgets.md) | iOS Widgets (WidgetKit) |
| [epic-04-live-activity.md](epic-04-live-activity.md) | iOS Live Activity & Dynamic Island **(Post-Launch)** |
| [epic-05-android-widget.md](epic-05-android-widget.md) | Android Widget (Glance) |
| [epic-06-accessibility.md](epic-06-accessibility.md) | Barrierefreiheit (Accessibility) |
| [epic-07-performance.md](epic-07-performance.md) | Performance-Optimierung |
| [epic-08-localization.md](epic-08-localization.md) | Lokalisierung (DE + EN) |
| [epic-09-datev-export.md](epic-09-datev-export.md) | DATEV-Export |
| [epic-10-app-settings.md](epic-10-app-settings.md) | App-Einstellungen & Rechtliches |
| [execution-waves.md](execution-waves.md) | Ausfuehrungsplan in Wellen |
| [implementation-checklist.md](implementation-checklist.md) | Phase-3-spezifische Konventionen |

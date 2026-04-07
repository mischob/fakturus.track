# Ausfuehrungsplan -- Phase 3 in Wellen

## Uebersicht

Phase 3 wird in 4 Wellen ausgefuehrt. Die EPICs sind weitgehend unabhaengig voneinander, was eine **hohe Parallelitaet** ermoeglicht. Bis zu 4 Arbeitsstroeme koennen gleichzeitig laufen.

**Parallel-Kapazitaet**: iOS-Agent + Android-Agent.

> **Hinweis**: E02 (Apple Watch) und E04 (Live Activity) werden als Post-Launch-Update nachgeliefert und sind nicht im Phase-3-Scope enthalten.

```
Woche  20     21     22     23
      +------+------+------+------+
      | W1              | W2       | W3         |  W4
      | Polish+         | A11y+    | DATEV+     |
      | Widgets+Perf    | Lokal.+  | Integration| Test
      |                 | Settings |            |
```

---

## Welle 1: Polish + Widgets + Performance (Woche 20-21)

**Ziel**: Dark Mode, Haptics und Animationen sind implementiert. Widgets zeigen Timer-Status. Performance ist gemessen und optimiert.

**Voraussetzungen**: Phase 2 abgeschlossen.

**Das ist die groesste Welle -- sie nutzt die maximale Parallelitaet aus, weil alle 4 Features unabhaengig sind.**

### Parallel-Strang A: UI/UX Polish (iOS-Agent + Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E01-S01 | iOS Dark Mode | iOS | M |
| P3-E01-S02 | Android Dark Mode | Android | M |
| P3-E01-S03 | iOS Haptic Feedback | iOS | S |
| P3-E01-S04 | Android Haptic Feedback | Android | S |
| P3-E01-S05 | iOS Animationen & Transitionen | iOS | M |
| P3-E01-S06 | Android Animationen & Transitionen | Android | M |
| P3-E01-S07 | iOS Loading States & Skeletons | iOS | S |
| P3-E01-S08 | Android Loading States & Skeletons | Android | S |
| P3-E01-S09 | Error Handling Konsistenz | Beide | M |

### Parallel-Strang B: iOS Widgets (iOS-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E03-S01 | Widget Target & App Group Setup | iOS | S |
| P3-E03-S02 | Timer-Status Widget (Small + Medium) | iOS | M |
| P3-E03-S03 | Widget Quick Actions | iOS | M |

### Parallel-Strang C: Android Widget (Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E05-S01 | Android Widget Setup (Glance) | Android | S |
| P3-E05-S02 | Android Timer-Status Widget | Android | M |
| P3-E05-S03 | Android Widget Quick Actions | Android | M |
| P3-E05-S04 | Android App Shortcut | Android | S |

### Parallel-Strang D: Performance (iOS-Agent + Android-Agent, wenn Kapazitaet)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E07-S01 | iOS Performance-Profiling | iOS | M |
| P3-E07-S02 | Android Performance-Profiling | Android | M |
| P3-E07-S03 | iOS Performance-Optimierung | iOS | M |
| P3-E07-S04 | Android Performance-Optimierung | Android | M |

**Welle 1 DoD**: Dark Mode funktioniert auf beiden Plattformen. Haptics und Animationen sind spuerbar. iOS Widget funktioniert. Android Widget funktioniert. Performance-Ziele (Cold Start < 1s, 60fps Scrolling) sind erreicht.

**Parallele Arbeit**: Bei 1 iOS-Agent + 1 Android-Agent:
- **iOS-Agent**: Strang A (Dark Mode + Haptics zuerst, ca. 1 Tag), dann Strang B (Widgets, ca. 2 Tage), dann Strang D (Performance)
- **Android-Agent**: Strang A (Dark Mode + Haptics, ca. 1 Tag), dann Strang C (Widget, ca. 2 Tage), dann Strang D (Performance)

---

## Welle 2: Accessibility + Lokalisierung + App-Settings (Woche 22)

**Ziel**: Die App ist barrierefrei, lokalisiert (DE + EN) und hat App-Einstellungen (Dark Mode Toggle, App-Info, Rechtliches).

**Voraussetzungen**: Welle 1 Strang A (UI Polish abgeschlossen -- finale UI fuer Audit und String-Extraktion)

### Parallel-Strang A: Accessibility (iOS-Agent + Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E06-S01 | iOS VoiceOver Audit & Fixes | iOS | M |
| P3-E06-S02 | Android TalkBack Audit & Fixes | Android | M |
| P3-E06-S03 | iOS Dynamic Type | iOS | M |
| P3-E06-S04 | Android Schriftgroessen-Anpassung | Android | M |
| P3-E06-S05 | Kontrast-Pruefung | Beide | S |

### Parallel-Strang B: Lokalisierung (iOS-Agent + Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E08-S01 | iOS String-Extraktion & Lokalisierung | iOS | M |
| P3-E08-S02 | Android String-Extraktion & Lokalisierung | Android | M |
| P3-E08-S03 | Datum/Zeit-Formatierung | Beide | S |

### Parallel-Strang C: App-Einstellungen

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E10-S01 | iOS App-Einstellungen Sektion | iOS | M |
| P3-E10-S02 | Android App-Einstellungen Sektion | Android | M |
| P3-E10-S03 | Datenschutzerklaerung & Impressum | Web | M |

**Welle 2 DoD**: VoiceOver und TalkBack funktionieren fuer alle Screens. Dynamic Type skaliert korrekt. Alle Texte in DE + EN lokalisiert. App-Einstellungen (Erscheinungsbild, Benachrichtigungen, Version, Rechtliches) funktionieren.

**Parallele Arbeit**: Strang A und B koennen vom gleichen Agent sequentiell bearbeitet werden (zuerst A11y Audit, dann Strings extrahieren). App-Settings (Strang C) laeuft parallel.

**Hinweis zur Agent-Verteilung**:
- **iOS-Agent**: E06-S01 (VoiceOver) -> E08-S01 (Strings) -> E10-S01 (Settings)
- **Android-Agent**: E06-S02 (TalkBack) -> E08-S02 (Strings) -> E10-S02 (Settings)
- **Kontrast + DateTime + Web**: Kann jeder Agent parallel machen

---

## Welle 3: DATEV-Export (Woche 22.5-23)

**Ziel**: DATEV-Export funktioniert und ist im Gesamt-Tab integriert.

**Voraussetzungen**: Welle 2 Strang C (App-Settings fuer Personalnummer-Feld), Phase 2 E06 (Export-Infrastruktur)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P3-E09-S01 | DATEV-Format Recherche & Spezifikation | Beide | S |
| P3-E09-S02 | iOS DATEV-Export Generator | iOS | M |
| P3-E09-S03 | Android DATEV-Export Generator | Android | M |
| P3-E09-S04 | DATEV-Export UI-Integration | Beide | S |

**Welle 3 DoD**: DATEV-Export generiert korrekt formatierte Dateien. Export ist im Gesamt-Tab verfuegbar. Share-Sheet funktioniert.

**Parallele Arbeit**: S01 (Spezifikation) zuerst, dann S02+S03 parallel (iOS + Android), dann S04.

**Hinweis**: Welle 3 kann teilweise mit Welle 2 ueberlappen -- die DATEV-Spezifikation (S01) hat keine Abhaengigkeit und kann schon in Welle 2 starten. Die Generatoren brauchen nur die Export-Infrastruktur aus Phase 2, nicht die App-Settings. Nur die UI-Integration (S04) braucht das Personalnummer-Feld.

---

## Welle 4: Testing & Bug-Fixing (Woche 22.5-23)

**Ziel**: Store-reife App. Keine bekannten Crashes. Performance-Ziele erreicht.

**Voraussetzungen**: Alle vorherigen Wellen

| Aufgabe | Plattform | Beschreibung |
|---------|-----------|-------------|
| Integrations-Test Phase 1+2+3 | Beide | Vollstaendiger Durchlauf ALLER User-Flows |
| Bug-Fixing | Beide | Gefundene Bugs beheben |
| Performance-Verifizierung | Beide | Cold Start < 1s, 60fps Scrolling, Memory OK |
| Crash-Test | Beide | Edge Cases: Kein Netzwerk, leere DB, viele Sessions |
| TestFlight Build | iOS | Store-reife Beta |
| Firebase App Distribution | Android | Store-reife Beta |
| Regressions-Test Phase 1+2 | Beide | Timer, History, Sync, Pausen, Kalender, Export funktionieren |

### Integrations-Test-Szenarien (Phase 3)

1. **Dark Mode Toggle**: System -> Hell -> Dunkel -> System (sofortiger Wechsel, kein Flicker)
2. **Widget Quick Action**: Timer via Widget starten -> App oeffnen -> Timer laeuft
3. **Android Widget**: Timer via Widget starten -> App oeffnen -> Timer laeuft -> Widget aktualisiert sich
4. **VoiceOver komplett**: Alle 4 Tabs mit VoiceOver durchnavigieren ohne Blocker
5. **Englische Sprache**: Geraet auf Englisch -> App komplett auf Englisch (keine deutschen Reste)
6. **DATEV-Export**: Monatsreport generieren -> Datei oeffnen -> Format pruefen
7. **Dynamic Type XXL**: Groesste Schrift -> Alle Screens lesbar, kein Overflow
8. **Performance**: Cold Start messen (< 1s), History mit 200 Eintraegen scrollen (60fps)

**Welle 4 DoD**: Store-reife App auf TestFlight und Firebase App Distribution. Alle 8 Testszenarien bestanden. Keine kritischen Bugs. Apple/Google Store-Anforderungen erfuellt.

---

## Zusammenfassung: Story-Counts pro Welle

| Welle | Stories | Aufwand-Schwerpunkt | Wochen |
|-------|---------|---------------------|--------|
| W1 | 19 | 5S + 10M + 4M(Perf) | 1.5 |
| W2 | 11 | 1S + 8M + 2M(Web) | 1 |
| W3 | 4 | 2S + 2M (DATEV + Integration) | 0.5 |
| W4 | -- | Testing + Bug-Fixing | 0.5 |
| **Gesamt** | **34 Stories** | | **~3 Wochen** |

> E02 (Apple Watch, 4 Stories) und E04 (Live Activity, 4 Stories) werden als Post-Launch-Update nachgeliefert.

---

## Diagramm: Parallelitaet ueber Zeit

```
Woche:  20          21          22          23
iOS:   [DarkMode][Haptics]
       [Animations─][Loading][Errors]
       [WidgetSetup][WidgetUI──][WidgetActions]
                                      [VoiceOver──][DynType]
                                      [iOS-Strings────────]
                                      [iOS-AppSettings──]
                                      [DATEV-iOS──][DATEV-UI]
                                                          [Test]

Andr:  [DarkMode][Haptics]
       [Animations─][Loading][Errors]
       [WidgetSetup][WidgetUI──][WidgetActions][Shortcut]
                                      [TalkBack──][FontScale]
                                      [Andr-Strings────────]
                                      [Andr-AppSettings──]
                                      [DATEV-Andr─][DATEV-UI]
                                                          [Test]

Perf:  [iOS-Profile][iOS-Optimize──]
       [Andr-Profile][Andr-Optimize]

Web:                               [Datenschutz+Impressum──]
Spec:                              [DATEV-Spec]
```

**Lesehinweis**: Bloecke die vertikal uebereinander stehen laufen parallel. Ein Agent bearbeitet Bloecke in seiner Zeile von links nach rechts.

---

## Kritischer Pfad (Wellen-Perspektive)

```
W1 (1.5 Wo) -> W2 (1 Wo) -> W3 (0.5 Wo) -> W4 (0.5 Wo) = 3.5 Wochen

Optimistischer Pfad (DATEV parallel zu W2):
W1 (1.5 Wo) -> W2+W3 ueberlappend (1 Wo) -> W4 (0.5 Wo) = 3 Wochen

Realistischer Pfad:
W1 (1.5 Wo) -> W2 (1 Wo) -> W3+W4 (0.5 Wo) = 3 Wochen
(DATEV-Spezifikation wird in W2 gestartet, Generatoren in W3 parallel zu Integration)
```

**Risiko**: Welle 1 ist weiterhin breit (19 Stories, 4 Straenge). Bei nur 2 Agents wird es eng. Mitigation: Widget Quick Actions (S03) als "nice-to-have" priorisieren -- grundlegende Widgets sind Pflicht.

> E02 (Apple Watch) und E04 (Live Activity) werden als Post-Launch-Update nachgeliefert. Sie sind "Wow-Features" aber nicht store-kritisch.

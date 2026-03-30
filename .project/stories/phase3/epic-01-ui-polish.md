# EPIC 01: UI/UX Polish (Dark Mode, Haptics, Animationen)

## Ziel

Die App wirkt professionell und hochwertig. Dark Mode funktioniert systemweit. Animationen und Transitionen sind fluessig. Loading States und Error Handling sind konsistent und nutzerfreundlich. Haptic Feedback gibt taktile Rueckmeldung bei wichtigen Aktionen.

## Abhaengigkeiten

- **Phase 2 abgeschlossen**: Alle Screens und Features muessen stehen, bevor Polish angewendet wird
- **Keine Backend-Aenderungen**: Rein client-seitige Verbesserungen

---

## Stories

### P3-E01-S01: iOS Dark Mode

**Als** Nutzer
**moechte ich** die App im Dark Mode nutzen koennen,
**damit** ich auch bei schlechten Lichtverhaeltnissen komfortabel arbeiten kann und mein System-Theme respektiert wird.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S02, P3-E02-*, P3-E03-*, P3-E04-*, P3-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Alle Screens unterstuetzen Dark Mode (kein hardcodierter weisser Hintergrund)
- [ ] Farben nutzen semantische Color-Assets:
  - `Color("primaryBackground")` statt `Color.white`
  - `Color("secondaryBackground")` statt hardcodierte Werte
  - `Color("primaryText")`, `Color("secondaryText")`
- [ ] Custom-Farben (Urlaub-Cyan, Krank-Rot, Feiertag-Lila) sind in beiden Modi gut lesbar
- [ ] Timer-Anzeige ist auch im Dark Mode kontrastreich und gut ablesbar
- [ ] Kalender-Ansicht: Tage, Markierungen und Farbkodierung funktionieren in Dark Mode
- [ ] Given das System-Theme ist auf "Dunkel" gestellt
  When die App geoeffnet wird
  Then erscheint die App im Dark Mode
  And alle Texte sind gut lesbar (WCAG AA Kontrast)
- [ ] Charts/Diagramme im Gesamt-Tab passen sich an Dark Mode an

**Technische Hinweise**:
- SwiftUI nutzt bereits `@Environment(\.colorScheme)` -- Hauptarbeit ist das Ersetzen hardcodierter Farben
- Color-Assets in `Assets.xcassets` mit "Any Appearance" + "Dark Appearance" definieren
- `.preferredColorScheme()` Modifier fuer App-weites Override (wenn User in Settings waehlt)

---

### P3-E01-S02: Android Dark Mode

**Als** Nutzer
**moechte ich** die App im Dark Mode nutzen koennen,
**damit** ich auch bei schlechten Lichtverhaeltnissen komfortabel arbeiten kann.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S01, P3-E02-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Material 3 DynamicColorScheme fuer Light + Dark nutzen
- [ ] Alle Screens passen sich korrekt an (kein hardcodiertes Weiss)
- [ ] Custom-Farben (Urlaub, Krank, Feiertag) funktionieren in beiden Modi
- [ ] Given das System-Theme ist auf "Dunkel" gestellt
  When die App geoeffnet wird
  Then erscheint die App im Dark Mode
  And Material 3 Farben werden korrekt genutzt

**Technische Hinweise**:
- Material 3 `dynamicDarkColorScheme()` / `dynamicLightColorScheme()` nutzen
- `isSystemInDarkTheme()` fuer Compose-basierte Entscheidungen
- Custom Theme-Overrides in `Theme.kt` fuer App-Setting (System/Hell/Dunkel)

---

### P3-E01-S03: iOS Haptic Feedback

**Als** Nutzer
**moechte ich** taktile Rueckmeldung bei wichtigen Aktionen spueren,
**damit** die App sich nativ und hochwertig anfuehlt.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S04, alle anderen EPICs
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `HapticManager.swift` in `Shared/` mit Convenience-Methoden
- [ ] Haptic Feedback bei folgenden Aktionen:
  - Timer Start: `.impact(.medium)`
  - Timer Stop: `.impact(.heavy)`
  - Timer Pause/Weiter: `.impact(.light)`
  - Session Fertig: `.notification(.success)`
  - Session Loeschen: `.notification(.warning)`
  - Urlaubstag Toggle: `.selection()`
  - Krankheitstag setzen: `.selection()`
  - Fehler: `.notification(.error)`
- [ ] Given der Nutzer tippt "Starten"
  When der Timer startet
  Then spuert er ein mittleres haptisches Feedback

**Technische Hinweise**:
- `UIImpactFeedbackGenerator`, `UISelectionFeedbackGenerator`, `UINotificationFeedbackGenerator`
- Generators vorinstanzieren (nicht bei jedem Tap neu erstellen)
- `prepare()` VOR dem erwarteten Event aufrufen fuer minimale Latenz

---

### P3-E01-S04: Android Haptic Feedback

**Als** Nutzer
**moechte ich** taktile Rueckmeldung bei wichtigen Aktionen spueren,
**damit** die App sich nativ und hochwertig anfuehlt.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S03, alle anderen EPICs
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `HapticManager.kt` in `util/`
- [ ] Gleiche Aktionen wie iOS mit plattformgerechten Feedback-Typen
- [ ] Given der Nutzer tippt "Starten"
  When der Timer startet
  Then spuert er eine Vibration

**Technische Hinweise**:
- `HapticFeedbackType.LongPress`, `HapticFeedbackType.TextHandleMove` (Compose)
- Oder `View.performHapticFeedback(HapticFeedbackConstants.CONFIRM)` (API 34+)
- Fallback fuer aeltere APIs: `Vibrator.vibrate(VibrationEffect.createOneShot(...))`

---

### P3-E01-S05: iOS Animationen & Transitionen

**Als** Nutzer
**moechte ich** fluessige Animationen und Uebergaenge in der App sehen,
**damit** die App sich hochwertig und reaktionsfreudig anfuehlt.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S06, alle anderen EPICs
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Timer-Start: Sanfte Animation von "Idle State" zu "Active Session Card"
- [ ] Timer-Stop: Sanfte Animation der Timer-Anzeige (Pulse-Effekt beim Stoppen)
- [ ] Session Fertig: Card gleitet sanft in die History
- [ ] Monatsgruppe auf/zuklappen: Animiertes Expand/Collapse mit `.animation(.spring())`
- [ ] Tab-Wechsel: Natuerliche Uebergangs-Animation
- [ ] Kalender-Monatswechsel: Slide-Animation (links/rechts)
- [ ] Pull-to-Refresh: Nativer ProgressView-Indikator
- [ ] Swipe-to-Delete: Nativer Swipe mit roten Loeschen-Button
- [ ] Given eine Session wird mit "Fertig" abgeschlossen
  When die Card verschwindet
  Then gleitet sie sanft nach oben und die History-Liste aktualisiert sich animiert

**Technische Hinweise**:
- SwiftUI `withAnimation(.spring(response: 0.3, dampingFraction: 0.7))` fuer die meisten Uebergaenge
- `.transition(.asymmetric(insertion: .slide, removal: .opacity))` fuer Card-Uebergaenge
- `.matchedGeometryEffect` fuer Session-Card-zu-History Uebergang (optional, nice-to-have)
- `.contentTransition(.numericText())` fuer Timer-Zaehler (iOS 17+)

---

### P3-E01-S06: Android Animationen & Transitionen

**Als** Nutzer
**moechte ich** fluessige Animationen und Uebergaenge in der App sehen,
**damit** die App sich hochwertig und reaktionsfreudig anfuehlt.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S05, alle anderen EPICs
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Gleiche Animationen wie iOS (plattformgerecht umgesetzt)
- [ ] Material Motion: Shared Element Transitions wo passend
- [ ] Given eine Session wird mit "Fertig" abgeschlossen
  When die Card verschwindet
  Then animiert sie sich sanft heraus

**Technische Hinweise**:
- `AnimatedVisibility`, `animateContentSize()` in Compose
- `Crossfade`, `AnimatedContent` fuer State-Wechsel
- `updateTransition` fuer komplexe Multi-Property-Animationen
- Material Motion: `EnterTransition.slideInVertically()`, `ExitTransition.fadeOut()`

---

### P3-E01-S07: iOS Loading States & Skeletons

**Als** Nutzer
**moechte ich** waehrend des Ladens visuelle Platzhalter sehen,
**damit** ich weiss dass die App arbeitet und nicht eingefroren ist.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S08, alle anderen EPICs
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Gesamt-Tab: Skeleton-Loading waehrend API-Daten geladen werden
  - Summary Cards mit Placeholder-Shimmer
  - Monatstabelle mit grauen Balken
- [ ] Kalender-Tab: Shimmer waehrend Urlaubstage geladen werden
- [ ] Erster App-Start nach Login: "Daten werden geladen..." mit Fortschrittsindikator
- [ ] Given der Gesamt-Tab wird geoeffnet und die API-Daten sind noch nicht da
  When der Nutzer den Tab sieht
  Then zeigen sich animierte Skeleton-Platzhalter

**Technische Hinweise**:
- `.redacted(reason: .placeholder)` mit Custom-Shimmer-Overlay
- Oder: Eigene `ShimmerView` Komponente mit Gradient-Animation
- Nicht JEDE Ansicht braucht Skeletons -- nur Screens mit sichtbarer Ladezeit (> 200ms)

---

### P3-E01-S08: Android Loading States & Skeletons

**Als** Nutzer
**moechte ich** waehrend des Ladens visuelle Platzhalter sehen,
**damit** ich weiss dass die App arbeitet.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: P3-E01-S07, alle anderen EPICs
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Gleiche Loading States wie iOS (plattformgerecht)
- [ ] Material 3 `CircularProgressIndicator` wo passend
- [ ] Shimmer-Effekt fuer Skeleton-Loading

**Technische Hinweise**:
- Accompanist Placeholder oder eigene Shimmer-Composable
- `Modifier.placeholder(visible = isLoading, highlight = PlaceholderHighlight.shimmer())`

---

### P3-E01-S09: Error Handling Konsistenz (Beide Plattformen)

**Als** Nutzer
**moechte ich** bei Fehlern klare, hilfreiche Meldungen sehen,
**damit** ich weiss was schiefgelaufen ist und was ich tun kann.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: Phase 2 abgeschlossen
**Parallelisierbar mit**: Alle anderen EPICs
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Einheitliches Error-Handling Pattern fuer alle Screens:
  - Netzwerk-Fehler: "Keine Internetverbindung. Daten werden lokal gespeichert."
  - API-Fehler: "Synchronisation fehlgeschlagen. Bitte spaeter erneut versuchen."
  - Token-Ablauf: "Sitzung abgelaufen. Bitte erneut anmelden." mit Login-Button
  - Daten-Fehler: "Daten konnten nicht geladen werden." mit Retry-Button
- [ ] iOS: Fehler als Banner (in-App, nicht Alert) -- konsistent mit ArbZG-Hinweisen aus Phase 1
- [ ] Android: Fehler als Snackbar mit optionaler Action ("Erneut versuchen")
- [ ] Retry-Logik: Einmaliger automatischer Retry bei Netzwerk-Fehlern
- [ ] Given eine API-Anfrage schlaegt fehl (z.B. Sync)
  When der Fehler angezeigt wird
  Then ist die Meldung deutsch, verstaendlich und bietet eine Aktion

**Technische Hinweise**:
- Zentrales `ErrorHandler`-Pattern: Fehler-Typ -> Nutzer-Meldung Mapping
- iOS: `NotificationBanner` View mit State-Management
- Android: `SnackbarHostState` in Scaffold
- Fehler-Texte in Lokalisierungsdateien (Vorbereitung fuer E08)

# Devils Advocate Review -- Phase 3 Detailplanung

**Reviewer**: Devils Advocate Agent
**Datum**: 29.03.2026
**Geprueft**: Alle 10 EPICs, tech-specs, execution-waves, parallel-implementation-guide, tech-blueprint, implementation-checklist

---

## Gesamturteil: Empfohlen mit Aenderungen

Die Planung ist insgesamt solide, gut strukturiert und die Parallelisierungsstrategie ist durchdacht. Die Tech-Specs enthalten brauchbare Code-Skizzen. Allerdings gibt es mehrere Punkte, die vor der Umsetzung adressiert werden sollten -- ein KRITISCHES Zeitrisiko, mehrere technische Ungenauigkeiten und einige konzeptionelle Schwaechen.

---

## Befunde

### KRITISCH: Zeitplan ist unrealistisch bei 1 Entwickler + AI-Agents (execution-waves.md)

**Kategorie**: Zeitplanung / Risiko
**Schweregrad**: KRITISCH

**Problem**: Der Plan rechnet mit "bis zu 6 parallelen Arbeitsstroemen" und einem "kritischen Pfad von 3 Wochen". Das impliziert 2-3 unabhaengige AI-Agents die simultan auf verschiedenen Branches arbeiten. Die Realitaet sieht anders aus:

1. **Welle 1 hat 30 Stories in 2 Wochen.** Das sind 3 Stories pro Tag, verteilt auf iOS + Android + watchOS. Jede Story erfordert Review, Testing, und Merge. Das ist mit 1 Mensch + AI extrem ambitioniert.

2. **Der "optimistische Pfad" (3.5 Wochen) hat keinen Puffer.** Der "realistische Pfad" (4 Wochen) hat auch keinen Puffer. Der "sequentielle Pfad" (4.5 Wochen) sprengt den Zeitrahmen. Das heisst: bei EINEM unerwarteten Problem (WidgetKit Lifecycle Bug, Watch Simulator Probleme, Provisioning Profile Issues fuer App Groups) ist der Plan im Verzug.

3. **Merge-Konflikte werden unterschaetzt.** 4 EPICs aendern `TimeTrackingViewModel.swift`. "Alle Aufrufe sind additiv" klingt trivial, aber in der Praxis mit parallelen Branches entstehen trotzdem Konflikte die manuell geloest werden muessen.

4. **watchOS-Entwicklung ist notorisch zeitaufwendig.** Watch-Simulator <-> iPhone-Simulator Kommunikation ist unzuverlaessig. WatchConnectivity-Debugging braucht oft physische Geraete. Die Schaetzung "1.5 Wochen" fuer eine komplette Watch-App inkl. Complications ist optimistisch.

**Risiko**: Phase 3 laeuft in Phase 4 hinein. Phase 4 (Store-Launch) hat einen fixen Termin. Verzoegerung gefaehrdet den Q3/Q4 2026 Launch.

**Alternative**:
- **Scope reduzieren**: Apple Watch (E02) und Live Activity (E04) sind "Wow-Features", aber nicht store-kritisch. Sie koennen als Post-Launch Update nachgeliefert werden. Das spart ~2.5 Wochen Aufwand und entlastet den iOS-Agent massiv.
- **Wenn Watch bleiben muss**: Watch-Complication (E02-S04) als "nice-to-have" einstufen. Nur Timer-Screen und WatchConnectivity sind Pflicht.
- **Puffer einplanen**: Mind. 1 Woche echter Puffer (nicht "Integrationstest"), der auch Puffer ist.

---

### KRITISCH: Widget Quick Actions (iOS) -- Race Condition bei State-Synchronisation

**Kategorie**: Fehleranfaelligkeit / Architektur
**Schweregrad**: KRITISCH

**Problem**: Die Widget Quick Actions (E03-S03) schreiben Timer-State in die App Group und setzen ein Flag (`widgetAction_start`), das die Haupt-App beim naechsten Oeffnen lesen soll. Das ist eine klassische Race Condition:

1. User tippt "Start" im Widget -> Widget schreibt `isRunning=true` + `widgetAction_start=true` in App Group
2. Widget zeigt sofort "Laeuft" an (optimistischer State)
3. Haupt-App wird NICHT sofort geoeffnet. User nutzt Widget weiter.
4. 30 Min spaeter oeffnet User die App -> App liest `widgetAction_start`, startet Session
5. **Problem**: Die StartTime ist jetzt 30 Min falsch (now statt Widget-Tap-Zeitpunkt)

Noch schlimmer: Was passiert wenn die App im Hintergrund ist und der Timer-State schon einen anderen Zustand hat? Was wenn eine alte Session noch aktiv ist?

**Risiko**: Falsche Arbeitszeiten, Datenverlust, inkonsistenter State zwischen Widget und App.

**Alternative**:
- Die `widgetAction_timestamp` ist zwar im Code vorhanden, wird aber nicht genutzt. Die `startSession()` Methode im processWidgetActions muss den Timestamp als StartTime verwenden, nicht `Date()`.
- Noch besser: Interactive Widgets (iOS 17+) koennen via `AppIntent` die App im Hintergrund aufwecken und sofort die Action ausfuehren (nicht erst beim naechsten Oeffnen). Die aktuelle Implementierung nutzt AppIntents, schreibt aber trotzdem in die App Group statt die Action direkt auszufuehren. Das ist ein Designfehler -- `AppIntent.perform()` laeuft im App-Kontext und kann direkt auf SwiftData zugreifen.
- Fuer Android (Glance): Das gleiche Problem existiert mit `ActionCallback`. Die Actions sollten direkt im App-Prozess ausgefuehrt werden (z.B. via Service oder BroadcastReceiver), nicht ueber DataStore-Flags.

---

### BEDENKEN: DATEV-Export Format ist nicht validiert (epic-09-datev-export.md)

**Kategorie**: Fachlichkeit / DATEV-Spezifikation
**Schweregrad**: BEDENKEN

**Problem**: Die DATEV-Spezifikation basiert auf einer vereinfachten Annahme ("CSV-artig mit Semikolon-Trennung"). DATEV hat mehrere Importformate mit unterschiedlichen Anforderungen:

1. **DATEV Lodas** vs. **DATEV LOHN und GEHALT**: Das sind verschiedene Produkte mit verschiedenen Importformaten. Die Spec nennt "Lodas", der Code generiert aber ein generisches CSV.
2. **DATEV-Importformat hat einen Header-Block** mit Metadaten (Berater-Nr., Mandanten-Nr., Abrechnungszeitraum, Erstellungsdatum). Das fehlt komplett in der Spezifikation.
3. **Lohnarten sind steuerberater-spezifisch.** Die Codes 200, 400, 500 sind nicht standardisiert. Jeder Steuerberater kann eigene Lohnart-Nummern verwenden. Ohne Konfigurierbarkeit ist der Export fuer viele Nutzer unbrauchbar.
4. **"UTF-8 ohne BOM"** -- die Spec sagt selbst "DATEV erwartet ASCII/ANSI". UTF-8 ist NICHT das gleiche wie ASCII/ANSI. Deutsche Umlaute in Mitarbeiternamen koennten Probleme verursachen.
5. **Dezimaltrennzeichen Punkt** -- das stimmt fuer Lodas. Aber fuer DATEV Lohn & Gehalt wird Komma erwartet. Die Spec muss klarer sein welches Produkt unterstuetzt wird.

**Risiko**: Export wird von Steuerberatern abgelehnt. Das "starke Differenzierungsmerkmal" wird zum "kaputtes Feature das keiner nutzen kann".

**Alternative**:
- Story P3-E09-S01 ("Recherche & Spezifikation") ist der richtige Ansatz, aber die Akzeptanzkriterien sind zu weich. Konkret: **Vor der Implementierung MUSS eine Test-Datei von einem echten Steuerberater oder DATEV-System importiert und validiert werden.** Ohne diese Validierung sollte das Feature nicht "Done" sein.
- Lohnarten muessen konfigurierbar sein (mindestens als Freitext-Mapping in den Settings). Das fehlt komplett.
- Alternativ: Statt "DATEV-Export" als "CSV-Export fuer Steuerberater" labeln und klar kommunizieren, dass die Lohnarten ggf. angepasst werden muessen. Weniger Versprechen, weniger Enttaeuschung.

---

### BEDENKEN: watchOS Target-Konfiguration in project.yml (tech-spec epic-02)

**Kategorie**: Technische Korrektheit
**Schweregrad**: BEDENKEN

**Problem**: Die project.yml Konfiguration fuer das watchOS Target hat mehrere Probleme:

1. **`type: application` statt `type: watchkit2-app`**: XcodeGen (via project.yml) erwartet fuer watchOS-Apps den Typ `watchkit2-app` (oder ab watchOS 7+ einfach `application` mit korrekter Platform-Angabe). Das KANN funktionieren, muss aber getestet werden.

2. **Fehlende Verknuepfung mit dem Haupt-App Target**: Das watchOS Target muss als Dependency des iOS-Targets konfiguriert werden, sonst wird die Watch-App nicht in die iOS-App eingebettet:
   ```yaml
   FakturusTrack:
     dependencies:
       - target: FakturusTrackWatch
         embed: true
   ```
   Das fehlt in der tech-blueprint.md.

3. **WatchConnectivity Target Membership**: Die Spec sagt "WatchConnectivityManager.swift muss in BEIDE Targets eingebunden sein". Bei project.yml mit separaten `sources` Pfaden ist das nicht trivial. Die Datei muesste entweder in einem Shared-Framework leben oder in beiden `sources`-Listen referenziert werden. Das wird nicht klar beschrieben.

4. **Widget Extension Target hat `dependencies: target: FakturusTrack, embed: false`**: Das ist korrekt fuer eine Extension, aber es muss eine `embed` Referenz in der Haupt-App geben (umgekehrt). Das fehlt ebenfalls.

**Risiko**: Setup scheitert, kostet einen halben Tag Debugging an Xcode/project.yml Konfiguration.

**Alternative**: Ein dedizierter "Target-Setup" Spike (2-4 Stunden) am Anfang von Welle 1, der nur die project.yml konfiguriert, alle Targets baut und die App Group verifiziert. Erst danach beginnen die Feature-Branches.

---

### BEDENKEN: WatchConnectivity -- sendMessage nur bei Reachable, aber kein Fallback

**Kategorie**: Fehleranfaelligkeit / UX
**Schweregrad**: BEDENKEN

**Problem**: Die Watch-App nutzt `sendMessage` fuer Aktionen (Start/Stop/Pause). `sendMessage` funktioniert aber nur wenn das iPhone "reachable" ist (App im Vordergrund oder Background mit aktiver Session). In der Praxis:

1. User hat iPhone in der Tasche, Watch am Handgelenk
2. iPhone-App ist terminiert (nicht nur im Hintergrund)
3. User tippt "Start" auf der Watch
4. `WCSession.default.isReachable` ist `false`
5. Die Action wird verworfen (der Code hat ein `guard WCSession.default.isReachable else { return }`)
6. **User denkt der Timer laeuft, aber nichts ist passiert**

Die Watch zeigt keinen Error, weil `sendAction` nichts zurueckgibt bei `!isReachable`.

**Risiko**: Verlorene Timer-Starts. Nutzer-Frustration. Vertrauensverlust in die Watch-App.

**Alternative**:
- Fallback auf `transferUserInfo` oder `updateApplicationContext` wenn `sendMessage` fehlschlaegt. Diese Methoden stellen die Zustellung sicher, auch wenn die iPhone-App nicht laeuft (das System weckt sie auf).
- Die Watch-App muss visuelles Feedback geben: "Aktion wird gesendet..." und nach Timeout "iPhone nicht erreichbar. Bitte App oeffnen."
- `WatchTimerViewModel.start()` sollte nicht nur `connectivity.sendAction(.start)` aufrufen, sondern auf die Bestaetigung warten und den State erst dann aendern (optimistic vs. pessimistic UI update).

---

### BEDENKEN: TimeTrackingViewModel wird zum God Object

**Kategorie**: Modularitaet / Separation of Concerns
**Schweregrad**: BEDENKEN

**Problem**: Der bestehende `TimeTrackingViewModel` hat bereits 234 Zeilen und verwaltet Session CRUD + Pause-Logik + Sync-Trigger. Phase 3 fuegt hinzu:
- `SharedDefaults.writeTimerState(...)` bei jeder Action (Widget)
- `LiveActivityManager.startActivity(...)` / `.updateActivity(...)` / `.endActivity(...)` (Live Activity)
- `WatchConnectivityManager.shared.sendTimerState(...)` (Watch)
- `HapticManager.xyz()` (Haptics)

Das sind 4-5 zusaetzliche Aufrufe am Ende **jeder** Timer-Methode (start/stop/pause/resume/finish). Der ViewModel wird zum zentralen Knotenpunkt fuer 7+ verschiedene Concerns.

**Risiko**:
- Jede Aenderung an einer Timer-Methode hat Auswirkungen auf Widget, Watch, LiveActivity, Haptics
- Testing wird schwierig (Mock fuer SharedDefaults + LiveActivityManager + WatchConnectivity + HapticManager)
- Die Datei wird 400+ Zeilen, schwer navigierbar

**Alternative**:
- **Observer-Pattern**: Statt die Aufrufe direkt in den ViewModel zu packen, einen `TimerStateNotifier` (oder einfach `Combine`/`@Observable` nutzen) einfuehren. Widget, Watch, LiveActivity und Haptics lauschen auf State-Aenderungen und reagieren selbststaendig.
- Minimaler Ansatz: Eine private `notifyExtensions()` Methode die alle externen Systeme informiert. Zumindest ist der Code dann an einer Stelle statt in 5 Methoden verstreut.
- Der parallel-implementation-guide deutet dieses Problem an ("Aufrufe am Ende der Methoden"), bietet aber keine strukturelle Loesung.

---

### BEDENKEN: Glance Widget -- Timer-Update Limitierung nicht adressiert

**Kategorie**: Technische Korrektheit / UX
**Schweregrad**: BEDENKEN

**Problem**: Android Glance Widgets haben kein Aequivalent zu iOS `Text(timerInterval:)`. Das bedeutet:
- iOS Widget: Timer aktualisiert sich automatisch jede Sekunde (dank `Text(timerInterval:)`)
- Android Widget: Timer zeigt den Stand zum Zeitpunkt des letzten Updates. WorkManager hat ein Minimum-Intervall von 15 Minuten.

Der BroadcastReceiver fuer "sofortige Updates bei State-Aenderung" hilft nur bei State-Wechseln (Start/Stop), nicht fuer den laufenden Timer-Zaehler.

Die Tech-Spec (tech-specs/epic-05) zeigt `formatElapsed(state.startTimeMillis)` in der Widget-Composable, was den Elapsed-Time zum Rendering-Zeitpunkt berechnet. Aber das Widget wird nicht jede Sekunde neu gerendert.

**Risiko**: Android Widget zeigt "03:42" und aktualisiert sich erst in 15 Minuten auf "03:57". iOS Widget zeigt live den Zaehler. Feature-Paritaet verfehlt.

**Alternative**:
- Akzeptieren, dass Android-Widgets keine Live-Timer koennen und das UX entsprechend anpassen: "Seit 08:30" statt "03:42" (Startzeit statt Elapsed Time). Das ist immer korrekt, egal wann das Widget zuletzt aktualisiert wurde.
- Oder: AlarmManager mit 1-Minuten-Intervall fuer Widget-Updates waehrend der Timer laeuft (Batterie-Impact dokumentieren und akzeptieren).

---

### BEDENKEN: Lokalisierung -- String-Extraktion NACH UI Polish ist riskant

**Kategorie**: Reihenfolge / Risiko
**Schweregrad**: BEDENKEN

**Problem**: E08 (Lokalisierung) haengt ab von E01 (UI Polish): "Alle UI-Strings muessen final sein, bevor sie extrahiert werden." Das bedeutet:
- In Welle 1 werden neue hardcodierte Strings eingefuehrt (Error Messages, Loading States, Watch UI, Widget UI)
- In Welle 2 werden diese dann extrahiert

Das heisst: Welle 1 produziert bewusst Code der gegen die eigene Konvention verstoesst ("Kein hardcodierter Text in Views", implementation-checklist.md Punkt 6).

**Risiko**: Strings werden vergessen. Besonders Watch-App und Widget-Strings (die in separaten Targets leben) werden leicht uebersehen bei der Extraktion.

**Alternative**:
- Strings von Anfang an als `String(localized:)` schreiben, auch wenn die Uebersetzungen erst in Welle 2 kommen. Das ist KEIN Mehraufwand -- es ist der gleiche Aufwand, nur frueher. Der String Catalog kann dann in Welle 2 mit Uebersetzungen gefuellt werden, ohne dass Views nochmal angefasst werden muessen.
- Mindestens: Eine String-Inventory-Checkliste pro EPIC, die in E08 abgehakt wird.

---

### HINWEIS: Accessibility -- Keine Mention von Reduce Motion

**Kategorie**: Vollstaendigkeit Accessibility
**Schweregrad**: HINWEIS

**Problem**: E01 fuehrt Animationen ein (spring(), transitions, matchedGeometryEffect). E06 prueft Accessibility. Aber nirgendwo wird `@Environment(\.accessibilityReduceMotion)` erwaehnt.

iOS-Nutzer die in den System-Settings "Reduce Motion" aktiviert haben, erwarten, dass Apps weniger Animationen zeigen. Apple kann Apps ablehnen, die das ignorieren.

**Alternative**: In E01-S05 und E06-S01 einen Akzeptanzpunkt ergaenzen:
- "Given `Reduce Motion` ist aktiviert, then werden spring-Animationen durch fade-Transitionen ersetzt oder entfallen"
- iOS: `@Environment(\.accessibilityReduceMotion) private var reduceMotion`
- Android: `Settings.Global.ANIMATOR_DURATION_SCALE` pruefen

---

### HINWEIS: Performance-Budget -- Memory-Ziel < 50MB iOS ist ambitioniert mit Watch + Widget

**Kategorie**: Performance
**Schweregrad**: HINWEIS

**Problem**: Das Performance-Budget setzt < 50MB Memory fuer iOS. Nach Phase 3 hat die App 3 Targets die sich Memory teilen (via App Group):
- Haupt-App
- Widget Extension (eigener Prozess)
- Watch Extension (eigener Prozess)

Die 50MB gelten vermutlich fuer die Haupt-App allein. Widget Extensions haben ein hartes Memory-Limit von ~30MB (iOS terminiert sie sonst). Das sollte explizit dokumentiert werden, damit bei Performance-Optimierung die Widget-Extension separat gemessen wird.

**Alternative**: Performance-Budget um Extension-Limits erweitern:
- Haupt-App: < 50MB
- Widget Extension: < 20MB (konservativ)
- Watch App: < 25MB (watchOS ist noch restriktiver)

---

### HINWEIS: DATEV-Export -- "PRO"-Badge ohne Feature-Gating ist verwirrend

**Kategorie**: UX-Konsistenz
**Schweregrad**: HINWEIS

**Problem**: E09-S04 sagt: "Button zeigt 'PRO'-Badge (Feature-Gating wird in Phase 4 implementiert, in Phase 3 ist der Button fuer alle sichtbar)". Das bedeutet: In der Beta sehen Nutzer einen "PRO"-Badge neben dem DATEV-Button, koennen ihn aber nutzen. In Phase 4 wird der Zugang gesperrt. Das schafft:
1. Verwirrung bei Beta-Testern ("Was heisst PRO?")
2. Negative Erfahrung bei Launch ("Feature das ich kannte ist jetzt gesperrt")

**Alternative**: In Phase 3 den Badge weglassen. Erst in Phase 4 mit dem Feature-Gating einfuehren. Weniger Verwirrung, kein Code der spaeter geaendert werden muss.

---

### HINWEIS: Widerspruch zwischen Dokumenten -- DATEV Abhaengigkeit

**Kategorie**: Dokumenten-Konsistenz
**Schweregrad**: HINWEIS

**Problem**: Es gibt einen Widerspruch in der Abhaengigkeit von E09 (DATEV):

- **README.md** Abhaengigkeitsdiagramm zeigt: E10 -> E09 (DATEV braucht App-Settings)
- **README.md** Erlaeuterung: "E09 baut auf der bestehenden Export-Infrastruktur aus Phase 2 auf und braucht E10 fuer die Settings-Integration"
- **execution-waves.md** Welle 3 Hinweis: "die DATEV-Spezifikation (S01) hat keine Abhaengigkeit und kann schon in Welle 2 starten. Die Generatoren brauchen nur die Export-Infrastruktur aus Phase 2, nicht die App-Settings. Nur die UI-Integration (S04) braucht das Personalnummer-Feld."

Also: Der Generator (E09-S02/S03) braucht E10 NICHT, nur die UI-Integration (E09-S04) braucht das Personalnummer-Feld aus E10. Das ist im Detail korrekt, aber der README-Dependency-Graph impliziert eine harte Abhaengigkeit E10 -> E09 (komplett), was nicht stimmt.

**Alternative**: README Abhaengigkeitsdiagramm praezisieren: E10 -> E09-S04 (nicht E09 gesamt).

---

### HINWEIS: SharedDefaults.swift -- WidgetKit Import in Shared-Datei

**Kategorie**: Modularitaet
**Schweregrad**: HINWEIS

**Problem**: `SharedDefaults.swift` importiert `WidgetKit` (fuer `WidgetCenter.shared.reloadAllTimelines()`). Diese Datei soll in der Haupt-App, Widget Extension und Watch App genutzt werden. `WidgetKit` ist aber auf watchOS nicht verfuegbar.

**Risiko**: Kompilierungsfehler im Watch-Target.

**Alternative**: Den `WidgetCenter.reloadAllTimelines()` Aufruf hinter ein `#if canImport(WidgetKit)` oder `#if os(iOS)` Guard setzen. Oder: Die writeTimerState-Methode nimmt ein optionales Closure fuer die Widget-Benachrichtigung, das nur in der Haupt-App gesetzt wird.

---

## Positives

Die Planung hat mehrere Staerken, die anerkannt werden sollten:

1. **Klare Abhaengigkeitsanalyse**: Das Parallelitaets-Diagramm und die Merge-Reihenfolge sind gut durchdacht. Die Contracts zwischen Features (A-D) sind eine hervorragende Idee.

2. **Mock-Strategie**: Die Mocks fuer isolierte Entwicklung (SharedDefaults Mock, WatchConnectivity Mock) ermoeglichen echte Parallelarbeit. Gut.

3. **Bestehenden Code respektieren**: Die Planung ist vorsichtig mit dem bestehenden TimeTrackingViewModel -- additive Aenderungen statt Refactoring. Fuer einen 4-Wochen-Sprint ist das die richtige Entscheidung.

4. **Theme.swift ist bereits Dark-Mode-ready**: Die bestehende `Color(light:dark:)` Extension zeigt, dass Phase 1/2 vorausschauend implementiert wurde. E01-S01 wird damit einfacher als geschaetzt.

5. **iOS Deployment Target 17.0**: Das ermoeglicht Interactive Widgets, String Catalogs, `contentTransition(.numericText())` und andere moderne APIs ohne Fallbacks. Gute Entscheidung.

6. **Keine neuen SPM-Dependencies fuer iOS**: WidgetKit, ActivityKit, WatchConnectivity sind alles System-Frameworks. Kein Dependency-Risiko.

7. **DATEV als PRO-Feature**: Richtige Tier-Zuordnung. Differenzierungsmerkmal hinter dem Premium-Tier ist strategisch sinnvoll.

8. **Ausfuehrliche Accessibility-Checkliste**: Die VoiceOver/TalkBack-Labels sind konkret und vollstaendig beschrieben (nicht nur "mach Accessibility").

---

## Empfehlung

### Sofort-Massnahmen (vor Implementierungsbeginn)

1. **Scope priorisieren**: Apple Watch (E02) und/oder Live Activity (E04) als "Post-Launch" markieren. Mindestens eines der beiden muss verschoben werden um realistischen Puffer zu schaffen. Empfehlung: Live Activity verschieben -- es erfordert das gleiche Widget-Target wie E03, macht die Integration komplexer und ist kein Store-Blocker.

2. **Widget Quick Actions Architektur ueberarbeiten**: `AppIntent.perform()` sollte die Timer-Action direkt ausfuehren (SwiftData-Zugriff im App-Kontext), nicht ueber App-Group-Flags. Das ist sowohl korrekter als auch einfacher.

3. **project.yml Target-Setup als erstes validieren**: Bevor irgendein Feature-Branch erstellt wird, muss die Multi-Target-Konfiguration (App + Widget + Watch + App Groups) funktionieren.

4. **SharedDefaults.swift: watchOS-Guard einfuegen** fuer den WidgetKit-Import.

### Vor Welle 2

5. **DATEV-Spezifikation extern validieren lassen** (Steuerberater oder DATEV-Kenner). Nicht erst nach der Implementierung.

6. **Lohnarten-Konfigurierbarkeit** mindestens als Freitext-Feld in den Settings einplanen.

### Laufend

7. **Strings von Anfang an lokalisierbar schreiben** (`String(localized:)`) statt nachtraeglich in Welle 2 zu extrahieren.

8. **`accessibilityReduceMotion`** in die Animations-Stories (E01-S05/S06) aufnehmen.

9. **Android Widget**: Startzeit statt Elapsed Time anzeigen, um das 15-Min-Update-Limit zu umgehen.

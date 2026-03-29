# EPIC 02: Einstellungen-Tab (Settings UI + Sync)

## Ziel

Der Einstellungen-Tab (Tab 4) erhaelt eine vollstaendige UI fuer alle Benutzereinstellungen: Arbeitszeit-Konfiguration, Bundesland-Auswahl, Urlaubstage, Kalender-URL, Schulferien-Verwaltung und Profil/Logout. Settings werden bidirektional mit dem Backend synchronisiert (Last-Write-Wins).

## Abhaengigkeiten

- **Phase 1**: Settings-Sync-Infrastruktur (SyncEngine.syncUserSettings), AuthManager (fuer Profil/Logout), App-Shell (Tab-Navigation)
- **Phase 1 E03**: UserSettings Model/Entity existiert bereits lokal
- **Keine Backend-Aenderung noetig**: Settings-API existiert bereits (`GET/PUT /v1/settings`, SchoolHolidays CRUD)

---

## Stories

### P2-E02-S01: iOS Settings-Screen Grundstruktur

**Als** Nutzer
**moechte ich** meine Einstellungen in einer uebersichtlichen Liste sehen,
**damit** ich alle Konfigurationsoptionen auf einen Blick finde.

**Plattform**: iOS
**Abhaengigkeiten**: Phase 1 (Tab-Navigation, UserSettings Model)
**Parallelisierbar mit**: P2-E02-S02 (Android Settings), alle E01/E05 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SettingsScreen.swift` als SwiftUI View im `Features/Settings/` Ordner
- [ ] InsetGroupedList-Style mit folgenden Sektionen:
  - PROFIL: Avatar (Initialen), Name, E-Mail, Logout-Button
  - ARBEITSZEIT: Stunden/Woche (TextField, numerisch), Arbeitstage (WorkdaySelector)
  - STANDORT: Bundesland (Picker), Anzeige "X Feiertage in {Jahr}"
  - URLAUB: Urlaubstage/Jahr (TextField, numerisch)
  - KALENDER: Kalender-URL (TextField), Schulferien (NavigationLink zu Sub-Screen)
  - APP: Version, Datenschutz (Link), Lizenzen (Link)
- [ ] Large Title: "Einstellungen"
- [ ] Given der Nutzer oeffnet den Einstellungen-Tab
  When die Settings geladen sind
  Then werden alle aktuellen Werte angezeigt (aus lokaler DB)
- [ ] Settings werden aus UserSettings-Model geladen (lokal)
- [ ] Platzhalter fuer Profil-Daten (Name/E-Mail aus B2C Claims, kann anfangs leer sein)

**Technische Hinweise**:
- `List { Section("ARBEITSZEIT") { ... } }` mit `.listStyle(.insetGrouped)`
- UserSettings via `@Query` oder SettingsViewModel laden
- Profil-Daten aus MSAL Account-Claims (display name, email)

---

### P2-E02-S02: Android Settings-Screen Grundstruktur

**Als** Nutzer
**moechte ich** meine Einstellungen in einer uebersichtlichen Liste sehen,
**damit** ich alle Konfigurationsoptionen auf einen Blick finde.

**Plattform**: Android
**Abhaengigkeiten**: Phase 1 (Navigation, UserSettingsEntity)
**Parallelisierbar mit**: P2-E02-S01 (iOS Settings), alle E01/E05 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SettingsScreen.kt` als Composable im `features/settings/` Ordner
- [ ] LazyColumn mit Material 3 Styling, gleiche Sektionen wie iOS:
  - Profil-Header (Card mit Avatar, Name, E-Mail, Logout-Button)
  - ARBEITSZEIT: Wochenstunden (OutlinedTextField), Arbeitstage (FilterChip Row)
  - STANDORT: Bundesland (ExposedDropdownMenuBox), Feiertag-Info
  - URLAUB: Urlaubstage/Jahr (OutlinedTextField)
  - KALENDER: Kalender-URL (OutlinedTextField), Schulferien (clickable Row -> Sub-Screen)
  - APP: Version, Datenschutz, Lizenzen
- [ ] Sektions-Header als Text mit `MaterialTheme.typography.labelLarge`
- [ ] Settings werden aus UserSettingsEntity via Room Flow geladen

**Technische Hinweise**:
- Kein Material 3 PreferenceScreen (zu eingeschraenkt), stattdessen LazyColumn mit eigenen Items
- `ExposedDropdownMenuBox` fuer Bundesland-Picker
- `FilterChip` fuer Arbeitstage (Mo-So Toggles)

---

### P2-E02-S03: iOS WorkdaySelector-Komponente

**Als** Nutzer
**moechte ich** meine Arbeitstage visuell per Tap auswaehlen koennen,
**damit** ich intuitiv sehe welche Tage aktiv sind.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E02-S01 (Settings-Screen als Kontext)
**Parallelisierbar mit**: P2-E02-S04 (Android), P2-E02-S05/S06 (Bundesland)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `WorkdaySelector.swift` als wiederverwendbare Komponente in `Shared/`
- [ ] 7 Tages-Buttons (Mo-So) horizontal angeordnet
- [ ] Aktive Tage: Filled Pill (Primary Color), Inaktive: Outline Pill (Gray)
- [ ] Tap toggled den jeweiligen Tag (Bitmask wird aktualisiert)
- [ ] Props: `workDays: Binding<Int>` (Bitmask: 1=Mo, 2=Di, 4=Mi, 8=Do, 16=Fr, 32=Sa, 64=So)
- [ ] Given workDays = 31 (Mo-Fr)
  When Nutzer auf "Fr" tippt
  Then workDays = 15 (Mo-Do)
  And Freitag wird als inaktiv dargestellt
- [ ] Given workDays = 15 (Mo-Do)
  When Nutzer auf "Sa" tippt
  Then workDays = 47 (Mo-Do + Sa)

**Technische Hinweise**:
- Bitmask-Logik: `workDays & (1 << dayIndex)` fuer Check, `workDays ^= (1 << dayIndex)` fuer Toggle
- Tag-Labels: ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"]
- Capsule-Shape fuer Buttons

---

### P2-E02-S04: Android WorkdaySelector-Komponente

**Als** Nutzer
**moechte ich** meine Arbeitstage visuell per Tap auswaehlen koennen,
**damit** ich intuitiv sehe welche Tage aktiv sind.

**Plattform**: Android
**Abhaengigkeiten**: P2-E02-S02 (Settings-Screen als Kontext)
**Parallelisierbar mit**: P2-E02-S03 (iOS), P2-E02-S05/S06 (Bundesland)
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] `WorkdaySelector.kt` als Composable in `ui/shared/`
- [ ] 7 FilterChips horizontal (FlowRow oder LazyRow)
- [ ] Aktive Tage: `FilterChip(selected = true)`, Inaktive: `FilterChip(selected = false)`
- [ ] Gleiche Bitmask-Logik wie iOS
- [ ] Parameter: `workDays: Int`, `onWorkDaysChange: (Int) -> Unit`

**Technische Hinweise**:
- `FilterChip` aus Material 3 (`androidx.compose.material3`)
- `Row(horizontalArrangement = Arrangement.spacedBy(4.dp))`

---

### P2-E02-S05: iOS BundeslandPicker mit Feiertag-Vorschau

**Als** Nutzer
**moechte ich** mein Bundesland auswaehlen und sofort sehen wie viele Feiertage es betrifft,
**damit** ich die Auswirkung auf meine Ueberstunden-Berechnung verstehe.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E02-S01 (Settings-Screen)
**Parallelisierbar mit**: P2-E02-S06 (Android), P2-E02-S03 (Workdays)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `BundeslandPicker.swift` als Komponente in `Shared/`
- [ ] Picker mit allen 16 Bundeslaendern (Vollname + Kuerzel)
- [ ] Unter dem Picker: "X Feiertage in {aktuelles Jahr}"
- [ ] Feiertag-Berechnung basierend auf ausgewaehltem Bundesland:
  - Bundesweite Feiertage (Neujahr, Karfreitag, Ostermontag, Tag der Arbeit, Christi Himmelfahrt, Pfingstmontag, Tag der Deutschen Einheit, 1./2. Weihnachtstag)
  - Plus landesspezifische Feiertage je nach Bundesland
- [ ] Given Bundesland = "BY" (Bayern)
  When die Feiertag-Anzahl berechnet wird
  Then werden 13 Feiertage angezeigt (9 bundesweit + Heilige Drei Koenige + Fronleichnam + Mariae Himmelfahrt + Allerheiligen)
- [ ] Given Bundesland = "HH" (Hamburg)
  When die Feiertag-Anzahl berechnet wird
  Then werden 10 Feiertage angezeigt (9 bundesweit + Reformationstag)
- [ ] Props: `bundesland: Binding<String>`, `year: Int`

**Technische Hinweise**:
- Feiertag-Berechnung kann lokal in der App erfolgen (statische Regeln + Oster-Berechnung)
- Alternative: Backend `/v1/holidays?bundesland=BY&year=2026` falls vorhanden
- Das Backend nutzt Nager.Date fuer Feiertag-Berechnung -- fuer die App reicht eine lokale Implementierung
- Enum `Bundesland` mit allen 16 Laendern und ihren Feiertagen

---

### P2-E02-S06: Android BundeslandPicker mit Feiertag-Vorschau

**Als** Nutzer
**moechte ich** mein Bundesland auswaehlen und sofort sehen wie viele Feiertage es betrifft,
**damit** ich die Auswirkung auf meine Ueberstunden-Berechnung verstehe.

**Plattform**: Android
**Abhaengigkeiten**: P2-E02-S02 (Settings-Screen)
**Parallelisierbar mit**: P2-E02-S05 (iOS), P2-E02-S04 (Workdays)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `BundeslandPicker.kt` als Composable in `ui/shared/`
- [ ] `ExposedDropdownMenuBox` mit allen 16 Bundeslaendern
- [ ] Feiertag-Info unterhalb: "X Feiertage in {Jahr}"
- [ ] Gleiche Feiertag-Berechnung wie iOS
- [ ] Parameter: `bundesland: String`, `onBundeslandChange: (String) -> Unit`, `year: Int`

**Technische Hinweise**:
- `ExposedDropdownMenuBox` mit `ExposedDropdownMenu` und `DropdownMenuItem`
- Feiertag-Berechnung als Utility-Klasse `HolidayCalculator`

---

### P2-E02-S07: iOS SettingsViewModel + Auto-Save + Sync

**Als** Nutzer
**moechte ich** dass meine Einstellungen automatisch gespeichert und synchronisiert werden,
**damit** ich keinen expliziten Speichern-Button druecken muss.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E02-S01 (Settings-Screen), Phase 1 (SyncEngine.syncUserSettings)
**Parallelisierbar mit**: P2-E02-S08 (Android ViewModel)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SettingsViewModel.swift` als `@Observable` Klasse
- [ ] Laedt UserSettings aus SwiftData beim Init
- [ ] Auto-Save bei jeder Aenderung:
  - Given der Nutzer aendert Wochenstunden von 40 auf 32
    When die Aenderung erfolgt (Debounce: 500ms nach letzter Eingabe)
    Then werden die Settings lokal gespeichert (isPendingSync = true, updatedAt = now)
    And ein Sync wird getriggert
- [ ] Validierung:
  - Wochenstunden: 0-168 (max 24*7)
  - Urlaubstage: 0-365
  - Mindestens 1 Arbeitstag muss aktiv sein
- [ ] Fehler-Handling:
  - Given eine ungueltige Eingabe (z.B. Wochenstunden = -5)
    When validiert wird
    Then wird ein Inline-Fehler angezeigt
    And die Aenderung wird NICHT gespeichert
- [ ] Logout-Funktion:
  - Given der Nutzer tippt auf "Abmelden"
    When ein Bestaetigung-Dialog erscheint und bestaetigt wird
    Then wird MSAL Logout aufgerufen
    And der Nutzer wird zum Login-Screen geleitet
- [ ] Profil-Daten (Name, E-Mail) aus AuthManager laden

**Technische Hinweise**:
- Settings-Sync nutzt Last-Write-Wins (siehe shared-concepts.md Abschnitt 2)
- Debounce mit `Task` + `try await Task.sleep(nanoseconds:)`
- Logout: `AuthManager.signOut()` -> Login-Screen

---

### P2-E02-S08: Android SettingsViewModel + Auto-Save + Sync

**Als** Nutzer
**moechte ich** dass meine Einstellungen automatisch gespeichert und synchronisiert werden,
**damit** ich keinen expliziten Speichern-Button druecken muss.

**Plattform**: Android
**Abhaengigkeiten**: P2-E02-S02 (Settings-Screen), Phase 1 (SyncEngine)
**Parallelisierbar mit**: P2-E02-S07 (iOS ViewModel)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SettingsViewModel.kt` mit StateFlow fuer alle Settings-Felder
- [ ] Auto-Save mit Debounce (500ms) analog zu iOS
- [ ] Gleiche Validierungen wie iOS
- [ ] Logout mit Bestaetigung
- [ ] Settings aus Room Flow laden, bei Aenderung via DAO speichern
- [ ] Sync-Trigger nach Speichern

**Technische Hinweise**:
- Debounce: `MutableStateFlow` + `debounce(500)` + `collectLatest`
- Room Flow fuer reaktive Settings-Anzeige
- Logout: `AuthManager.signOut()` + Navigation zum Login

---

### P2-E02-S09: iOS Schulferien-Verwaltung

**Als** Nutzer
**moechte ich** meine Schulferien-Zeitraeume verwalten koennen,
**damit** diese bei der Ueberstunden-Berechnung beruecksichtigt werden.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E02-S01 (Settings-Screen als Navigation-Kontext), Phase 1 (SchoolHolidayPeriod Model)
**Parallelisierbar mit**: P2-E02-S10 (Android), alle anderen E02-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Sub-Screen `SchoolHolidaysScreen.swift` erreichbar via NavigationLink aus Settings
- [ ] Liste der Schulferien gruppiert nach Jahr
- [ ] "+" Button in der Navigation Bar fuer neuen Eintrag
- [ ] Sheet fuer Bearbeiten/Erstellen:
  - Name (TextField, z.B. "Osterferien")
  - Start-Datum (DatePicker)
  - End-Datum (DatePicker)
  - Validierung: End > Start
- [ ] Swipe-to-Delete fuer bestehende Eintraege
- [ ] Given der Nutzer erstellt "Sommerferien" vom 06.07. bis 18.08.2026
  When gespeichert wird
  Then erscheint der Eintrag in der Liste unter "2026"
  And der Eintrag wird mit dem Backend synchronisiert (POST /v1/school-holidays)
- [ ] Given der Nutzer loescht "Osterferien"
  When der Eintrag entfernt wird
  Then wird DELETE /v1/school-holidays/{id} aufgerufen

**Technische Hinweise**:
- SchoolHolidayPeriod Model aus data-layer.md nutzen
- Backend-Endpoints: POST/PUT/DELETE /v1/school-holidays
- Sync kann einfach per CRUD erfolgen (kein Bulk-Sync noetig, da wenige Eintraege)

---

### P2-E02-S10: Android Schulferien-Verwaltung

**Als** Nutzer
**moechte ich** meine Schulferien-Zeitraeume verwalten koennen,
**damit** diese bei der Ueberstunden-Berechnung beruecksichtigt werden.

**Plattform**: Android
**Abhaengigkeiten**: P2-E02-S02 (Settings-Screen), Phase 1 (Room Entities)
**Parallelisierbar mit**: P2-E02-S09 (iOS), alle anderen E02-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Sub-Screen `SchoolHolidaysScreen.kt` mit LazyColumn
- [ ] FAB oder Top-Bar "+" fuer neuen Eintrag
- [ ] Dialog fuer Bearbeiten/Erstellen (Name, Start, Ende mit DatePicker)
- [ ] SwipeToDismiss fuer Loeschen
- [ ] Gleiche Sync-Logik wie iOS (CRUD gegen Backend)

**Technische Hinweise**:
- Material 3 `DatePickerDialog` fuer Datumsauswahl
- `AlertDialog` mit TextFields fuer den Erstellen/Bearbeiten-Dialog
- SchoolHoliday-Entity und DAO muessen in Phase 2 erstellt werden (falls nicht in Phase 1 angelegt)

---

### P2-E02-S11: Feiertag-Berechnungslogik (Shared Utility)

**Als** Entwickler
**moechte ich** eine plattformuebergreifend konsistente Feiertag-Berechnung haben,
**damit** Bundesland-Picker, Kalender und Gesamt-Tab die gleichen Feiertage anzeigen.

**Plattform**: Beide (iOS + Android, jeweils eigene Implementierung)
**Abhaengigkeiten**: Keine (Utility-Klasse)
**Parallelisierbar mit**: Alle Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] iOS: `HolidayCalculator.swift` in `Shared/`
- [ ] Android: `HolidayCalculator.kt` in `util/`
- [ ] Methode: `holidays(bundesland: String, year: Int) -> [(Date, String)]`
  - Gibt alle Feiertage mit Name und Datum zurueck
- [ ] Bundesweite Feiertage (9 Stueck):
  - Neujahr (01.01.), Karfreitag (variabel), Ostermontag (variabel)
  - Tag der Arbeit (01.05.), Christi Himmelfahrt (variabel), Pfingstmontag (variabel)
  - Tag der Deutschen Einheit (03.10.), 1. Weihnachtstag (25.12.), 2. Weihnachtstag (26.12.)
- [ ] Landesspezifische Feiertage:
  - Heilige Drei Koenige (06.01.): BW, BY, ST
  - Internationaler Frauentag (08.03.): BE
  - Fronleichnam (variabel): BW, BY, HE, NW, RP, SL
  - Mariae Himmelfahrt (15.08.): BY (teilweise), SL
  - Weltkindertag (20.09.): TH
  - Reformationstag (31.10.): BB, HB, HH, MV, NI, SH, SN, ST, TH
  - Allerheiligen (01.11.): BW, BY, NW, RP, SL
  - Buss- und Bettag (variabel): SN
- [ ] Oster-Berechnung (Gauss-Algorithmus) fuer variable Feiertage
- [ ] Given bundesland = "NW", year = 2026
  When holidays() aufgerufen wird
  Then werden 11 Feiertage zurueckgegeben (9 bundesweit + Fronleichnam + Allerheiligen)
- [ ] Given bundesland = "SN", year = 2026
  When holidays() aufgerufen wird
  Then werden 11 Feiertage zurueckgegeben (9 bundesweit + Reformationstag + Buss-und-Bettag)

**Technische Hinweise**:
- Gauss'sche Osterformel: `a = year % 19; b = year / 100; ...` (Standard-Implementierung)
- Christi Himmelfahrt = Ostern + 39 Tage
- Pfingstmontag = Ostern + 50 Tage
- Fronleichnam = Ostern + 60 Tage
- Buss- und Bettag = Mittwoch vor dem 23. November
- Bayern Mariae Himmelfahrt: Gilt in Gemeinden mit ueberwiegend katholischer Bevoelkerung -- fuer die App vereinfachen wir: gilt in ganz Bayern (Praxis-Entscheidung)

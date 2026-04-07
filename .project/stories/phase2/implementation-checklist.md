# Implementation-Checkliste -- Phase 2

Dieses Dokument muss jeder Entwickler / AI-Agent lesen, bevor er eine Phase-2-Story anfaengt. Es ergaenzt die Phase-1-Checkliste (`stories/phase1/implementation-checklist.md`) um Phase-2-spezifische Hinweise.

---

## 1. Phase-1-Checkliste gilt weiterhin

Alle Konventionen aus der Phase-1-Checkliste gelten unveraendert:
- Namenskonventionen (Dateien, Klassen, Properties, DTOs)
- Ordnerstruktur (NICHT aendern -- neue Features in bestehende Ordner)
- Code-Stil (Deutsch fuer UI-Strings, Englisch fuer Code)
- Error Handling (ViewModels fangen, SyncEngine loggt)
- Git-Workflow (Branch-Naming, Commit-Messages)
- Definition of Done (Akzeptanzkriterien, Tests, Previews)

---

## 2. Phase-2-spezifische Ordner

### Neue Feature-Ordner (in bestehende Struktur integrieren)

**iOS:**
```
Features/
  Settings/        -- SettingsScreen, SettingsViewModel, SchoolHolidaysScreen
  Vacation/        -- VacationScreen, VacationViewModel, VacationCalendar
  Overview/        -- OverviewScreen, OverviewViewModel
Services/
  Export/          -- PDFReportGenerator, CSVExporter
Shared/
  WorkdaySelector.swift
  BundeslandPicker.swift
  OvertimeCard.swift
  HolidayCalculator.swift
```

**Android:**
```
features/
  settings/        -- SettingsScreen, SettingsViewModel, SchoolHolidaysScreen
  vacation/        -- VacationScreen, VacationViewModel
  overview/        -- OverviewScreen, OverviewViewModel
services/
  export/          -- PDFReportGenerator, CSVExporter
ui/shared/
  WorkdaySelector.kt
  BundeslandPicker.kt
  OvertimeCard.kt
  VacationCalendar.kt
util/
  HolidayCalculator.kt
```

---

## 3. Bestehende Dateien erweitern (NICHT neu erstellen)

Phase 2 erweitert viele bestehende Phase-1-Dateien. IMMER zuerst pruefen ob die Datei existiert:

| Datei | Phase-2-Erweiterung |
|-------|---------------------|
| `SyncEngine` (iOS/Android) | `syncSickDays()` hinzufuegen, in `syncAll()` aufrufen |
| `APIClient` (iOS/Android) | SickDay-Methoden hinzufuegen (getSickDays, syncSickDays, ...) |
| `DTOs` (iOS/Android) | SickDay DTOs hinzufuegen (SickDaySyncItem, SickDayDTO, ...) |
| `AppDatabase` (Android) | `sickDayDao()` hinzufuegen, Schema-Version erhoehen |
| `SwiftData Container` (iOS) | SickDay zum Schema hinzufuegen |
| `App-Shell / Navigation` | Placeholder-Tabs durch echte Screens ersetzen |

**ACHTUNG**: Bestehende Funktionalitaet NICHT brechen! Phase-1-Features (Timer, History, Sync, Pausen) muessen weiterhin funktionieren.

---

## 4. Neue DB-Models / Entities

### SickDay (neue Entity in Phase 2)

| Plattform | Phase 1 Version | Phase 2 Version | Migration |
|-----------|----------------|----------------|-----------|
| iOS       | V1             | V2             | V1→V2     |
| Android   | V2             | V3             | V2→V3     |

**iOS (SwiftData):**
- Schema-Version von V1 auf V2 erhoehen
- `SickDay` Model zu `SchemaV2.models` hinzufuegen
- Migration V1->V2 definieren (oder leichtgewichtige Migration nutzen)

**Android (Room):**
- Schema-Version von V2 auf V3 erhoehen
- `MIGRATION_2_3` implementieren: `CREATE TABLE sick_days (...)`
- `SickDayDao` erstellen und in `AppDatabase` registrieren

### SchoolHolidayPeriod

- Pruefen ob in Phase 1 bereits angelegt (siehe data-layer.md)
- Falls nicht: analog zu SickDay als neue Entity erstellen
- Backend-CRUD (POST/PUT/DELETE /v1/school-holidays) nutzen (kein Bulk-Sync)

---

## 5. Feiertag-Berechnung

Die Feiertag-Berechnung wird an mehreren Stellen gebraucht:
- **BundeslandPicker** (E02): Zeigt "X Feiertage in {Jahr}"
- **VacationCalendar** (E03): Markiert Feiertage als nicht-antippbar
- **OverviewScreen** (E05): Zeigt Feiertag-Anzahl in Summary-Card
- **CSVExporter** (E06): Markiert Feiertage in der Typ-Spalte
- **PDFReportGenerator** (E06): Listet Feiertage im Report

**Daher: EINE zentrale HolidayCalculator-Klasse** (E02-S11), die von allen genutzt wird.

**Wichtig: Caching!** `holidays(bundesland, year)` pro Monatswechsel EINMAL aufrufen und als Set cachen. Nicht bei jedem Cell-Render neu berechnen. Die Gauss-Osterformel ist zwar schnell, aber das wiederholte Erzeugen von Date-Objekten und Set-Lookups bei jedem Frame ist unnoetig und kann bei Kalender-Scrolling zu Rucklern fuehren.

### Oster-Berechnung (Gauss)

```
// Vereinfachte Gauss-Osterformel
a = year % 19
b = year / 100
c = year % 100
d = b / 4
e = b % 4
f = (b + 8) / 25
g = (b - f + 1) / 3
h = (19 * a + b - d - g + 15) % 30
i = c / 4
k = c % 4
l = (32 + 2 * e + 2 * i - h - k) % 7
m = (a + 11 * h + 22 * l) / 451
month = (h + l - 7 * m + 114) / 31   // 3 = Maerz, 4 = April
day = ((h + l - 7 * m + 114) % 31) + 1

Ostersonntag = month/day
```

Variable Feiertage:
- Karfreitag = Ostern - 2
- Ostermontag = Ostern + 1
- Christi Himmelfahrt = Ostern + 39
- Pfingstmontag = Ostern + 50
- Fronleichnam = Ostern + 60
- Buss- und Bettag = Mittwoch vor dem 23. November (berechnen!)

---

## 6. Settings-Sync: Last-Write-Wins

Settings nutzen **Last-Write-Wins** (nicht Server-wins wie WorkSessions):

```
1. Lokale Settings laden
2. Server-Settings via GET /v1/settings laden
3. Vergleiche UpdatedAt:
   -> Lokal neuer? -> PUT /v1/settings (lokale Werte hochladen)
   -> Server neuer? -> Lokale Settings ueberschreiben
   -> Gleich? -> Nichts tun
4. Lokale Settings als synced markieren
```

**Wichtig**: Bei jeder lokalen Settings-Aenderung `updatedAt = now()` setzen!

**Bekannte Limitierung**: Settings Last-Write-Wins arbeitet auf Dokument-Ebene, nicht auf Feld-Ebene. Bei gleichzeitiger Aenderung auf zwei Geraeten gewinnt die letzte Aenderung komplett -- auch Felder die auf dem anderen Geraet nicht geaendert wurden, werden ueberschrieben. Fuer Phase 2 ist das akzeptabel, da Settings selten gleichzeitig auf mehreren Geraeten geaendert werden.

---

## 7. Kalender-Spezifika

### Wochenstart: MONTAG

Alle Kalender-Berechnungen und -Anzeigen muessen Montag als ersten Wochentag nutzen (deutscher Standard). NICHT Sonntag (US-Standard)!

**iOS:**
```swift
var calendar = Calendar.current
calendar.firstWeekday = 2  // 2 = Montag
```

**Android:**
```kotlin
// java.time.DayOfWeek.MONDAY ist bereits Montag
val firstDayOfMonth = YearMonth.of(year, month).atDay(1)
val dayOfWeek = firstDayOfMonth.dayOfWeek  // MONDAY..SUNDAY
val offset = dayOfWeek.value - 1  // 0 fuer Montag, 6 fuer Sonntag
```

### Arbeitstage aus Bitmask

Nicht hardcoded Mo-Fr! Arbeitstage kommen aus `UserSettings.workDays` Bitmask:
```
Bitmask: 1=Mo, 2=Di, 4=Mi, 8=Do, 16=Fr, 32=Sa, 64=So
Standard Mo-Fr = 1+2+4+8+16 = 31

Pruefung ob ein Wochentag Arbeitstag ist:
isWorkday(dayOfWeek) = (workDays & (1 << (dayOfWeek - 1))) != 0
// dayOfWeek: 1=Mo, 2=Di, ..., 7=So
```

---

## 8. Export-Spezifika

### CSV: Semikolon + Komma-Dezimal

Der deutsche Excel-Standard verwendet:
- **Semikolon** als Spalten-Trennzeichen (NICHT Komma!)
- **Komma** als Dezimal-Trennzeichen (8,00 statt 8.00)
- **UTF-8 mit BOM** (`\uFEFF` als erstes Byte, damit Excel Umlaute korrekt anzeigt)

### PDF: A4 Hochformat

- iOS: `UIGraphicsPDFRendererFormat` oder HTML -> WKWebView PDF
- Android: `PdfDocument` oder HTML -> WebView PrintAdapter
- Empfehlung: HTML-Ansatz auf beiden Plattformen (einfacher zu stylen, konsistentes Layout)

---

## 9. Definition of Done (Phase 2 Story)

Eine Phase-2-Story ist "Done" wenn:

- [ ] Alle Akzeptanzkriterien aus dem EPIC-Dokument erfuellt
- [ ] Phase-1-Features funktionieren weiterhin (Regressions-Check)
- [ ] Code kompiliert ohne Warnungen
- [ ] App laeuft auf Simulator/Emulator
- [ ] Neue Screens haben funktionierende SwiftUI Previews / Compose Previews
- [ ] Tests fuer ViewModel-Logik und Berechnungen (HolidayCalculator, Bitmask-Logik)
- [ ] Neue Dateien im richtigen Ordner
- [ ] Bestehende Dateien nur erweitert, nicht gebrochen
- [ ] Deutsche UI-Strings (keine englischen Labels in der UI)
- [ ] Feiertag-Berechnung auf Korrektheit geprueft (Stichprobe: 2-3 Bundeslaender)

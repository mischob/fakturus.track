# Tech-Spec: EPIC 08 -- Lokalisierung (DE + EN)

## Uebersicht

Alle hardcodierten UI-Strings durch Lokalisierungsmechanismen ersetzen. iOS: String Catalog (Localizable.xcstrings). Android: strings.xml + values-en/strings.xml. Deutsch ist Default, Englisch ist Fallback.

---

## S01: iOS String-Extraktion

### String Catalog (Xcode 15+)

Neues File: `FakturusTrack/Resources/Localizable.xcstrings`

Xcode 15+ String Catalogs ersetzen `.strings` + `.stringsdict`. Vorteile:
- Einzelne Datei fuer alle Sprachen
- Pluralisierung eingebaut
- Xcode zeigt fehlende Uebersetzungen

### String-Key Konventionen

```
{screen}_{element}_{detail}

Beispiele:
times_tab_title                  = "Zeiten"
times_timer_start                = "Starten"
times_timer_stop                 = "Stoppen"
times_timer_pause                = "Pause"
times_timer_resume               = "Weiter"
times_timer_finish               = "Fertig"
times_session_running            = "Laufende Sitzung"
times_session_stopped            = "Gestoppt"
times_session_idle               = "Bereit fuer den naechsten Eintrag"
times_history_entries %lld       = "%lld Eintraege"   (Pluralisierung!)

vacation_tab_title               = "Urlaub"
vacation_remaining               = "Resturlaub"
vacation_days_remaining %lld %lld = "%lld von %lld Urlaubstagen"
vacation_holiday                 = "Feiertag"
vacation_sick_day                = "Krankheitstag"

overview_tab_title               = "Gesamt"
overview_overtime                = "Ueberstunden"
overview_worked                  = "Gearbeitet"
overview_expected                = "Erwartet"
overview_export_pdf              = "PDF-Export"
overview_export_csv              = "CSV-Export"
overview_export_datev            = "DATEV-Export"

settings_tab_title               = "Einstellungen"
settings_work_hours              = "Wochenstunden"
settings_work_days               = "Arbeitstage"
settings_bundesland              = "Bundesland"
settings_appearance              = "Erscheinungsbild"
settings_appearance_system       = "System"
settings_appearance_light        = "Hell"
settings_appearance_dark         = "Dunkel"
settings_personal_number         = "Personalnummer"
settings_notifications           = "Benachrichtigungen"
settings_version                 = "Version"
settings_privacy                 = "Datenschutzerklaerung"
settings_imprint                 = "Impressum"
settings_licenses                = "Open-Source-Lizenzen"

error_network                    = "Keine Internetverbindung. Daten werden lokal gespeichert."
error_sync_failed                = "Synchronisation fehlgeschlagen. Bitte spaeter erneut versuchen."
error_token_expired              = "Sitzung abgelaufen. Bitte erneut anmelden."
error_data_load                  = "Daten konnten nicht geladen werden."
error_retry                      = "Erneut versuchen"

arbzg_6h_hint                    = "Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu."
arbzg_9h_hint                    = "Sie haben die regulaere Hoechstarbeitszeit von 8 Stunden ueberschritten."
arbzg_10h_hint                   = "Achtung: Die gesetzliche Hoechstarbeitszeit von 10 Stunden ist erreicht."
```

### Migration: Hardcodierte Strings ersetzen

```swift
// VORHER:
Text("Starten")
Text("Bereit fuer den naechsten Eintrag")
Text("\(count) Eintraege")

// NACHHER:
Text(String(localized: "times_timer_start"))
Text(String(localized: "times_session_idle"))
Text(String(localized: "times_history_entries \(count)"))
```

### Pluralisierung im String Catalog

```json
// In Localizable.xcstrings:
{
  "times_history_entries %lld" : {
    "localizations" : {
      "de" : {
        "variations" : {
          "plural" : {
            "one" : { "stringUnit" : { "value" : "%lld Eintrag" } },
            "other" : { "stringUnit" : { "value" : "%lld Eintraege" } }
          }
        }
      },
      "en" : {
        "variations" : {
          "plural" : {
            "one" : { "stringUnit" : { "value" : "%lld entry" } },
            "other" : { "stringUnit" : { "value" : "%lld entries" } }
          }
        }
      }
    }
  }
}
```

---

## S02: Android String-Extraktion

### values/strings.xml (Deutsch -- Default)

```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <!-- Tabs -->
    <string name="times_tab_title">Zeiten</string>
    <string name="vacation_tab_title">Urlaub</string>
    <string name="overview_tab_title">Gesamt</string>
    <string name="settings_tab_title">Einstellungen</string>

    <!-- Timer -->
    <string name="times_timer_start">Starten</string>
    <string name="times_timer_stop">Stoppen</string>
    <string name="times_timer_pause">Pause</string>
    <string name="times_timer_resume">Weiter</string>
    <string name="times_timer_finish">Fertig</string>
    <string name="times_session_running">Laufende Sitzung</string>
    <string name="times_session_stopped">Gestoppt</string>
    <string name="times_session_idle">Bereit fuer den naechsten Eintrag</string>

    <!-- Plurals -->
    <plurals name="times_history_entries">
        <item quantity="one">%d Eintrag</item>
        <item quantity="other">%d Eintraege</item>
    </plurals>

    <!-- Vacation -->
    <string name="vacation_remaining">Resturlaub</string>
    <string name="vacation_days_remaining">%1$d von %2$d Urlaubstagen</string>
    <string name="vacation_holiday">Feiertag</string>
    <string name="vacation_sick_day">Krankheitstag</string>

    <!-- Overview -->
    <string name="overview_overtime">Ueberstunden</string>
    <string name="overview_worked">Gearbeitet</string>
    <string name="overview_expected">Erwartet</string>
    <string name="overview_export_pdf">PDF-Export</string>
    <string name="overview_export_csv">CSV-Export</string>
    <string name="overview_export_datev">DATEV-Export</string>

    <!-- Settings -->
    <string name="settings_work_hours">Wochenstunden</string>
    <string name="settings_work_days">Arbeitstage</string>
    <string name="settings_bundesland">Bundesland</string>
    <string name="settings_appearance">Erscheinungsbild</string>
    <string name="settings_appearance_system">System</string>
    <string name="settings_appearance_light">Hell</string>
    <string name="settings_appearance_dark">Dunkel</string>
    <string name="settings_personal_number">Personalnummer</string>
    <string name="settings_notifications">Benachrichtigungen</string>
    <string name="settings_version">Version</string>
    <string name="settings_privacy">Datenschutzerklaerung</string>
    <string name="settings_imprint">Impressum</string>
    <string name="settings_licenses">Open-Source-Lizenzen</string>

    <!-- Errors -->
    <string name="error_network">Keine Internetverbindung. Daten werden lokal gespeichert.</string>
    <string name="error_sync_failed">Synchronisation fehlgeschlagen. Bitte spaeter erneut versuchen.</string>
    <string name="error_token_expired">Sitzung abgelaufen. Bitte erneut anmelden.</string>
    <string name="error_data_load">Daten konnten nicht geladen werden.</string>
    <string name="error_retry">Erneut versuchen</string>

    <!-- ArbZG -->
    <string name="arbzg_6h_hint">Erinnerung: Nach 6 Stunden Arbeit steht Ihnen eine Pause von mindestens 30 Minuten zu.</string>
    <string name="arbzg_9h_hint">Sie haben die regulaere Hoechstarbeitszeit von 8 Stunden ueberschritten.</string>
    <string name="arbzg_10h_hint">Achtung: Die gesetzliche Hoechstarbeitszeit von 10 Stunden ist erreicht.</string>

    <!-- Widget -->
    <string name="widget_name">Fakturus Timer</string>
    <string name="widget_description">Timer-Status und heutige Arbeitszeit</string>
    <string name="shortcut_start_timer">Timer starten</string>
    <string name="shortcut_start_timer_long">Arbeitstimer starten</string>
    <string name="shortcut_view_history">Letzte Eintraege</string>
</resources>
```

### values-en/strings.xml (Englisch)

```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <!-- Tabs -->
    <string name="times_tab_title">Times</string>
    <string name="vacation_tab_title">Vacation</string>
    <string name="overview_tab_title">Overview</string>
    <string name="settings_tab_title">Settings</string>

    <!-- Timer -->
    <string name="times_timer_start">Start</string>
    <string name="times_timer_stop">Stop</string>
    <string name="times_timer_pause">Pause</string>
    <string name="times_timer_resume">Resume</string>
    <string name="times_timer_finish">Done</string>
    <string name="times_session_running">Active session</string>
    <string name="times_session_stopped">Stopped</string>
    <string name="times_session_idle">Ready for next entry</string>

    <plurals name="times_history_entries">
        <item quantity="one">%d entry</item>
        <item quantity="other">%d entries</item>
    </plurals>

    <!-- ... analog fuer alle anderen Keys ... -->

    <!-- ArbZG (informelle Uebersetzung, nicht rechtsverbindlich) -->
    <string name="arbzg_6h_hint">Reminder: After 6 hours of work, you are entitled to a break of at least 30 minutes (German Working Hours Act).</string>
    <string name="arbzg_9h_hint">You have exceeded the regular maximum working time of 8 hours.</string>
    <string name="arbzg_10h_hint">Warning: The legal maximum working time of 10 hours has been reached.</string>
</resources>
```

### Migration in Compose

```kotlin
// VORHER:
Text("Starten")

// NACHHER:
Text(stringResource(R.string.times_timer_start))

// Mit Formatierung:
Text(stringResource(R.string.vacation_days_remaining, remaining, total))

// Plurals:
Text(pluralStringResource(R.plurals.times_history_entries, count, count))
```

---

## S03: Datum/Zeit-Formatierung

### iOS: Bestehende Date+Formatting.swift erweitern

```swift
// Date+Formatting.swift -- Locale-aware Formatierung
extension Date {
    /// Datum im lokalen Format: "29.03.2026" (DE) oder "Mar 29, 2026" (EN)
    var localizedDate: String {
        formatted(date: .numeric, time: .omitted)
    }

    /// Wochentag + Datum: "Fr 29.03." (DE) oder "Fri 03/29" (EN)
    var localizedShortDate: String {
        formatted(.dateTime.weekday(.abbreviated).day().month(.defaultDigits))
    }

    /// Monat + Jahr: "Maerz 2026" (DE) oder "March 2026" (EN)
    var localizedMonthYear: String {
        formatted(.dateTime.month(.wide).year())
    }

    /// Uhrzeit: "08:30" (24h DE) oder "8:30 AM" (12h EN, je nach Geraet)
    var localizedTime: String {
        formatted(date: .omitted, time: .shortened)
    }
}
```

### Android: DateFormatting.kt erweitern

```kotlin
// util/DateFormatting.kt -- Locale-aware
object DateFormatting {
    fun localizedDate(date: LocalDate): String =
        date.format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM)
            .withLocale(Locale.getDefault()))

    fun localizedTime(instant: Instant): String {
        val time = instant.atZone(ZoneId.systemDefault()).toLocalTime()
        return time.format(DateTimeFormatter.ofLocalizedTime(FormatStyle.SHORT)
            .withLocale(Locale.getDefault()))
    }

    fun localizedMonthYear(yearMonth: YearMonth): String =
        yearMonth.format(DateTimeFormatter.ofPattern("MMMM yyyy", Locale.getDefault()))
}
```

### WICHTIG: CSV/DATEV-Export bleibt IMMER Deutsch

```swift
// CSVExporter -- Feste Locale, NICHT Locale.current!
let dateFormatter = DateFormatter()
dateFormatter.locale = Locale(identifier: "de_DE")  // IMMER deutsch
dateFormatter.dateFormat = "dd.MM.yyyy"
```

```kotlin
// CSVExporter.kt -- Feste Locale
val formatter = DateTimeFormatter.ofPattern("dd.MM.yyyy")  // Kein Locale.getDefault()!
```

---

## Nicht uebersetzte Elemente (bewusst)

| Element | Grund |
|---------|-------|
| Bundesland-Namen | Rechtlich relevante deutsche Bezeichnungen |
| Feiertag-Namen | Deutsche Feiertage, kein Aequivalent |
| DATEV-Export Header | Datenformat, keine UI |
| CSV-Export Header | Datenformat fuer deutsche Steuerberater |

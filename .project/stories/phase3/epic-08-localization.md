# EPIC 08: Lokalisierung (DE + EN)

## Ziel

Alle UI-Strings sind in Lokalisierungsdateien ausgelagert. Deutsch ist die primaere Sprache, Englisch der Fallback. Die App waehlt automatisch die Sprache basierend auf den Geraeteeinstellungen. Datum/Zeit folgen dem deutschen Format (TT.MM.JJJJ, HH:MM). Die Lokalisierung ist Voraussetzung fuer den App Store (Apple empfiehlt lokalisierte Beschreibungen fuer den deutschen Markt).

## Abhaengigkeiten

- **E01 (UI Polish)**: Alle UI-Strings muessen final sein, bevor sie extrahiert werden
- **Keine Backend-Aenderungen**: Rein client-seitig

## Design-Entscheidung

**Deutsch zuerst, Englisch als Fallback**:
- Hauptzielmarkt ist Deutschland -- deutschsprachige UI hat Prioritaet
- Englisch als Fallback fuer internationale Nutzer (z.B. Expats in DE)
- KEINE weiteren Sprachen in V1 (kann spaeter ergaenzt werden)

---

## Stories

### P3-E08-S01: iOS String-Extraktion & Lokalisierung

**Als** Nutzer mit englischen Geraeteeinstellungen
**moechte ich** die App auf Englisch nutzen koennen,
**damit** ich die Zeiterfassung auch ohne Deutschkenntnisse bedienen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E01 (UI Polish abgeschlossen)
**Parallelisierbar mit**: P3-E08-S02, P3-E02-*, P3-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Alle hardcodierten deutschen Strings durch `String(localized:)` (iOS 16+) oder `NSLocalizedString` ersetzt
- [ ] `Localizable.xcstrings` (String Catalog, Xcode 15+) mit:
  - `de` (Deutsch): Primaere Sprache, alle Strings
  - `en` (Englisch): Vollstaendige Uebersetzung
- [ ] Mindestens folgende String-Kategorien lokalisiert:
  - Tab-Labels: "Zeiten", "Urlaub", "Gesamt", "Einstellungen"
  - Timer: "Starten", "Stoppen", "Pause", "Weiter", "Fertig"
  - History: Monatsname, Wochentag, "Eintraege", "Stunden"
  - Urlaub: "Resturlaub", "Urlaubstage", "Feiertag", "Krankheitstag"
  - Gesamt: "Ueberstunden", "Gearbeitet", "Erwartet"
  - Settings: Alle Labels und Beschreibungen
  - Fehler: Alle Fehlermeldungen
  - ArbZG-Hinweise (ACHTUNG: rechtliche Texte nur auf Deutsch, Englisch als informelle Uebersetzung)
- [ ] Pluralisierung korrekt: "1 Eintrag" vs "5 Eintraege" (Stringsdict / String Catalog Plural Rules)
- [ ] Given die Geraetesprache ist Englisch
  When die App geoeffnet wird
  Then erscheinen alle Texte auf Englisch
- [ ] Given die Geraetesprache ist Franzoesisch (nicht unterstuetzt)
  When die App geoeffnet wird
  Then erscheinen alle Texte auf Englisch (Fallback)

**Technische Hinweise**:
- Xcode 15+: String Catalogs (`.xcstrings`) statt `.strings` + `.stringsdict`
- `String(localized: "timer_start", defaultValue: "Starten")` fuer neue Strings
- `String(localized: "entries_count \(count)", defaultValue: "\(count) Eintraege")` fuer Pluralisierung
- ACHTUNG: Keine `Localizable.strings` + `Localizable.stringsdict` mischen mit String Catalogs
- Bundesland-Namen muessen NICHT uebersetzt werden (bleiben deutsch, da rechtlich relevant)

---

### P3-E08-S02: Android String-Extraktion & Lokalisierung

**Als** Nutzer mit englischen Geraeteeinstellungen
**moechte ich** die App auf Englisch nutzen koennen,
**damit** ich die Zeiterfassung auch ohne Deutschkenntnisse bedienen kann.

**Plattform**: Android
**Abhaengigkeiten**: P3-E01 (UI Polish abgeschlossen)
**Parallelisierbar mit**: P3-E08-S01, P3-E02-*, P3-E05-*, P3-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Alle hardcodierten Strings durch `stringResource(R.string.xxx)` ersetzt
- [ ] `res/values/strings.xml` (Deutsch, Default)
- [ ] `res/values-en/strings.xml` (Englisch)
- [ ] Gleiche String-Kategorien wie iOS lokalisiert
- [ ] Pluralisierung korrekt via `<plurals>` in `strings.xml`
- [ ] Given die Geraetesprache ist Englisch
  When die App geoeffnet wird
  Then erscheinen alle Texte auf Englisch

**Technische Hinweise**:
- Default `values/strings.xml` = Deutsch (da Hauptzielmarkt)
- `values-en/strings.xml` = Englisch
- `<plurals name="entries_count"><item quantity="one">%d Eintrag</item><item quantity="other">%d Eintraege</item></plurals>`
- `stringResource(R.string.xxx, arg1, arg2)` fuer formatierte Strings
- Compose: `stringResource()` in Composables, NICHT `context.getString()` (wegen Recomposition)

---

### P3-E08-S03: Datum/Zeit-Formatierung (Beide Plattformen)

**Als** Nutzer
**moechte ich** dass Datum und Uhrzeit im fuer mich gewohnten Format angezeigt werden,
**damit** ich die Informationen schnell erfassen kann.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P3-E08-S01/S02 (Lokalisierung-Grundlage)
**Parallelisierbar mit**: P3-E06-*, P3-E07-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Deutsches Format:
  - Datum: "29.03.2026" (TT.MM.JJJJ)
  - Wochentag: "Fr 29.03." oder "Freitag, 29. Maerz 2026"
  - Uhrzeit: "08:30" (24-Stunden-Format)
  - Monat: "Maerz 2026"
  - Dauer: "8:00h" oder "8 Stunden 0 Minuten"
- [ ] Englisches Format:
  - Datum: "03/29/2026" oder "Mar 29, 2026"
  - Uhrzeit: "8:30 AM" oder "08:30" (je nach Geraeteeinstellung)
  - Monat: "March 2026"
- [ ] ACHTUNG: CSV-Export bleibt IMMER im deutschen Format (Semikolon, Komma-Dezimal) -- das ist ein Datenformat, keine UI-Anzeige
- [ ] Given die Geraetesprache ist Deutsch
  When ein Datum angezeigt wird
  Then ist das Format TT.MM.JJJJ

**Technische Hinweise**:
- iOS: `Date.FormatStyle()` oder `DateFormatter` mit `Locale.current`
- Android: `DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM).withLocale(Locale.getDefault())`
- NICHT hardcoden: `String(format: "%02d.%02d.%04d")` -- nutze Formatter!
- Zentrale `DateFormatHelper` Klasse fuer konsistente Formatierung

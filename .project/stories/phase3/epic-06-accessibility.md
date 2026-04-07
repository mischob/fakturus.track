# EPIC 06: Barrierefreiheit (Accessibility)

## Ziel

Die App ist fuer Menschen mit Behinderungen vollstaendig nutzbar. VoiceOver (iOS) und TalkBack (Android) koennen die komplette App bedienen. Dynamic Type wird unterstuetzt. Kontraste erfuellen WCAG AA. Die Barrierefreiheit ist nicht nur eine "nette" Ergaenzung -- sie ist Store-Anforderung (Apple fordert VoiceOver-Support, Google empfiehlt TalkBack).

## Abhaengigkeiten

- **E01 (UI/UX Polish)**: Die finale UI muss stehen, damit das Audit auf der finalen Version laeuft
- **Keine Backend-Aenderungen**: Rein client-seitig

---

## Stories

### P3-E06-S01: iOS VoiceOver Audit & Fixes

**Als** sehbehinderter Nutzer
**moechte ich** die App vollstaendig mit VoiceOver bedienen koennen,
**damit** ich meine Arbeitszeit wie alle anderen erfassen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E01 (UI Polish abgeschlossen)
**Parallelisierbar mit**: P3-E06-S02, P3-E02-*, P3-E03-*, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Zeiten-Tab**:
  - Timer-Anzeige wird als "Laufender Timer, 3 Stunden 42 Minuten" vorgelesen
  - Buttons haben klare Labels: "Timer starten", "Timer stoppen", "Pause", "Fertig"
  - History-Eintraege: "Freitag 28. Maerz, 8 Uhr 30 bis 17 Uhr, 8 Stunden Nettoarbeitszeit"
  - Monatsgruppen: "Maerz 2026, 11 Eintraege, 38 Stunden 45 Minuten"
- [ ] **Urlaub-Tab**:
  - Kalender-Tage haben Labels: "15. Juli, Donnerstag, Arbeitstag" / "15. Juli, Urlaubstag"
  - Feiertage: "1. Mai, Maifeiertag, nicht verfuegbar"
  - Krankheitstage: "3. Maerz, Krankheitstag"
  - Resturlaub: "25 von 30 Urlaubstagen verbleibend"
- [ ] **Gesamt-Tab**:
  - Summary Cards: "Ueberstunden plus 12 Stunden 30 Minuten"
  - Monatstabelle: Zeile fuer Zeile vorlesbar
  - Export-Buttons mit klaren Labels
- [ ] **Einstellungen**:
  - Arbeitstage-Toggles: "Montag, aktiviert" / "Samstag, deaktiviert"
  - Bundesland-Picker ist navigierbar
- [ ] **Navigation**: Tab-Wechsel mit VoiceOver funktioniert
- [ ] **Accessibility Traits**: Buttons, Headers, Adjustable-Elemente korrekt markiert
- [ ] Given VoiceOver ist aktiviert
  When der Nutzer durch die App navigiert
  Then werden alle interaktiven Elemente korrekt vorgelesen und sind bedienbar

**Technische Hinweise**:
- `.accessibilityLabel()` fuer alle Custom-Views
- `.accessibilityHint()` fuer nicht-offensichtliche Aktionen ("Doppeltippen zum Starten")
- `.accessibilityValue()` fuer veraenderliche Werte (Timer, Zaehler)
- `.accessibilityTraits(.isButton/.isHeader/.updatesFrequently)`
- Timer: `.accessibilityAddTraits(.updatesFrequently)` damit VoiceOver nicht bei jedem Tick vorliest
- `.accessibilityElement(children: .combine)` fuer zusammengehoerige Elemente
- Xcode Accessibility Inspector fuer systematisches Audit

---

### P3-E06-S02: Android TalkBack Audit & Fixes

**Als** sehbehinderter Nutzer
**moechte ich** die App vollstaendig mit TalkBack bedienen koennen,
**damit** ich meine Arbeitszeit wie alle anderen erfassen kann.

**Plattform**: Android
**Abhaengigkeiten**: P3-E01 (UI Polish abgeschlossen)
**Parallelisierbar mit**: P3-E06-S01, P3-E02-*, P3-E05-*, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Gleiche inhaltliche Anforderungen wie iOS VoiceOver (alle Screens, alle Elemente)
- [ ] `contentDescription` fuer alle Images und Icons
- [ ] Semantics korrekt gesetzt in Compose
- [ ] Navigation mit TalkBack funktioniert (Swipe links/rechts durch Elemente)
- [ ] Given TalkBack ist aktiviert
  When der Nutzer durch die App navigiert
  Then werden alle interaktiven Elemente korrekt vorgelesen und sind bedienbar

**Technische Hinweise**:
- `Modifier.semantics { contentDescription = "..." }` in Compose
- `Modifier.semantics(mergeDescendants = true) { ... }` fuer zusammengehoerige Elemente
- `Modifier.clearAndSetSemantics { ... }` fuer Custom-Descriptions
- `Role.Button`, `Role.Checkbox`, `Role.Tab` fuer korrekte Semantik
- Android Accessibility Scanner fuer systematisches Audit

---

### P3-E06-S03: iOS Dynamic Type

**Als** Nutzer mit eingeschraenktem Sehvermoegen
**moechte ich** die Schriftgroesse der App an meine Systemeinstellung anpassen koennen,
**damit** ich alle Texte gut lesen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E01 (UI Polish abgeschlossen)
**Parallelisierbar mit**: P3-E06-S04, P3-E02-*, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Alle Texte nutzen Dynamic Type Stile (`.font(.body)`, `.font(.headline)`, etc.)
- [ ] Kein hardcodierter Font-Size (`.font(.system(size: 16))` vermeiden)
- [ ] Layouts passen sich an groessere Schrift an (kein Text-Abschneiden, kein Ueberlauf)
- [ ] Timer-Anzeige: Skaliert mit, aber hat eine sinnvolle Maximalgrösse
- [ ] Kalender: Tageszahlen skalieren mit, Grid passt sich an
- [ ] History-Zeilen: Werden hoeher bei groesserer Schrift, kein Overflow
- [ ] Given die System-Schriftgroesse ist auf "Extra Gross" gestellt
  When die App geoeffnet wird
  Then sind alle Texte vergroessert und das Layout funktioniert ohne Ueberlauf
- [ ] Given die System-Schriftgroesse ist auf "Extra Klein" gestellt
  When die App geoeffnet wird
  Then funktioniert das Layout ebenfalls korrekt

**Technische Hinweise**:
- SwiftUI `.font(.body)` etc. unterstuetzen Dynamic Type automatisch
- Problem: Custom-Layouts mit festen Hoehen brechen bei grosser Schrift
- `@ScaledMetric` Property Wrapper fuer Abstande und Icon-Groessen
- `.minimumScaleFactor(0.7)` als Fallback wo noetig (nicht ideal, nur als Notloesung)
- Testen mit Xcode > Environment Overrides > Dynamic Type

---

### P3-E06-S04: Android Schriftgroessen-Anpassung

**Als** Nutzer mit eingeschraenktem Sehvermoegen
**moechte ich** die Schriftgroesse der App an meine Systemeinstellung anpassen koennen,
**damit** ich alle Texte gut lesen kann.

**Plattform**: Android
**Abhaengigkeiten**: P3-E01 (UI Polish abgeschlossen)
**Parallelisierbar mit**: P3-E06-S03, P3-E02-*, P3-E05-*, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Alle Texte nutzen `sp` (skalierbare Pixel), nicht `dp` fuer Schriftgroessen
- [ ] Material 3 Typography-Stile korrekt verwendet
- [ ] Layouts passen sich an (kein Ueberlauf bei 200% Schriftgroesse)
- [ ] Given die System-Schriftgroesse ist auf "Extra Gross" gestellt
  When die App geoeffnet wird
  Then sind alle Texte vergroessert und das Layout funktioniert

**Technische Hinweise**:
- Compose `MaterialTheme.typography.bodyLarge` etc. nutzen sp automatisch
- Problem: `fontSize = 14.dp` statt `14.sp` ist ein haeufiger Fehler
- `LocalDensity.current.fontScale` fuer bedingte Layout-Anpassungen
- Testen mit: Einstellungen > Anzeige > Schriftgroesse > Maximum

---

### P3-E06-S05: Kontrast-Pruefung (Beide Plattformen)

**Als** Nutzer mit Sehschwaeche
**moechte ich** alle Texte und Elemente mit ausreichendem Kontrast sehen koennen,
**damit** ich die App ohne Einschraenkungen nutzen kann.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P3-E01 (Dark Mode muss fertig sein)
**Parallelisierbar mit**: P3-E06-S01/S02, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Alle Text-Hintergrund-Kombinationen erfuellen WCAG AA (min. 4.5:1 fuer normalen Text, 3:1 fuer grossen Text)
- [ ] Farbkodierung ist NICHT die einzige Information (Urlaub: Cyan + "U" Label, Krank: Rot + "K" Label)
- [ ] Light Mode und Dark Mode erfuellen beide WCAG AA
- [ ] Timer-Farben (gruen/rot fuer Ueberstunden) haben ausreichend Kontrast
- [ ] Given alle Farb-Kombinationen werden mit einem Kontrast-Tool geprueft
  When eine Kombination unter 4.5:1 liegt
  Then wird sie korrigiert

**Technische Hinweise**:
- iOS: Xcode Accessibility Inspector > Color Contrast
- Android: Android Accessibility Scanner
- Online-Tool: WebAIM Contrast Checker
- Haeufige Problemstellen: Grau-auf-Weiss, Hellgruen-auf-Weiss, Cyan-auf-Weiss

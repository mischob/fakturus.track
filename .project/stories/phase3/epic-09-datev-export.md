# EPIC 09: DATEV-Export

## Ziel

Export der Arbeitszeitdaten im DATEV-kompatiblen Format fuer die Lohn- und Gehaltsabrechnung. Dies ist ein PRO-Feature und ein starkes Differenzierungsmerkmal -- laut Marktanalyse bieten nur Crewmeister und ZEP aktuell DATEV-Export an. Steuerberater koennen die exportierten Daten direkt in DATEV Lohn & Gehalt importieren.

> **Beta-Hinweis**: Der DATEV-Export ist als **Beta-Feature** markiert. Das Export-Format muss mit einem Steuerberater validiert werden, bevor es als stabil gilt. In der UI wird ein "(Beta)"-Badge neben dem DATEV-Export-Button angezeigt.

## Abhaengigkeiten

- **Phase 2 E06**: Export-Infrastruktur (PDFReportGenerator, CSVExporter, Share-Sheet) muss stehen
- **E10 (App-Settings)**: Personalnummer-Feld in den Settings (wird fuer DATEV-Export benoetigt). Feature-Gating (PRO) wird in Phase 4 implementiert.

## Design-Entscheidung

**Client-seitige DATEV-Generierung** (wie PDF/CSV):
- Kein Backend noetig
- Offline verfuegbar
- Gleiche Share-Sheet-Integration wie bestehende Exports

**DATEV Lodas Format** (Lohn & Gehalt):
- ASCII-basiert, feste Feldbreiten oder CSV-artig
- Personalnummer, Datum, Stunden, Lohnart
- Abstimmung mit DATEV-Dokumentation noetig

---

## Stories

### P3-E09-S01: DATEV-Format Recherche & Spezifikation

**Als** Product Owner
**moechte ich** das exakte DATEV-Import-Format fuer Arbeitszeitdaten spezifizieren,
**damit** der Export korrekt in DATEV Lohn & Gehalt importiert werden kann.

**Plattform**: Beide (Spezifikation, keine Implementierung)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle anderen Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] DATEV Lodas Importformat dokumentiert:
  - Dateiformat: ASCII, Trennzeichen-basiert
  - Pflichtfelder: Personalnummer, Datum, Arbeitsstunden, Lohnart
  - Optionale Felder: Pausen, Ueberstunden, Abwesenheitstyp
- [ ] Mapping definiert:
  - WorkSession -> DATEV-Zeile (Datum, Start, Ende, Stunden)
  - VacationDay -> DATEV-Zeile (Lohnart "Urlaub")
  - SickDay -> DATEV-Zeile (Lohnart "Krank")
- [ ] Beispiel-Datei erstellt und von einem Steuerberater oder DATEV-Kenner validiert
- [ ] Dateiname-Konvention: `DATEV_Lohn_{YYYY}-{MM}.csv`

**Technische Hinweise**:
- DATEV Lodas Importformat: Siehe DATEV Hilfe-Center / Dokumentation
- **Lohnarten sind konfigurierbar** (nicht hardcoded). Default-Werte: 200 (Gehalt), 400 (Urlaub), 500 (Krankheit). Der Nutzer kann diese in den DATEV-Einstellungen anpassen, da Lohnarten je nach Steuerberater/Unternehmen variieren.
- Personalnummer: Muss vom Nutzer in Settings konfigurierbar sein (neues Feld)
- Alternativ: DATEV-ASCII-Format mit festen Feldbreiten (aelter, aber weit verbreitet)

---

### P3-E09-S02: iOS DATEV-Export Generator

**Als** Nutzer mit PRO-Abo
**moechte ich** meine Arbeitszeitdaten im DATEV-kompatiblen Format exportieren koennen,
**damit** mein Steuerberater die Daten direkt in DATEV importieren kann.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E09-S01 (Spezifikation), Phase 2 E06 (Export-Infrastruktur)
**Parallelisierbar mit**: P3-E09-S03 (Android), P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `DATEVExporter.swift` in `Services/Export/`
- [ ] Methode: `generateDATEVExport(month: Int, year: Int, sessions: [WorkSession], vacationDays: [VacationDay], sickDays: [SickDay], personalNumber: String) -> String`
- [ ] Ausgabe entspricht der spezifizierten DATEV-Formatierung
- [ ] Jede WorkSession wird als eigene Zeile exportiert (Datum, Stunden, Lohnart)
- [ ] Urlaubstage und Krankheitstage als separate Zeilen mit entsprechender Lohnart
- [ ] Given Maerz 2026 mit 20 Arbeitstagen, 2 Urlaubstagen, 1 Krankheitstag
  When der DATEV-Export generiert wird
  Then enthaelt die Datei 23 Zeilen (20 Arbeit + 2 Urlaub + 1 Krank)
  And das Format ist DATEV-kompatibel
- [ ] Personalnummer aus Settings (neues optionales Feld)

**Technische Hinweise**:
- Gleiche Architektur wie CSVExporter: StringBuilder + Temp-File + Share-Sheet
- Encoding: ASCII oder UTF-8 (je nach DATEV-Anforderung)
- Dezimalformat: Punkt als Dezimaltrennzeichen (DATEV-Standard, NICHT Komma!)

---

### P3-E09-S03: Android DATEV-Export Generator

**Als** Nutzer mit PRO-Abo
**moechte ich** meine Arbeitszeitdaten im DATEV-kompatiblen Format exportieren koennen,
**damit** mein Steuerberater die Daten direkt importieren kann.

**Plattform**: Android
**Abhaengigkeiten**: P3-E09-S01 (Spezifikation), Phase 2 E06 (Export-Infrastruktur)
**Parallelisierbar mit**: P3-E09-S02 (iOS), P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `DATEVExporter.kt` in `services/export/`
- [ ] Gleiche Funktionalitaet und gleiches Format wie iOS
- [ ] Given die gleichen Testdaten wie bei iOS
  When der DATEV-Export generiert wird
  Then ist das Ergebnis identisch

**Technische Hinweise**:
- Gleiche Architektur wie Android CSVExporter
- `StringBuilder` + FileProvider fuer Share-Intent

---

### P3-E09-S04: DATEV-Export UI-Integration (Beide Plattformen)

**Als** Nutzer
**moechte ich** den DATEV-Export im Gesamt-Tab neben PDF und CSV finden,
**damit** ich alle Export-Formate an einer Stelle habe.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P3-E09-S02/S03 (Generatoren), P3-E10 (App-Settings fuer Personalnummer)
**Parallelisierbar mit**: P3-E06-*, P3-E08-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Neuer "DATEV-Export" Button im Gesamt-Tab Export-Sektion (unterhalb von PDF und CSV)
- [ ] Button zeigt "PRO"-Badge (Feature-Gating wird in Phase 4 implementiert, in Phase 3 ist der Button fuer alle sichtbar)
- [ ] Tap oeffnet Monatsauswahl und generiert DATEV-Datei
- [ ] Share-Sheet oeffnet sich mit generierter Datei
- [ ] OverviewViewModel um `generateDATEVExport()` Methode erweitert
- [ ] Personalnummer aus Settings wird uebergeben (leerer String wenn nicht gesetzt)
- [ ] Given der Nutzer tippt auf "DATEV-Export"
  When Maerz 2026 ausgewaehlt wird
  Then wird die DATEV-Datei generiert und das Share-Sheet oeffnet sich

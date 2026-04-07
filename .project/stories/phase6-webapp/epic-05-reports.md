# EPIC 05: Reports & Export

## Ziel

Ueberstunden-Dashboard mit Jahresuebersicht, monatlicher Aufschluesselung und umfangreichen Export-Optionen. Desktop-optimiert: alle 12 Monate auf einen Blick, Export-Optionen direkt erreichbar.

---

## Stories

### S01: Summary Cards
**Als** Benutzer **moechte ich** auf der Reports-Seite eine Zusammenfassung meiner Kennzahlen sehen, **damit** ich den Ueberblick behalte.

**Akzeptanzkriterien:**
- [ ] 4 Summary Cards nebeneinander (responsive: 2x2 auf Tablet)
- [ ] Ueberstunden: Wert farbkodiert (gruen/rot), "Gesamt {Jahr}"
- [ ] Urlaub: "X von Y Tagen", Resturlaub
- [ ] Feiertage: Anzahl im aktuellen Jahr (basierend auf Bundesland)
- [ ] Krankheitstage: Anzahl im aktuellen Jahr
- [ ] Daten via OvertimeCalculation + VacationDays API

**Aufwand:** S

---

### S02: Jahresnavigation
**Als** Benutzer **moechte ich** zwischen Jahren wechseln, **damit** ich auch vergangene Zeitraeume auswerten kann.

**Akzeptanzkriterien:**
- [ ] Jahres-Selector: "<< 2025 | 2026 | 2027 >>"
- [ ] Vor-/Zurueck-Pfeile
- [ ] Dropdown alternativ fuer groessere Spruenge
- [ ] Alle Daten auf der Seite aktualisieren sich beim Jahreswechsel
- [ ] Default: Aktuelles Jahr

**Aufwand:** S

---

### S03: Monatstabelle
**Als** Benutzer **moechte ich** eine monatliche Aufschluesselung meiner Arbeitszeit sehen, **damit** ich Trends und Abweichungen erkennen kann.

**Akzeptanzkriterien:**
- [ ] Tabelle mit Spalten: Monat, Arbeitstage, Gearbeitet, Erwartet, Differenz
- [ ] Alle 12 Monate angezeigt (auch zukuenftige als "--")
- [ ] Differenz farbkodiert: Gruen positiv, Rot negativ
- [ ] Laufender Monat als "(lfd.)" markiert
- [ ] Footer-Zeile: Gesamtsumme ueber alle Monate
- [ ] Zeiten im Format "HHH:MM" (z.B. "171:15h")
- [ ] Daten via OvertimeCalculation API

**Aufwand:** M

---

### S04: PDF Monatsreport
**Als** Benutzer **moechte ich** einen PDF-Report fuer einen Monat herunterladen, **damit** ich einen Arbeitszeitnachweis fuer meinen Arbeitgeber oder Behoerden habe.

**Akzeptanzkriterien:**
- [ ] Monats-Selector im Export-Bereich
- [ ] Button "PDF Monatsreport"
- [ ] PDF enthaelt:
  - Header: "fakturus.track -- Arbeitszeitnachweis"
  - Monat + Jahr
  - Name des Benutzers (aus UserSettings oder Auth)
  - Personalnummer (falls vorhanden)
  - Tabelle: Datum, Wochentag, Start, Ende, Pause, Netto
  - Urlaubs-/Krankheitstage markiert
  - Footer: Soll, Ist, Differenz, Urlaubstage, Krankheitstage
- [ ] PDF wird server-seitig generiert (QuestPDF o.ae.)
- [ ] Download als `arbeitszeitnachweis-2026-03.pdf`

**Aufwand:** L

---

### S05: CSV-Export (Monat/Quartal/Jahr)
**Als** Benutzer **moechte ich** meine Daten als CSV exportieren, **damit** ich sie in Excel oder anderen Tools weiterverarbeiten kann.

**Akzeptanzkriterien:**
- [ ] Zeitraum-Selector: Monat / Quartal / Jahr
- [ ] Button "CSV Export"
- [ ] CSV-Format: Datum;Start;Ende;Pause;Netto;Typ (Arbeit/Urlaub/Krank/Feiertag)
- [ ] Semikolon als Trennzeichen (deutsch-freundlich)
- [ ] UTF-8 BOM fuer Excel-Kompatibilitaet
- [ ] Download als `zeiterfassung-2026-03.csv` / `zeiterfassung-2026-Q1.csv` / `zeiterfassung-2026.csv`

**Aufwand:** M

---

### S06: DATEV-Export
**Als** Benutzer **moechte ich** meine Daten im DATEV-Format exportieren, **damit** mein Steuerberater die Daten direkt importieren kann.

**Akzeptanzkriterien:**
- [ ] Button "DATEV Export" (nur Web-App, nicht in Mobile)
- [ ] Monatsbezogener Export
- [ ] DATEV-Lohn-Format (CSV mit spezifischen Spalten):
  - Personalnummer
  - Datum
  - Arbeitsstunden
  - Ueberstunden
  - Urlaubstage
  - Krankheitstage
- [ ] Personalnummer aus UserSettings
- [ ] Hinweis: "DATEV-Export erfordert eine Personalnummer" (falls leer, Link zu Einstellungen)

**Aufwand:** M

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Summary Cards | S | E02 |
| S02 Jahresnavigation | S | S01 |
| S03 Monatstabelle | M | S01 |
| S04 PDF Monatsreport | L | E01-S04 |
| S05 CSV-Export | M | E01-S04 |
| S06 DATEV-Export | M | E01-S04, E06-S04 (Personalnr.) |

**Gesamt: ca. 1.5 Wochen**

# EPIC 03: Zeiterfassung (Timer + History)

## Ziel

Vollstaendige Zeiterfassungs-Funktionalitaet: Live-Timer, History-Tabelle, Session-Bearbeitung und manuelle Nacherfassung. Desktop-optimiert mit breiten Tabellen und Side-Panel-Editing.

---

## Stories

### S01: Dashboard Timer-Widget
**Als** Benutzer **moechte ich** auf dem Dashboard einen Timer starten und stoppen, **damit** ich meine Arbeitszeit mit einem Klick erfassen kann.

**Akzeptanzkriterien:**
- [ ] Timer-Card auf dem Dashboard (linke Haelfte, 2-Column Grid)
- [ ] Idle-State: "Bereit fuer den naechsten Eintrag" + grosser "Starten"-Button
- [ ] Running-State:
  - Gruener pulsierender Punkt + "Laufende Sitzung"
  - Grosser Timer (48px, monospaced, sekundengenau via SignalR/Blazor Timer)
  - Startzeit + Datum angezeigt
  - Buttons: Pause / Stop / Fertig
- [ ] Paused-State:
  - Violetter Indikator + "Pausiert"
  - Pause-Dauer wird angezeigt
  - Buttons: Weiter / Fertig
- [ ] Stopped-State:
  - Oranger Indikator + "Gestoppt"
  - Start-/Endzeit editierbar (Inline)
  - Buttons: Fertig / Verwerfen
- [ ] Timer-Aktualisierung jede Sekunde (Blazor Server: kein JS noetig)
- [ ] **KRITISCH -- Circuit-Disconnect-Sicherheit:**
  - [ ] Aktive Timer-Session wird SOFORT beim Start ans Backend persistiert (POST WorkSession mit startTime, ohne stopTime)
  - [ ] Bei Browser-Neuladen, Tab-Wechsel oder Circuit-Reconnect: Aktive Session wird vom Backend geladen und Timer fortgesetzt
  - [ ] Bei Circuit-Disconnect (Browser geschlossen, Netzwerk weg): Timer-Daten sind sicher im Backend, keine Daten gehen verloren
  - [ ] Der TimerService ist nur ein View-Concern (berechnet verstrichene Zeit aus Backend-StartTime), NICHT die Source of Truth
- [ ] Keyboard Shortcut: `Space` zum Starten/Stoppen (wenn Dashboard fokussiert)

**Aufwand:** L

---

### S02: Dashboard Tages-Stats
**Als** Benutzer **moechte ich** neben dem Timer meine heutigen Statistiken sehen, **damit** ich weiss wie viel ich heute schon gearbeitet habe.

**Akzeptanzkriterien:**
- [ ] Stats-Card auf dem Dashboard (rechte Haelfte)
- [ ] Angezeigt:
  - Startzeit (erste Session heute)
  - Pause (Gesamtpause heute)
  - Netto-Arbeitszeit (bisherige Gesamtdauer)
  - Soll-Stunden (basierend auf UserSettings)
  - Fehlende Stunden (Soll - Ist)
- [ ] ArbZG-Warnbanner innerhalb der Card:
  - Ab 6h ohne 30min Pause: "Pause empfohlen (ArbZG)"
  - Ab 9h ohne 45min Pause: "Mindestpause 45 min (ArbZG)"
  - Ab 10h: "Tageshöchstarbeitszeit erreicht (ArbZG)"
- [ ] Warnbanner: Gelber Hintergrund, dismiss-Button

**Aufwand:** M

---

### S03: Dashboard Summary Cards
**Als** Benutzer **moechte ich** auf dem Dashboard meine Ueberstunden und meinen Resturlaub sehen, **damit** ich den Ueberblick behalte.

**Akzeptanzkriterien:**
- [ ] 2 Summary Cards unter Timer/Stats (2-Column Grid)
- [ ] Ueberstunden-Card:
  - Wert farbkodiert (gruen positiv, rot negativ)
  - Aktueller Monat als Untertitel
- [ ] Urlaub-Card:
  - "X von Y Tagen" + Fortschrittsbalken
  - Resturlaub prominent
- [ ] Daten via OvertimeCalculation API + VacationDays API

**Aufwand:** S

---

### S04: Dashboard letzte Eintraege
**Als** Benutzer **moechte ich** auf dem Dashboard meine letzten Eintraege sehen, **damit** ich schnell pruefen kann ob alles stimmt.

**Akzeptanzkriterien:**
- [ ] Liste der letzten 5 Sessions unter den Summary Cards
- [ ] Pro Zeile: Datum, Start-Ende, Pause, Netto, Sync-Status
- [ ] Klick auf Zeile oeffnet Session-Edit (Side-Panel oder Navigation zu /time)
- [ ] "Alle anzeigen"-Link navigiert zu /time
- [ ] Leerer Zustand: "Noch keine Eintraege. Starten Sie Ihren ersten Timer."

**Aufwand:** S

---

### S05: Zeiten-Seite -- History-Tabelle
**Als** Benutzer **moechte ich** alle meine Zeiteintraege in einer uebersichtlichen Tabelle sehen, **damit** ich meine Arbeitszeiten pruefen und korrigieren kann.

**Akzeptanzkriterien:**
- [ ] Tabelle mit Spalten: Datum (Tag + Wochentag), Start, Ende, Pause, Netto, Sync-Status
- [ ] Gruppiert nach Monat (auf-/zuklappbar)
- [ ] Aktueller Monat standardmaessig aufgeklappt
- [ ] Monats-Header: Monatsname, Anzahl Eintraege, Gesamtdauer
- [ ] Monats-Footer: Soll-Stunden, Ist-Stunden, Differenz (farbkodiert)
- [ ] Aeltere Monate zunaechst zugeklappt (klickbar zum Aufklappen)
- [ ] Sortierung: absteigend nach Datum (neuste oben)
- [ ] Zeilenhover: Leichter Hintergrund
- [ ] Klick auf Zeile: Oeffnet Session-Edit Side-Panel

**Aufwand:** L

---

### S06: Session-Edit Side-Panel
**Als** Benutzer **moechte ich** eine Session bearbeiten, **damit** ich fehlerhafte Eintraege korrigieren kann.

**Akzeptanzkriterien:**
- [ ] Side-Panel (400px, rechts) oeffnet sich bei Klick auf Session
- [ ] Felder:
  - Datum (Date-Picker)
  - Startzeit (Time-Picker)
  - Endzeit (Time-Picker)
  - Pause in Minuten (Zahlenfeld)
- [ ] Berechnete Felder (read-only):
  - Brutto-Dauer
  - Netto-Dauer
- [ ] Validierung:
  - Ende > Start
  - Dauer <= 24h
  - Pause >= 0
  - Datum nicht in der Zukunft
- [ ] Buttons: Speichern (Primary), Loeschen (Danger), Abbrechen
- [ ] Loeschen: Bestaetigung-Dialog ("Eintrag wirklich loeschen?")
- [ ] Escape-Taste schliesst Panel
- [ ] Aenderungen werden sofort an API gesendet

**Aufwand:** M

---

### S07: Manuelle Nacherfassung
**Als** Benutzer **moechte ich** vergessene Arbeitstage manuell nachtragen, **damit** meine Zeiterfassung vollstaendig ist.

**Akzeptanzkriterien:**
- [ ] Button "+ Nacherfassung" im Zeiten-Header
- [ ] Oeffnet Side-Panel mit leeren Feldern (gleiches Panel wie S06)
- [ ] Datum-Default: Heute
- [ ] Startzeit-Default: Leer
- [ ] Endzeit-Default: Leer
- [ ] Pause-Default: 30 Minuten
- [ ] Keyboard Shortcut: `Ctrl+N`
- [ ] Nach Speichern: Session erscheint in der Tabelle, Panel schliesst

**Aufwand:** S

---

### S08: Session loeschen (Tabelle)
**Als** Benutzer **moechte ich** einen Eintrag direkt aus der Tabelle loeschen, **damit** ich fehlerhafte Eintraege schnell entfernen kann.

**Akzeptanzkriterien:**
- [ ] Loeschen-Icon (Papierkorb) am Ende jeder Tabellenzeile
- [ ] Klick auf Icon: Bestaetigung-Dialog
- [ ] Nach Loeschen: Zeile verschwindet mit Animation
- [ ] Undo-Toast fuer 5 Sekunden ("Eintrag geloescht. [Rueckgaengig]")
- [ ] Rueckgaengig stellt Session wieder her

**Aufwand:** S

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Timer-Widget | L | E02, E01-S04 |
| S02 Tages-Stats | M | S01 |
| S03 Summary Cards | S | E01-S04 |
| S04 Letzte Eintraege | S | E01-S04 |
| S05 History-Tabelle | L | E02, E01-S04 |
| S06 Session-Edit Panel | M | S05 |
| S07 Nacherfassung | S | S06 |
| S08 Session loeschen | S | S05 |

**Gesamt: ca. 2 Wochen**

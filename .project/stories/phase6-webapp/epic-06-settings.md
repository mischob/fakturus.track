# EPIC 06: Einstellungen & Account-Management

## Ziel

Vollstaendige Einstellungsseite mit Multi-Column-Layout, Profilverwaltung, Arbeitszeitkonfiguration und Account-Management. Desktop-optimiert: alle Einstellungen auf einen Blick.

---

## Stories

### S01: Profil-Sektion
**Als** Benutzer **moechte ich** mein Profil sehen und verwalten, **damit** ich weiss welches Konto angemeldet ist.

**Akzeptanzkriterien:**
- [ ] Profil-Card oben auf der Einstellungsseite (volle Breite)
- [ ] Angezeigt: Avatar (oder Initialen), Name, E-Mail-Adresse
- [ ] "Abmelden"-Button (rechts)
- [ ] "Konto loeschen"-Link (destructive, mit Bestaetigung):
  - Dialog: "Konto und alle Daten unwiderruflich loeschen?"
  - Eingabe der E-Mail zur Bestaetigung
  - API-Call: Account-Delete Endpoint

**Aufwand:** M

---

### S02: Arbeitszeit-Einstellungen
**Als** Benutzer **moechte ich** meine Wochenstunden und Arbeitstage konfigurieren, **damit** die Soll-Stunden korrekt berechnet werden.

**Akzeptanzkriterien:**
- [ ] Card "Arbeitszeit" (linke Spalte)
- [ ] Feld "Stunden/Woche": Zahlenfeld (Dezimal, z.B. 40.0, 32.5)
  - Minimum: 1, Maximum: 48
  - Validierung bei Eingabe
- [ ] Arbeitstage-Selector: 7 Toggle-Buttons (Mo-So)
  - Aktiv: Filled (Primary)
  - Inaktiv: Outline (Gray)
  - Mindestens 1 Tag muss aktiv sein
- [ ] Auto-Save: Aenderungen werden nach 500ms Debounce gespeichert
- [ ] API-Call: UserSettings Update
- [ ] Toast: "Einstellungen gespeichert"

**Aufwand:** M

---

### S03: Standort & Urlaub
**Als** Benutzer **moechte ich** mein Bundesland und meine Urlaubstage konfigurieren, **damit** Feiertage und Urlaubsanspruch korrekt berechnet werden.

**Akzeptanzkriterien:**
- [ ] Card "Standort & Urlaub" (rechte Spalte)
- [ ] Bundesland-Dropdown: Alle 16 Bundeslaender
  - Unter dem Dropdown: "X Feiertage in {Jahr}" (dynamisch)
- [ ] Feld "Urlaubstage/Jahr": Zahlenfeld (Integer)
  - Minimum: 0, Maximum: 50
  - Hinweis: "Gesetzlicher Mindestanspruch: 20 Tage bei 5-Tage-Woche"
- [ ] Auto-Save mit Debounce
- [ ] Bei Bundesland-Wechsel: Hinweis "Feiertage werden neu berechnet"

**Aufwand:** S

---

### S04: Personalnummer
**Als** Benutzer **moechte ich** meine Personalnummer hinterlegen, **damit** sie in Exports (PDF, DATEV) erscheint.

**Akzeptanzkriterien:**
- [ ] Feld "Personalnummer" in der Profil-Card
- [ ] Freitext-Feld (max. 20 Zeichen)
- [ ] Optional (kann leer bleiben)
- [ ] Auto-Save mit Debounce

**Aufwand:** S

---

### S05: Schulferien-Verwaltung
**Als** Benutzer **moechte ich** meine Schulferien verwalten, **damit** sie im Kalender angezeigt werden.

**Akzeptanzkriterien:**
- [ ] Card "Kalender" (linke Spalte, zweite Reihe)
- [ ] Link "Schulferien verwalten" oeffnet ein Modal/Dialog
- [ ] Im Dialog:
  - Liste der bestehenden Schulferien-Perioden
  - Pro Eintrag: Name, Start-Datum, End-Datum, Loeschen-Button
  - "Neue Schulferien hinzufuegen"-Button
  - Formular: Name, Start-Datum (DatePicker), End-Datum (DatePicker)
- [ ] API: SchoolHolidayPeriods CRUD
- [ ] Validierung: Ende >= Start, Name nicht leer

**Aufwand:** M

---

### S06: Erscheinungsbild
**Als** Benutzer **moechte ich** das Erscheinungsbild der App waehlen, **damit** ich zwischen hellem und dunklem Design wechseln kann.

**Akzeptanzkriterien:**
- [ ] Sektion am unteren Rand der Einstellungsseite
- [ ] Drei Optionen als Toggle-Gruppe: System / Hell / Dunkel
- [ ] Auswahl wird sofort angewendet (kein Neuladen)
- [ ] Auswahl wird in UserSettings gespeichert

**Aufwand:** S

---

### S07: App-Info & Legal-Links
**Als** Benutzer **moechte ich** Links zu Datenschutz, AGB und Impressum finden, **damit** ich meine Rechte kennen kann.

**Akzeptanzkriterien:**
- [ ] Footer-Sektion auf der Einstellungsseite
- [ ] Links: Datenschutz (/privacy), AGB (/terms), Impressum (/imprint)
- [ ] Links oeffnen in neuem Tab (target="_blank")
- [ ] Version der Web-App angezeigt (z.B. "v1.0.0")

**Aufwand:** S

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Profil | M | E02, E01-S03 |
| S02 Arbeitszeit | M | E02, E01-S04 |
| S03 Standort & Urlaub | S | E02, E01-S04 |
| S04 Personalnummer | S | S01 |
| S05 Schulferien | M | E01-S04 |
| S06 Erscheinungsbild | S | E02-S05 |
| S07 App-Info & Legal | S | E07 |

**Gesamt: ca. 1 Woche**

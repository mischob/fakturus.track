# EPIC 04: Urlaub & Kalender

## Ziel

Vollstaendige Urlaubs- und Krankheitstage-Verwaltung mit grossem Desktop-Kalender, Modus-Wechsel und Bereichsauswahl. Feature-Paritaet mit der Mobile-App, plus Desktop-spezifische Verbesserungen.

---

## Stories

### S01: Kalender-Komponente
**Als** Benutzer **moechte ich** einen grossen Monatskalender sehen, **damit** ich meine Urlaubs- und Krankheitstage uebersichtlich verwalten kann.

**Akzeptanzkriterien:**
- [ ] Monatskalender, Desktop-optimiert (grosse Tagesfelder)
- [ ] Monatsnavigation: Vor/Zurueck-Pfeile + Monatsname/Jahr
- [ ] Wochentage als Header (Mo, Di, Mi, Do, Fr, Sa, So)
- [ ] Woche beginnt mit Montag
- [ ] Farbkodierung der Tage:
  - Normal (Arbeitstag): Weiss/Standard
  - Urlaub: Cyan-Hintergrund (#06B6D4)
  - Krankheitstag: Rot-Hintergrund (#EF4444)
  - Feiertag: Lila-Punkt + Name (#8B5CF6)
  - Wochenende: Grau, nicht klickbar
  - Schulferien: Oranger Unterstrich (#F97316)
  - Heute: Blauer Rahmen
- [ ] Feiertage: Name im Tagesfeld sichtbar (Desktop hat genug Platz)
- [ ] Legende unterhalb des Kalenders

**Aufwand:** L

---

### S02: Urlaub setzen/entfernen
**Als** Benutzer **moechte ich** per Klick auf einen Arbeitstag Urlaub setzen oder entfernen, **damit** ich meinen Urlaub schnell eintragen kann.

**Akzeptanzkriterien:**
- [ ] Klick auf leeren Arbeitstag: Setzt Urlaub (cyan)
- [ ] Klick auf Urlaubstag: Entfernt Urlaub
- [ ] Resturlaub-Zaehler wird sofort aktualisiert
- [ ] Nicht klickbar: Wochenenden, Feiertage
- [ ] API-Call: VacationDay erstellen/loeschen
- [ ] Toast bei Fehler ("Urlaub konnte nicht gespeichert werden")
- [ ] Warnung wenn kein Resturlaub mehr: "Kein Resturlaub mehr verfuegbar"

**Aufwand:** M

---

### S03: Modus-Wechsel (Urlaub/Krankheit)
**Als** Benutzer **moechte ich** zwischen Urlaub- und Krankheitsmodus wechseln, **damit** ich beide Abwesenheitstypen eintragen kann.

**Akzeptanzkriterien:**
- [ ] Toggle-Buttons ueber/unter dem Kalender: [Urlaub] [Krankheit]
- [ ] Aktiver Modus visuell hervorgehoben (Primary-Button vs. Outline-Button)
- [ ] Im Urlaub-Modus: Klick setzt/entfernt Urlaub
- [ ] Im Krankheits-Modus: Klick setzt/entfernt Krankheitstag
- [ ] Klick auf bestehenden Tag des ANDEREN Typs:
  - Bestaetigung: "Diesen Urlaubstag in einen Krankheitstag umwandeln?"
  - Bei Bestaetigung: Typ wird gewechselt
- [ ] Krankheitstage reduzieren Soll-Stunden (Urlaubskontingent unveraendert)

**Aufwand:** M

---

### S04: Bereichsauswahl (Shift+Klick)
**Als** Benutzer **moechte ich** mehrere Tage auf einmal markieren, **damit** ich laengere Urlaubszeitraeume schnell eintragen kann.

**Akzeptanzkriterien:**
- [ ] Erster Klick: Markiert Start-Tag
- [ ] Shift+Klick: Markiert alle Arbeitstage zwischen Start und Ende
- [ ] Wochenenden und Feiertage werden uebersprungen
- [ ] Visual Preview: Bevor Shift losgelassen, werden die betroffenen Tage hervorgehoben
- [ ] Resturlaub-Zaehler zeigt Vorschau an ("-X Tage")
- [ ] Warnung wenn Resturlaub nicht ausreicht
- [ ] Alle Tage werden in einem Batch an die API gesendet

**Aufwand:** M

---

### S05: Resturlaub-Card
**Als** Benutzer **moechte ich** meinen Resturlaub prominent sehen, **damit** ich weiss wie viele Tage ich noch habe.

**Akzeptanzkriterien:**
- [ ] Card oberhalb des Kalenders
- [ ] Anzeige: "X von Y Tagen" + Fortschrittsbalken
- [ ] Genommene Tage als Zahl
- [ ] Fortschrittsbalken: Cyan fuer genommen, Grau fuer uebrig
- [ ] Berechnung basierend auf VacationDays API

**Aufwand:** S

---

### S06: Krankheitstage-Card
**Als** Benutzer **moechte ich** meine Krankheitstage als Zusammenfassung sehen, **damit** ich den Ueberblick behalte.

**Akzeptanzkriterien:**
- [ ] Card neben Resturlaub-Card
- [ ] Anzeige: "X Tage in {Jahr}"
- [ ] Berechnung basierend auf VacationDays API (type = sick)

**Aufwand:** S

---

### S07: Feiertage-Liste
**Als** Benutzer **moechte ich** die kommenden Feiertage sehen, **damit** ich meine Arbeit und meinen Urlaub besser planen kann.

**Akzeptanzkriterien:**
- [ ] Karte "Kommende Feiertage" unterhalb des Kalenders
- [ ] Liste der naechsten 5 Feiertage (basierend auf Bundesland)
- [ ] Pro Eintrag: Datum + Name
- [ ] Vergangene Feiertage werden nicht angezeigt

**Aufwand:** S

---

### S08: Schulferien-Anzeige
**Als** Benutzer **moechte ich** Schulferien im Kalender sehen, **damit** ich meine Urlaubsplanung mit den Schulferien abstimmen kann.

**Akzeptanzkriterien:**
- [ ] Schulferien-Tage im Kalender mit orangem Unterstrich markiert
- [ ] Karte "Schulferien {Jahr}" unterhalb des Kalenders
- [ ] Liste der eingetragenen Schulferien-Perioden (Name, Zeitraum)
- [ ] Daten via SchoolHolidayPeriods API

**Aufwand:** S

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Kalender-Komponente | L | E02 |
| S02 Urlaub setzen/entfernen | M | S01 |
| S03 Modus-Wechsel | M | S02 |
| S04 Bereichsauswahl | M | S02 |
| S05 Resturlaub-Card | S | E01-S04 |
| S06 Krankheitstage-Card | S | E01-S04 |
| S07 Feiertage-Liste | S | E01-S04 |
| S08 Schulferien | S | E01-S04 |

**Gesamt: ca. 1.5 Wochen**

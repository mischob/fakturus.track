# UX-Flows -- Benutzerszenarien

## 1. Erster App-Start (Onboarding)

### Szenario
Neuer Nutzer oeffnet die App zum ersten Mal.

### Flow

```
App oeffnet
    │
    v
Login Screen
    │
    ├── Tap "Mit Apple anmelden"
    │       │
    │       v
    │   B2C Login (System-WebView)
    │       │
    │       ├── [Erfolg] ──> Erste Synchronisation
    │       │                    │
    │       │                    v
    │       │               "Daten werden geladen..."
    │       │               (Spinner, 2-5 Sekunden)
    │       │                    │
    │       │                    v
    │       │               Zeiten Screen (leer)
    │       │               "Bereit fuer den ersten Eintrag"
    │       │
    │       └── [Abgebrochen] ─���> Zurueck zu Login Screen
    │
    ├── Tap "Mit Google anmelden"
    │       └── (gleicher Flow wie Apple)
    │
    └── Tap "Mit E-Mail anmelden"
            └── (gleicher Flow, B2C E-Mail/Passwort Formular)
```

### Design-Entscheidungen
- **Kein Onboarding-Wizard**: Die App ist einfach genug, dass kein Tutorial noetig ist
- **Kein "Tour ueberspringen"**: Nutzer kommt direkt zur App
- **Erste Synchronisation**: Kurzer Ladebildschirm, dann direkt zur App
- **Settings-Defaults**: Sinnvolle Defaults (40h/Woche, Mo-Fr, NW, 30 Tage Urlaub)

---

## 2. Taegliche Zeiterfassung

### Szenario
Mitarbeiter kommt morgens zur Arbeit und erfasst seine Zeit.

### Flow: Einfacher Tag

```
App oeffnen
    │
    v
Zeiten Screen
(Idle State: "Bereit fuer den naechsten Eintrag")
    │
    ├── Tap "Starten" ──> Session wird erstellt
    │                       Startzeit = jetzt (08:30)
    │                       Timer startet (animiert)
    │                       │
    │                       v
    │                   ActiveSessionCard
    │                   (Timer laeuft: 00:00:01...)
    │                       │
    │                  [Arbeitstag vergeht]
    │                       │
    │                       v
    │                   Tap "Stop" ──> Timer haelt an
    │                   Endzeit = jetzt (17:00)
    │                   Session im "Stopped" State
    │                       │
    │                       v
    │                   Tap "Fertig" ──> Session wird abgeschlossen
    │                   Verschiebt sich in die History
    │                   Sync wird getriggert
    │                       │
    │                       v
    │                   Zeiten Screen
    │                   (Idle State + neue Session in History)
```

### Flow: Korrektur noetig

```
ActiveSessionCard (gestoppt)
    │
    ├── Startzeit antippen ──> TimePicker oeffnet sich
    │       │
    │       v
    │   Neue Startzeit waehlen (z.B. 08:00 statt 08:30)
    │       │
    │       v
    │   Dauer wird automatisch aktualisiert
    │
    ├── Endzeit antippen ──> TimePicker oeffnet sich
    │       └── (gleicher Flow)
    │
    └── Tap "Fertig" ──> Session mit korrigierten Zeiten abschliessen
```

### Flow: Vergessener Eintrag nachtraeglich erfassen

```
Zeiten Screen (Idle State)
    │
    v
Tap "Starten" ──> Neue Session (heute, jetzt)
    │
    v
Datum antippen ──> DatePicker
    │
    v
Gestern waehlen (28.03.2026)
    │
    v
Startzeit auf 08:00 setzen
Endzeit auf 17:00 setzen
    │
    v
Tap "Fertig" ──> Session fuer gestern gespeichert
```

---

## 2b. Pause erfassen (Marktanalyse: gesetzliche Pflicht)

> **Marktanalyse-Erkenntnis:** Pausenerfassung ist gesetzliche Pflicht (ArbZG).
> 7/11 Wettbewerber bieten diese Funktion. Ohne Pausen ist die App nicht compliant.

### Szenario: Mittagspause waehrend laufender Session

### Flow

```
ActiveSessionCard (Timer laeuft: 04:02:15)
    │
    ├── Tap "Pause" ──> Timer wird angehalten
    │                     Pause-Timer startet (sichtbar)
    │                     Card wechselt zu "Pausiert"-State
    │                     │
    │                [Mittagspause]
    │                     │
    │                     v
    │                Tap "Weiter" ──> Timer laeuft weiter
    │                     Pausendauer wird aufaddiert (z.B. 32 min)
    │                     Anzeige: "Pause: 32 min"
    │
    └── Spaeter: Tap "Fertig"
         Session wird abgeschlossen
         Brutto: 08:30h | Pause: 32min | Netto: 07:58h
```

### Flow: Pause manuell eingeben (nachtraegliche Erfassung)

```
Session bearbeiten (Session Detail Sheet)
    │
    v
Neues Feld: "Pause (Minuten)"
    │
    v
Eingabe: 30 ──> Nettodauer wird neu berechnet
    │
    v
Tap "Speichern"
```

### ArbZG-Pausenhinweis

```
Timer erreicht 6 Stunden (ohne Pause)
    │
    v
Dezenter Hinweis (In-App Banner):
"Erinnerung: Nach 6 Stunden Arbeit steht Ihnen
 eine Pause von mindestens 30 Minuten zu."
    │
    v
Timer erreicht 9 Stunden (Pause < 45 min)
    │
    v
"Erinnerung: Nach 9 Stunden Arbeit betraegt
 die Mindestpause 45 Minuten."
```

---

## 3. History durchsehen

### Szenario
Nutzer moechte seine Eintraege der letzten Wochen pruefen.

### Flow

```
Zeiten Screen
    │
    v
Scrollen zur History
    │
    v
Monatsgruppe "Maerz 2026" antippen ──> Gruppe klappt auf
    │                                     Einzelne Sessions sichtbar
    │
    ├── Session antippen ──> Session Detail Sheet oeffnet sich
    │       │
    │       ├── Zeiten korrigieren ──> Speichern ──> Sheet schliesst
    │       │
    │       └── Loeschen ──> Bestaetigung ──> Session entfernt
    │
    ├── Session nach links wischen ──> Loeschen-Option
    │       │
    │       └── Tap "Loeschen" ──> Session entfernt
    │                                (Undo-Snackbar fuer 5 Sekunden)
    │
    └── Pull-to-Refresh ──> Sync mit Backend
                             Sessions werden aktualisiert
```

---

## 4. Urlaub eintragen

### Szenario
Nutzer plant seinen Urlaub und traegt die Tage ein.

### Flow: Einzelne Tage

```
Tab "Urlaub" antippen
    │
    v
Urlaub Screen
(Kalender zeigt aktuellen Monat)
    │
    v
Zum Juli navigieren (→ → → →)
    │
    v
Tap auf 15. Juli (Arbeitstag) ──> Tag wird cyan markiert
                                    Resturlaub: 24 (vorher 25)
    │
    v
Tap auf 16. Juli ──> Markiert (Resturlaub: 23)
Tap auf 17. Juli ──> Markiert (Resturlaub: 22)
Tap auf 18. Juli ──> Markiert (Resturlaub: 21)
    │
    v
Tap auf 19. Juli ──> Samstag, nicht anwaehlbar (grau)
    │
    v
Tap auf 21. Juli ──> Markiert (Resturlaub: 20)
    │
    v
Sync wird automatisch getriggert
```

### Flow: Tag wieder entfernen

```
Tap auf bereits markierten 15. Juli ���─> Markierung entfernt
                                         Resturlaub: 21 (wieder +1)
```

### Edge Cases
- Feiertag antippen: Keine Reaktion (visueller Hinweis: lila, nicht anwaehlbar)
- Wochenende antippen: Keine Reaktion (grau, nicht anwaehlbar)
- Letzter Urlaubstag verbraucht: Warnung "Kein Resturlaub mehr"

---

## 5. Ueberstunden pruefen

### Szenario
Nutzer moechte wissen, wie viele Ueberstunden er hat.

### Flow

```
Tab "Gesamt" antippen
    │
    v
Gesamt Screen
    │
    v
Summary Cards sichtbar:
  - Ueberstunden: +12:30h (gruen)
  - Urlaub: 5/30 (25 uebrig)
  - Feiertage: 11
    │
    v
Scrollen zur Monatstabelle
    │
    v
Monat fuer Monat pruefen:
  Jan: +3:15h | Feb: -1:30h | Maer: -0:15h | ...
    │
    v
Vorjahr pruefen: Tap "← 2025"
    │
    v
Tabelle zeigt 2025 (alle 12 Monate)
```

---

## 6. Einstellungen aendern

### Szenario
Nutzer wechselt von Vollzeit auf Teilzeit (32h/Woche, Mo-Do).

### Flow

```
Tab "Einstellungen" antippen
    │
    v
Einstellungen Screen
    │
    v
Sektion "Arbeitszeit":
    │
    ├── "Stunden/Woche" antippen
    │       │
    │       v
    │   Wert von 40 auf 32 aendern
    │   (Stepper/NumericField)
    │
    └── Arbeitstage:
        │
        v
    [Mo] [Di] [Mi] [Do] [Fr] Sa  So
    Tap auf "Fr" ──> Freitag deaktiviert
    Ergebnis: [Mo] [Di] [Mi] [Do]  Fr  Sa  So
    │
    v
Automatisch gespeichert und synchronisiert
    │
    v
Hinweis: "Ueberstunden werden neu berechnet"
```

### Flow: Bundesland wechseln

```
Sektion "Standort":
    │
    v
"Bundesland" antippen ──> Picker/Dropdown oeffnet sich
    │
    v
"Bayern" auswaehlen (vorher: Nordrhein-Westfalen)
    │
    v
Anzeige aktualisiert: "13 Feiertage in 2026" (vorher: 11)
    │
    v
Automatisch gespeichert
```

---

## 7. Sync-Verhalten

### Szenario: Offline arbeiten

```
Nutzer startet App (kein Internet)
    │
    v
Offline-Banner erscheint:
"Offline -- Aenderungen werden lokal gespeichert"
    │
    v
Nutzer erfasst Zeiten normal
(Alles wird lokal in SQLite gespeichert)
    │
    v
[Internet wieder verfuegbar]
    │
    v
Offline-Banner verschwindet
Automatischer Sync startet
Sync-Indikator zeigt "Synchronisiere..."
    │
    v
Sync abgeschlossen
Sync-Indikator: "Synchronisiert ✓" (fuer 3 Sekunden)
```

### Szenario: Manueller Sync

```
Nutzer zieht Liste nach unten (Pull-to-Refresh)
    │
    v
Sync startet
    │
    ├── [Erfolg] ──> Daten aktualisiert
    │                 Kurze Erfolgsmeldung
    │
    └── [Fehler] ──> Fehlermeldung:
                      "Sync fehlgeschlagen. Bitte erneut versuchen."
                      Daten bleiben lokal gespeichert
```

---

## 8. Fehlerfaelle

### Login fehlgeschlagen

```
Login Button antippen
    │
    v
B2C WebView oeffnet sich
    │
    ├── Falsches Passwort ──> B2C zeigt eigene Fehlermeldung
    │                          Nutzer kann erneut versuchen
    │
    ├── Netzwerk-Fehler ──> "Anmeldung fehlgeschlagen.
    │                         Bitte pruefen Sie Ihre Internetverbindung."
    │
    └���─ Nutzer bricht ab ──> Zurueck zum Login Screen
                              Keine Fehlermeldung
```

### Session-Speichern fehlgeschlagen

```
Tap "Fertig"
    │
    v
[Lokales Speichern fehlschlaegt] (unwahrscheinlich)
    │
    v
Fehlermeldung: "Speichern fehlgeschlagen. Bitte erneut versuchen."
Session bleibt im aktuellen Zustand
```

### API-Fehler bei Sync

```
Sync startet
    │
    v
[401 Unauthorized]
    │
    v
Token-Refresh wird versucht
    │
    ├��─ [Refresh erfolgreich] ──> Sync wird wiederholt
    │
    └── [Refresh fehlgeschlagen] ──> Nutzer wird zum Login aufgefordert:
                                      "Ihre Sitzung ist abgelaufen.
                                       Bitte melden Sie sich erneut an."
                                      [Anmelden]
```

---

## 8b. Monatsreport exportieren (Marktanalyse: Must-Have)

> **Marktanalyse:** PDF/Excel-Export ist Basisanforderung. 10/11 Wettbewerber bieten das.
> Nutzer muessen Nachweise gegenueber Behoerden und Arbeitgebern erbringen koennen.

### Szenario: PDF-Monatsreport erstellen

```
Tab "Gesamt" ──> Sektion "Export" (unterhalb der Monatstabelle)
    │
    ├── Tap "PDF-Monatsreport"
    │       │
    │       v
    │   Monatsauswahl (Picker: "Maerz 2026")
    │       │
    │       v
    │   Vorschau des Reports:
    │   ┌─────────────────────────────┐
    │   │  fakturus.track             │
    │   │  Arbeitszeitnachweis        │
    │   │  Maerz 2026                 │
    │   │  Max Mustermann             │
    │   │                             │
    │   │  Datum   Start  Ende  P  h  │
    │   │  01.03.  08:30  17:00 30 8:00│
    │   │  02.03.  09:00  17:30 30 8:00│
    │   │  ...                        │
    │   │                             │
    │   │  Soll: 168:00h              │
    │   │  Ist:  172:30h              │
    │   │  Ueberstunden: +4:30h       │
    │   │  Urlaub: 2 Tage             │
    │   │  Krank: 1 Tag               │
    │   └─────────────────────────────┘
    │       │
    │       v
    │   [Teilen]  [Speichern]
    │       │
    │       v
    │   System-Share-Sheet
    │   (E-Mail, AirDrop, Dateien, ...)
    │
    └── Tap "CSV-Export"
            │
            v
        Zeitraum waehlen (Monat / Quartal / Jahr)
            │
            v
        CSV wird generiert und Share-Sheet oeffnet sich
```

---

## 8c. Krankheitstage erfassen (Marktanalyse: Wichtige Luecke)

### Szenario: Krankheitstag eintragen

### Interaktionslogik (vollstaendig spezifiziert)

```
Tab "Urlaub" antippen
    │
    v
Urlaub Screen (Kalender)

=== TAP-Verhalten (Schnellaktion) ===

Tap auf leeren Arbeitstag ──> Urlaub setzen (cyan)
    │                          (Rueckwaertskompatibilitaet zum bestehenden Urlaub-Flow)
    │
Tap auf Urlaubstag ──> Urlaub entfernen (normal)
    │                   Resturlaub +1
    │
Tap auf Krankheitstag ──> Krankheitstag entfernen (normal)
    │                      Krankheitstage-Zaehler -1
    │                      Soll-Stunden werden wieder normal

=== LONG-PRESS-Verhalten (Erweiterte Aktion) ===

Long-Press auf leeren Arbeitstag ──> Kontext-Menue:
    │                                  "Urlaub" / "Krank"
    │
    ├── Tap "Urlaub" ──> Tag wird cyan markiert (Urlaub)
    │                     Resturlaub -1
    │
    └── Tap "Krank" ──> Tag wird rot markiert (Krankheitstag)
                         Krankheitstage-Zaehler +1
                         Soll-Stunden reduziert
                         (Urlaubskontingent unveraendert)

Long-Press auf markierten Tag (Urlaub ODER Krank) ──> Kontext-Menue:
    │                                                   "Typ wechseln" / "Entfernen"
    │
    ├── Tap "Typ wechseln"
    │       ├── War Urlaub ──> Wird Krankheitstag (rot)
    │       │                   Resturlaub +1, Krankheitstage +1
    │       │
    │       └── War Krank ──> Wird Urlaub (cyan)
    │                          Resturlaub -1, Krankheitstage -1
    │
    └── Tap "Entfernen" ──> Markierung komplett entfernt
                             Entsprechende Zaehler angepasst
```

### Design-Entscheidung
**Long-Press mit Kontext-Menue** fuer erweiterte Aktionen -- klarer als zyklisches Tippen,
vertrautes Pattern auf beiden Plattformen. Einfacher Tap bleibt fuer die haeufigste Aktion
(Urlaub setzen/entfernen) erhalten, um Rueckwaertskompatibilitaet zu gewaehrleisten.

### Edge Cases
- Feiertag (Long-Press oder Tap): Keine Reaktion (visuell lila, nicht anwaehlbar)
- Wochenende (Long-Press oder Tap): Keine Reaktion (grau, nicht anwaehlbar)
- Tag mit laufender/abgeschlossener WorkSession: Warnung "Arbeitssitzung vorhanden" im Kontext-Menue

---

## 9. ArbZG-Hinweise (Phase 2+)

### Szenario: 10h Ueberschreitung

```
Timer laeuft seit 10 Stunden
    │
    v
Dezente Benachrichtigung (in-App Banner, nicht Push):
"Hinweis: Sie arbeiten seit 10 Stunden.
 Laut Arbeitszeitgesetz betraegt die taegliche
 Hoechstarbeitszeit 10 Stunden."
    │
    v
[Verstanden]  ──> Banner schliesst sich
                   Timer laeuft weiter (kein Zwangsstopp)
```

### Szenario: Pausenhinweis

```
Timer erreicht 6 Stunden
    │
    v
Dezenter Hinweis:
"Erinnerung: Nach 6 Stunden Arbeit steht Ihnen
 eine Pause von mindestens 30 Minuten zu."
    │
    v
[Verstanden]
```

### Design-Prinzip
- **Informativ, nicht einschraenkend**: Die App gibt Hinweise, erzwingt aber nichts
- **Dezent**: Kein Modal, kein Popup -- nur ein Banner oder Toast
- **Einmalig pro Tag**: Hinweis wird nur einmal pro Tag gezeigt
- **Konfigurierbar**: In den Settings ein-/ausschaltbar

# Feature-Liste -- Fakturus Track Native Apps

## Legende

- **[IST]** -- Feature existiert bereits im Backend/MAUI-App
- **[NEU]** -- Neues Feature fuer die nativen Apps
- **[VERBESSERT]** -- Bestehendes Feature wird deutlich verbessert
- **P1/P2/P3** -- Prioritaet (P1 = Phase 1 MVP, P2 = Phase 2, P3 = Phase 3+)
- **[FREE]** / **[STARTER]** / **[PRO]** / **[TEAM]** -- Tier-Zuordnung (siehe Preismodell)

> **Hinweis Preismodell (aus Marktanalyse):** fakturus.track wird ein Freemium-Modell nutzen:
> FREE (1 User, 365 Tage Historie), STARTER (2,99 EUR/User/Monat), PRO (4,99 EUR), TEAM (ab 3,99 EUR, ab 5 User).
> Features sind nach Tier markiert. Details in `/marketing/preisanalyse.md`.

---

## 1. Authentifizierung & Benutzerverwaltung

### 1.1 Login [IST][VERBESSERT] -- P1
- Azure AD B2C Integration mit MSAL
- **[NEU]** Social Login Buttons: Apple, Google, E-Mail (adaptiert von fakturus.poi, muss in B2C-Policy konfiguriert werden)
- **[NEU]** Biometrische Entsperrung (Face ID / Touch ID / Fingerprint) fuer Schnellzugriff
- **[IST]** Anonymer Modus (offline Nutzung ohne Login)
- **[IST]** Automatische Token-Aktualisierung (Silent Token Refresh)
- **[IST]** Sichere Token-Speicherung (Keychain/Keystore)

### 1.2 Logout [IST] -- P1
- Abmeldung mit Token-Bereinigung
- **[NEU]** Bestaetigung vor Logout ("Moechten Sie sich abmelden?")

### 1.3 Profil [NEU] -- P2
- Anzeige des angemeldeten Benutzers (Name, E-Mail)
- **[NEU]** Profilbild (aus B2C-Claims oder Initialen-Avatar)

---

## 2. Zeiterfassung (Kern-Feature)

### 2.1 Neue Arbeitssitzung erstellen [IST][VERBESSERT] -- P1
- "Neuer Eintrag" Button prominent auf dem Hauptscreen
- **[VERBESSERT]** One-Tap Start: Sofortiger Arbeitsbeginn mit einem Tap
- **[NEU]** Haptic Feedback bei Start/Stop
- Datum wird automatisch auf heute gesetzt

### 2.2 Start/Stop-Workflow [IST][VERBESSERT] -- P1 [FREE]
- **Start**: Setzt Startzeit auf aktuelle Uhrzeit
- **Stop**: Setzt Endzeit auf aktuelle Uhrzeit (Session bleibt offen zur Bearbeitung)
- **Finish**: Schliesst die Session ab und verschiebt sie in die History
- **[VERBESSERT]** Live-Timer-Anzeige waehrend laufender Session (animiert)
- **[NEU]** Benachrichtigung nach 10h Arbeitszeit (ArbZG-Konformitaet)

### 2.2b Pausenerfassung [NEU] -- P1 [FREE]
> **Marktanalyse-Erkenntnis:** Pausenerfassung ist gesetzliche Pflicht (ArbZG) und fehlt aktuell.
> 7 von 11 Wettbewerbern bieten Pausenerfassung. Ohne dieses Feature ist fakturus.track
> nicht compliant und faellt bei Feature-Vergleichen negativ auf.
>
> **Tier-Begruendung:** Gesetzliche Pflichtfunktion (ArbZG) darf nicht hinter einer Paywall stehen.
> Pausenerfassung muss im FREE-Tier verfuegbar sein.

- **[NEU]** Pause-Button waehrend laufender Session (stoppt Timer, Session bleibt aktiv)
- **[NEU]** Mehrere Pausen pro Session moeglich
- **[NEU]** Manuelle Pauseneingabe bei nachtraeglicher Erfassung (Pausendauer in Minuten)
- **[NEU]** Automatische Berechnung der Nettoarbeitszeit (Brutto minus Pausen)
- **[NEU]** ArbZG-Pausenhinweis: Nach 6h Arbeit Hinweis auf 30min Pflichtpause, nach 9h auf 45min
- **[NEU]** Pausendauer wird in History und Reports angezeigt
- **Backend-Aenderung erforderlich:** WorkSession-DTO muss um Pausenfelder erweitert werden
  (z.B. `PauseMinutes: int` oder separate `PauseEntry`-Entity)

### 2.3 Manuelle Zeitbearbeitung [IST][VERBESSERT] -- P1
- Start- und Endzeit manuell anpassbar
- **[VERBESSERT]** Native Date/Time Picker (iOS DatePicker, Android TimePicker)
- **[VERBESSERT]** Datum nachtraeglich aenderbar
- **[NEU]** Validierung: Endzeit muss nach Startzeit liegen

### 2.4 Session loeschen [IST] -- P1
- Loeschen einer Session (lokal + Backend)
- **[VERBESSERT]** Swipe-to-Delete Geste
- **[NEU]** Undo-Moeglichkeit (Snackbar mit "Rueckgaengig")

### 2.5 History-Ansicht [IST][VERBESSERT] -- P1
- Gruppierung nach Monat (Monatsname deutsch)
- Auf-/Zuklappbare Monatsgruppen
- Pro Gruppe: Anzahl Eintraege, Gesamtdauer
- **[VERBESSERT]** Scrollen zu aktuellem Monat bei App-Start
- **[NEU]** Pull-to-Refresh zum manuellen Sync

### 2.6 Kalender-Import [IST] -- P2
- Import von Arbeitszeiten aus iCal-Feed
- **[VERBESSERT]** Visuelles Matching: Kalender-Events neben manuellen Eintraegen anzeigen
- **[NEU]** Automatischer Import (bei App-Start pruefen)

---

## 3. Ueberstunden & Gesamt-Uebersicht

### 3.1 Ueberstunden-Dashboard [IST][VERBESSERT] -- P2
- Gesamt-Ueberstunden des Jahres
- Monatliche Aufschluesselung (gearbeitet vs. erwartet vs. Ueberstunden)
- **[VERBESSERT]** Visuelles Dashboard mit Diagrammen statt nur Tabelle
- **[NEU]** Wochen-Ansicht (zusaetzlich zu Monats-Ansicht)
- **[NEU]** Trend-Anzeige (Pfeil hoch/runter im Vergleich zum Vormonat)

### 3.2 Jahresauswahl [IST] -- P2
- Navigation zwischen Jahren (Vor/Zurueck)
- Anzeige aller 12 Monate (aktuelle Jahr: nur bis aktueller Monat)

### 3.3 Urlaubsuebersicht (in Gesamt) [IST] -- P2
- Genommene Urlaubstage / Gesamtanspruch
- Verbleibende Urlaubstage

### 3.4 Feiertage (in Gesamt) [IST] -- P2
- Anzahl Feiertage im Jahr (basierend auf Bundesland)
- **[NEU]** Liste der Feiertage mit Datum

### 3.5 Schulferien-Beruecksichtigung [IST] -- P2
- Anzeige der nicht gearbeiteten Stunden waehrend Schulferien
- **[NEU]** Visuelle Markierung von Schulferien-Tagen im Kalender

---

## 4. Urlaubsverwaltung

### 4.1 Urlaubstage erfassen [IST] -- P2
- **[VERBESSERT]** Kalender-Ansicht zum Markieren von Urlaubstagen
- Tap auf Tag = Urlaub eintragen/entfernen (Toggle)
- **[NEU]** Bereichsauswahl (Von-Bis Datum fuer mehrere Tage)
- **[NEU]** Anzeige von Feiertagen und Wochenenden im Kalender (nicht auswaehlbar)

### 4.2 Urlaubstage synchronisieren [IST] -- P2
- Bidirektionale Synchronisation mit Backend
- Identische Sync-Logik wie Arbeitssitzungen

### 4.3 Urlaubsanspruch [IST] -- P2
- Anzeige des Resturlaubs
- **[NEU]** Warnung bei geringem Resturlaub (< 5 Tage)
- **[NEU]** Anteilige Berechnung bei Teilzeit (basierend auf Arbeitstagen pro Woche)

---

## 5. Einstellungen

### 5.1 Arbeitszeit-Konfiguration [IST][VERBESSERT] -- P2
- Wochenstunden (Standard: 40h)
- Arbeitstage-Auswahl (Bitmask: Mo-So, Standard Mo-Fr)
- **[VERBESSERT]** Visuelle Tagesauswahl (Toggles fuer jeden Wochentag)

### 5.2 Bundesland [IST][VERBESSERT] -- P2
- Auswahl des Bundeslandes (Dropdown mit allen 16 Bundeslaendern)
- Beeinflusst Feiertagsberechnung
- **[VERBESSERT]** Anzeige der Auswirkung ("X Feiertage in diesem Jahr")

### 5.3 Urlaubstage pro Jahr [IST] -- P2
- Konfiguration des Jahresurlaubs (Standard: 30 Tage)

### 5.4 Kalender-URL [IST] -- P2
- Eingabe einer iCal-Feed-URL
- **[NEU]** URL-Validierung mit Test-Abruf

### 5.5 Schulferien [IST] -- P2
- Verwaltung von Schulferien-Zeitraeumen
- Name, Start- und Enddatum, Jahr

### 5.6 App-Einstellungen [NEU] -- P3 [FREE]
- **[NEU]** Dark Mode Unterstuetzung (System/Hell/Dunkel)
- **[NEU]** Benachrichtigungs-Einstellungen
- **[NEU]** App-Version und Build-Nummer Anzeige
- **[NEU]** Rechtliche Hinweise (Datenschutz, Impressum, Lizenzen)

---

## 5b. Reporting & Export (Marktanalyse: Must-Have vor Launch)

> **Marktanalyse-Erkenntnis:** PDF/Excel-Export ist Basisanforderung -- 10 von 11 Wettbewerber
> bieten mindestens PDF-Export. Nutzer muessen Nachweise gegenueber Behoerden und Arbeitgebern
> erbringen koennen. Ohne Export-Funktion fehlt ein zentrales Compliance-Feature.

### 5b.1 PDF-Monatsreport [NEU] -- P2 [STARTER]
- **[NEU]** Monatsreport als PDF generieren (direkt in der App)
- Inhalt: Alle Sessions des Monats mit Datum, Start, Ende, Pause, Nettodauer
- Zusammenfassung: Gesamtstunden, Soll-Stunden, Ueberstunden, Urlaubstage
- **[NEU]** PDF teilen via System-Share-Sheet (E-Mail, Messenger, AirDrop etc.)
- **[NEU]** PDF lokal speichern (Files App / Downloads)
- Design: Sauber formatiert mit Fakturus-Logo, Monat/Jahr, Mitarbeitername

### 5b.2 Excel/CSV-Export [NEU] -- P2 [STARTER]
- **[NEU]** Monatsexport als CSV (kompatibel mit Excel, Google Sheets)
- **[NEU]** Spalten: Datum, Wochentag, Start, Ende, Pause (min), Netto (h), Typ (Arbeit/Urlaub/Feiertag/Krank)
- **[NEU]** Export-Zeitraum waehlbar (Monat, Quartal, Jahr)
- Teilen via System-Share-Sheet

### 5b.3 DATEV-Export [NEU] -- P3 [PRO]
> **Marktanalyse:** DATEV-Export ist der wichtigste Integrationspunkt im DACH-Raum.
> Nur Crewmeister und ZEP bieten das aktuell. Starkes Differenzierungsmerkmal.
- **[NEU]** Export im DATEV-kompatiblen Format (Lohn & Gehalt)
- **[NEU]** Steuerberater-taugliches Format

---

## 5c. Krankheitstage (Marktanalyse: Wichtige Luecke)

> **Marktanalyse-Erkenntnis:** 6 von 11 Wettbewerber bieten Krankheitstage-Tracking.
> Fuer eine vollstaendige Abwesenheitsverwaltung ist dies eine erwartete Basisfunktion.

### 5c.1 Krankheitstage erfassen [NEU] -- P2 [STARTER]
- **[NEU]** Krankheitstage im Kalender markieren (aehnlich wie Urlaub, aber mit eigener Farbe)
- **[NEU]** Krankheitstage reduzieren Soll-Stunden (wie Feiertage/Urlaub)
- **[NEU]** Separate Anzeige in Gesamt-Uebersicht: "X Krankheitstage"
- **[NEU]** Krankheitstage werden NICHT vom Urlaubskontingent abgezogen
- **Backend-Aenderung erforderlich:** Neue Entity `SickDay` oder Erweiterung von VacationDay mit Typ-Feld

---

## 6. Synchronisation

### 6.1 Offline-first Architektur [IST] -- P1
- Alle Daten werden lokal gespeichert (SQLite)
- Aenderungen werden als "pending" markiert
- Sync bei Netzwerk-Verfuegbarkeit

### 6.2 Background Sync [IST][VERBESSERT] -- P1
- Periodischer Sync (konfigurierbares Intervall, Standard: 30s)
- **[VERBESSERT]** Intelligenter Sync: Nur bei tatsaechlichen Aenderungen
- **[NEU]** iOS Background App Refresh
- **[NEU]** Android WorkManager fuer zuverlaessigen Background Sync

### 6.3 Manueller Sync [IST][VERBESSERT] -- P1
- Sync-Button
- **[VERBESSERT]** Pull-to-Refresh Geste
- Visueller Sync-Status-Indikator

### 6.4 Konfliktloesung [IST] -- P1
- Server-wins Strategie (Backend ist Source of Truth)
- Lokale Aenderungen werden bei Konflikt ueberschrieben

### 6.5 Netzwerk-Monitoring [IST] -- P1
- Automatische Erkennung von Online/Offline Status
- **[VERBESSERT]** Visueller Offline-Indikator (dezent, nicht aufdringlich)
- Automatischer Sync bei Wiederherstellung der Verbindung

---

## 7. Widgets & Erweiterungen (Phase 3)

### 7.1 iOS Widget [NEU] -- P3
- Home Screen Widget mit aktuellem Timer
- Quick Start/Stop Aktion aus Widget
- Heutige Arbeitszeit Zusammenfassung

### 7.2 Apple Watch App [NEU] -- P3
- Minimale Companion App
- Start/Stop Arbeitszeit vom Handgelenk
- Heutige Arbeitszeit anzeigen

### 7.3 Android Widget [NEU] -- P3
- Home Screen Widget analog iOS
- Quick-Start Shortcut

### 7.4 iOS Live Activity [NEU] -- P3
- Live Activity auf dem Sperrbildschirm waehrend laufender Session
- Dynamic Island Integration (iPhone 14 Pro+)

---

## 8. Nicht-funktionale Anforderungen

### 8.1 Performance
- App-Start unter 1 Sekunde (Cold Start)
- Smooth Scrolling (60fps) in allen Listen
- Sync darf UI nicht blockieren

### 8.2 Datenschutz (DSGVO)
- Keine Tracking-SDKs
- Daten nur auf eigenem Server (Azure Germany)
- Transparente Datenverarbeitung (Privacy Policy in App)
- Token in Secure Storage (Keychain/Keystore)

### 8.3 Barrierefreiheit
- VoiceOver / TalkBack Support
- Dynamic Type (iOS) / Schriftgroessen-Anpassung (Android)
- Ausreichende Kontraste (WCAG AA)

### 8.4 Lokalisierung
- Deutsch als primaere Sprache
- Englisch als Fallback
- Datum/Zeit in deutschem Format (TT.MM.JJJJ, HH:MM)

### 8.5 ArbZG-Konformitaet
- Erinnerung bei Ueberschreitung der taeglichen Hoechstarbeitszeit (10h)
- Hinweis auf Pausenpflicht (nach 6h: 30min, nach 9h: 45min)
- Hinweis auf Mindestruhezeit (11h zwischen Arbeitstagen)

---

## Feature-Priorisierungs-Matrix (MoSCoW) -- aktualisiert nach Marktanalyse

### Must Have (Phase 1 MVP)
- Login/Logout (Azure B2C) [FREE]
- Zeiterfassung (CRUD + Start/Stop/Finish) [FREE]
- **Pausenerfassung** [FREE] -- *Marktanalyse: gesetzliche Pflicht (ArbZG), darf nicht hinter Paywall*
- Offline-first mit Sync [FREE]
- History-Ansicht (Monatsgruppen) [FREE]
- Netzwerk-Monitoring [FREE]

### Should Have (Phase 2 -- vor Store-Launch)
- Ueberstunden-Dashboard [FREE]
- Urlaubsverwaltung [STARTER]
- **PDF-Monatsreport** [STARTER] -- *Marktanalyse: Basisanforderung, 10/11 Wettbewerber*
- **CSV/Excel-Export** [STARTER] -- *Marktanalyse: Nachweis gegenueber Behoerden*
- **Krankheitstage** [STARTER] -- *Marktanalyse: 6/11 Wettbewerber, erwartete Basisfunktion*
- Einstellungen (komplett) [FREE/STARTER]
- Kalender-Import [STARTER]
- Pull-to-Refresh [FREE]

### Could Have (Phase 3)
- Widgets (iOS + Android) [FREE]
- Apple Watch [STARTER]
- Live Activity / Dynamic Island [FREE]
- Dark Mode [FREE]
- DATEV-Export [PRO] -- *Marktanalyse: staerkstes Differenzierungsmerkmal im DACH-Raum*
- Biometrische Entsperrung [FREE]
- **Einfache Projektzuordnung** [PRO] -- *Marktanalyse: Kunden/Projekt pro Session*
- **Audit-Log** [PRO] -- *Marktanalyse: Manipulationssicherheit fuer Compliance*

### Won't Have (nicht in V1)
- Multi-Mandanten (Firmen-Verwaltung)
- Genehmigungsworkflows (erst mit Team-Tier)
- GPS-basierte Zeiterfassung
- Team-Ansichten (erst mit Team-Tier)
- Schichtplanung
- Rechnungsstellung

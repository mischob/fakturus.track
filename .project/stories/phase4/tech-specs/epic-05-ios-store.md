# Tech-Spec: EPIC 05 -- App Store Vorbereitung (iOS)

## Uebersicht

Kein neuer App-Code. Dieses EPIC ist Store-Konfiguration, Screenshot-Erstellung und Review-Compliance-Pruefung.

---

## Screenshots

### Empfohlene Szenarien (5-6 Screenshots)

| Nr | Screen | Demo-Daten | Marketing-Text (DE) | Marketing-Text (EN) |
|----|--------|-----------|---------------------|---------------------|
| 1 | Timer (laufende Session) | Session seit 08:15, aktuelle Dauer ~4:30h, gruener Punkt | "Arbeitszeit erfassen. Einfach." | "Track work hours. Simply." |
| 2 | History (Monatsansicht) | 3 Monate, je 15-20 Eintraege, variierte Zeiten | "Alle Zeiten im Ueberblick" | "All hours at a glance" |
| 3 | Urlaub-Kalender | Aktueller Monat mit 3 Urlaubstagen + Feiertag markiert | "Urlaub & Feiertage verwalten" | "Manage vacation & holidays" |
| 4 | Gesamt-Tab (Dashboard) | Ueberstunden +12:30h, Urlaub 8/30, Krankheit 2 | "Ueberstunden auf einen Blick" | "Overtime at a glance" |
| 5 | Export (PDF-Vorschau) | PDF mit Monatsreport, Share-Sheet angedeutet | "Export fuer Ihren Steuerberater" | "Export for your accountant" |
| 6 | Dark Mode (Timer) | Identisch zu Screenshot 1, aber Dark Mode | "Auch im Dunkeln produktiv" | "Productive in the dark" |

### Geraeteklassen

| Geraet | Aufloesung | Pflicht |
|--------|-----------|---------|
| iPhone 6.7" (15 Pro Max / 16 Pro Max) | 1290 x 2796 | Ja |
| iPhone 6.1" (15 / 16) | 1179 x 2556 | Ja |
| iPad Pro 12.9" | 2048 x 2732 | Optional (nur wenn iPad-Support) |

### Automatisierung

```bash
# Fastlane Snapshot (empfohlen)
fastlane snapshot

# Oder manuell via Simulator:
xcrun simctl io booted screenshot screenshot_timer.png
```

Fastlane `frameit` fuer Geraete-Rahmen + Marketing-Text-Overlay.

### Demo-Daten fuer Screenshots

Realistisch aussehende Daten, 3 Monate zurueck:
- Verschiedene Arbeitszeiten (7:30-8:30h)
- Pausen zwischen 30-60 Min
- 3 Urlaubstage, 2 Krankheitstage, 5 Feiertage
- Positive Ueberstunden (+12:30h)
- Bundesland: NRW (typischer Nutzer)

---

## App Store Listing

### Metadaten

| Feld | DE | EN |
|------|----|----|
| App-Name | Fakturus Track -- Zeiterfassung | Fakturus Track -- Time Tracking |
| Untertitel | Arbeitszeit. Einfach. Ueberall. | Work hours. Simple. Everywhere. |
| Keywords | Zeiterfassung,Arbeitszeit,Stempeluhr,ArbZG,Ueberstunden,Stundenzettel,DATEV | time tracking,work hours,timesheet,overtime,punch clock,DATEV,work log |
| Kategorie | Business (primaer), Productivity (sekundaer) | |
| Altersfreigabe | 4+ | |
| Preis | Kostenlos (mit IAP) | |

### Beschreibung (DE -- erste 167 Zeichen besonders wichtig)

```
Fakturus Track -- die native Zeiterfassungs-App fuer Deutschland. ArbZG-konform. Offline-first. Mit PDF- und DATEV-Export fuer Ihren Steuerberater.

KOSTENLOS STARTEN
- Arbeitszeit per Knopfdruck erfassen
- Pausen automatisch tracken
- Feiertage nach Bundesland
- 365 Tage History

STARTER (ab 2,99 EUR/Monat)
- Unbegrenzte History
- PDF-Monatsreport & CSV-Export
- Urlaubs- und Krankheitstage verwalten
- Widgets fuer den Homescreen
- Ueberstunden-Dashboard

PRO (ab 4,99 EUR/Monat)
- Alles aus Starter
- DATEV-Export (Lodas-Format)
- Schulferien-Anzeige
- Kalender-Integration

WARUM FAKTURUS TRACK?
- ArbZG-konform: Erfuellt die Zeiterfassungspflicht gemaess Arbeitszeitgesetz
- Offline-first: Funktioniert ohne Internetverbindung, synchronisiert automatisch
- Nativ fuer iOS: Keine Web-App, echte SwiftUI-Performance
- Made in Germany: Alle Daten in der EU (Azure Germany)
- Kein Tracking: Keine Werbung, keine Analytics-SDKs
```

### Promotional Text (aenderbar ohne Review)

```
Jetzt NEU: Fakturus Track -- die native Zeiterfassungs-App fuer Deutschland. ArbZG-konform. Offline-first. DATEV-Export.
```

---

## Review Compliance Checkliste

### Guideline 3.1.1 (In-App Purchase)

- Alle Premium-Features nur ueber Apple IAP freigeschaltet
- Kein externer Zahlungslink
- Keine Hinweise auf guenstigere Preise ausserhalb des App Stores

### Guideline 3.1.2 (Subscriptions)

- Auto-Renewable korrekt implementiert
- Preis dynamisch geladen (`Product.displayPrice`)
- Kuendigungshinweis in Paywall: "Abo verlaengert sich automatisch. Kuendigung jederzeit ueber Apple-ID-Einstellungen."
- Terms of Use und Privacy Policy in Paywall verlinkt

### Guideline 3.2.2 (Restore Purchases)

- "Kaeufe wiederherstellen" Button in Settings vorhanden
- Funktioniert via `AppStore.sync()`

### Guideline 5.1.1 (Privacy)

- Privacy Policy URL in App Store Connect hinterlegt
- URL oeffentlich erreichbar (ohne Login)

### Guideline 5.1.2 (App Privacy Details)

| Kategorie | Daten | Verknuepft mit Nutzer |
|-----------|-------|----------------------|
| Persoenliche Daten | Name, E-Mail (Azure B2C) | Ja |
| Nutzungsdaten | Arbeitszeiten, Pausen, Urlaubstage | Ja |
| Identifikatoren | Geraete-ID (fuer Sync) | Ja |
| Tracking | Keine | -- |
| Diagnose | Crashes (Sentry, anonymisiert) | Nein |

### Guideline 2.1 (Performance)

- Cold Start < 1 Sekunde
- Kein Crash im normalen Flow
- App funktioniert auch ohne Netzwerk

### Guideline 4.0 (Design)

- 100% native SwiftUI, kein WebView fuer Kern-Features
- Standard iOS Navigation Patterns

### Review Notes (fuer Apple Reviewer)

```
Fakturus Track is a time tracking app for the German market.

Test Account:
- Email: reviewer@test.fakturus.com
- Password: [wird generiert]

To test In-App Purchases:
1. Open any locked feature (e.g., PDF Export in "Gesamt" tab)
2. Tap the lock icon to open the Paywall
3. Subscribe to Starter or Pro

The app works without a subscription (FREE tier) with basic time tracking.
ArbZG-relevant features (timer, breaks, holidays) are always free as required by German labor law.
```

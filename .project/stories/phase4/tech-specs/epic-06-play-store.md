# Tech-Spec: EPIC 06 -- Google Play Store Vorbereitung (Android)

## Uebersicht

Kein neuer App-Code. Store-Konfiguration, Screenshots und Data Safety Section.

---

## Screenshots

### Identische Motive wie iOS (Konsistenz)

| Nr | Screen | Marketing-Text (DE) |
|----|--------|---------------------|
| 1 | Timer (laufende Session) | "Arbeitszeit erfassen. Einfach." |
| 2 | History (Monatsansicht) | "Alle Zeiten im Ueberblick" |
| 3 | Urlaub-Kalender | "Urlaub & Feiertage verwalten" |
| 4 | Gesamt-Tab (Dashboard) | "Ueberstunden auf einen Blick" |
| 5 | Export (PDF/CSV) | "Export fuer Ihren Steuerberater" |

### Geraeteklassen

| Geraet | Aufloesung | Pflicht |
|--------|-----------|---------|
| Phone (1080x1920+) | z.B. Pixel 8 | Ja |
| 7-Zoll Tablet | z.B. Nexus 7 | Empfohlen |
| 10-Zoll Tablet | z.B. Pixel Tablet | Optional |

### Feature Graphic

- Abmessung: 1024 x 500 px
- Inhalt: App-Logo + Tagline "Arbeitszeit erfassen. Einfach. Ueberall."
- Hintergrund: Markenfarbe (Theme.primary)

### Screenshot-Erstellung

```bash
# Emulator Screenshot
adb shell screencap /sdcard/screenshot.png
adb pull /sdcard/screenshot.png

# Oder ueber Android Studio: View -> Tool Windows -> Device File Explorer
```

Gleiche Demo-Daten wie iOS verwenden fuer Konsistenz.

---

## Store Listing

### Metadaten

| Feld | DE | EN |
|------|----|----|
| App-Name | Fakturus Track -- Zeiterfassung | Fakturus Track -- Time Tracking |
| Kurzbeschreibung | Zeiterfassung fuer Deutschland. ArbZG-konform. Offline-first. DATEV-Export. | Time tracking for Germany. Compliant. Offline-first. DATEV export. |
| Kategorie | Business | |
| Tags | Zeiterfassung, Arbeitszeit, Stempeluhr | |
| Preis | Kostenlos (mit IAP) | |
| Kontakt | support@fakturus.com, https://track.fakturus.com | |

### Vollstaendige Beschreibung (DE)

Identisch mit iOS-Beschreibung (siehe epic-05), aber mit HTML-Formatierung:

```html
<b>Fakturus Track</b> -- die native Zeiterfassungs-App fuer Deutschland. ArbZG-konform. Offline-first.

<b>KOSTENLOS STARTEN</b>
- Arbeitszeit per Knopfdruck erfassen
- Pausen automatisch tracken
- Feiertage nach Bundesland

<b>STARTER (ab 2,99 EUR/Monat)</b>
- PDF-Monatsreport & CSV-Export
- Urlaubs- und Krankheitstage
- Widgets & Ueberstunden-Dashboard

<b>PRO (ab 4,99 EUR/Monat)</b>
- DATEV-Export (Lodas-Format)
- Schulferien & Kalender-Integration

<b>WARUM FAKTURUS TRACK?</b>
- ArbZG-konform: Erfuellt die Zeiterfassungspflicht
- Offline-first: Funktioniert ohne Internet
- Nativ fuer Android: Jetpack Compose, Material 3
- Made in Germany: Daten in der EU
- Kein Tracking, keine Werbung
```

---

## Data Safety Section

### Daten die erhoben werden

| Datentyp | Erhoben | Geteilt | Zweck |
|----------|---------|---------|-------|
| Name | Ja | Nein | Account-Identifikation |
| E-Mail | Ja | Nein | Account, Login |
| Arbeitszeiten | Ja | Nein | App-Kernfunktion |
| Pausen | Ja | Nein | App-Kernfunktion |
| Urlaubstage | Ja | Nein | App-Kernfunktion |
| Krankheitstage | Ja | Nein | App-Kernfunktion |
| Geraete-ID | Ja | Nein | Synchronisation |
| Crash-Logs | Ja (anonym) | Nein | Fehlerbehebung |

### Daten die NICHT erhoben werden

- Standort
- Fotos / Videos / Dateien
- Kontakte
- Finanzinformationen (Zahlungen via Google Play)
- Browserverlauf
- SMS / Anrufliste
- Kalender (lokal via Intent, nicht ueber API)

### Sicherheit

- Verschluesselung bei Uebertragung: Ja (HTTPS)
- Verschluesselung gespeicherter Daten: Ja (Android Keystore fuer Tokens)
- Loeschmoeglichkeit: Ja (Account-Loeschung ueber Support oder In-App)

### Datenweitergabe

- Keine Weitergabe an Dritte
- Kein Verkauf von Daten

---

## Content Rating (IARC)

Fragebogen-Antworten:

| Frage | Antwort |
|-------|---------|
| Gewalt | Nein |
| Sexuelle Inhalte | Nein |
| Sprache | Nein |
| Drogen | Nein |
| In-App-Kaeufe | Ja |
| User-Generated Content | Nein |
| Werbung | Nein |

**Ergebnis**: PEGI 3 / Everyone

---

## Target Audience

- Zielgruppe: Erwachsene (18+)
- Keine Kinder-Zielgruppe (Business-App)
- Kein COPPA-relevanter Inhalt

---

## Ads Declaration

- "Nein, diese App enthaelt keine Werbung"

---

## Technische Anforderungen

| Feld | Wert |
|------|------|
| minSdk | 33 (Android 13) |
| targetSdk | 35 (aktuell) |
| Format | AAB (Android App Bundle) |
| App Signing | Google Play App Signing aktiviert |
| ProGuard/R8 | Aktiviert fuer Release |

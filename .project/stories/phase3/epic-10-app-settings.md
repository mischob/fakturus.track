# EPIC 10: App-Einstellungen & Rechtliches

## Ziel

Der Einstellungen-Tab wird um App-spezifische Einstellungen erweitert: Dark Mode Auswahl, Benachrichtigungs-Einstellungen, App-Versionsinformation und rechtliche Hinweise (Datenschutz, Impressum, Lizenzen). Dies ist Store-Pflicht (Privacy Policy Link, Impressum fuer deutsche Apps).

## Abhaengigkeiten

- **E01 (Dark Mode)**: Die Dark-Mode-Logik muss stehen, damit der Setting-Toggle funktioniert
- **Phase 2 E02**: Settings-Screen muss existieren (neue Sektion wird hinzugefuegt)

---

## Stories

### P3-E10-S01: iOS App-Einstellungen Sektion

**Als** Nutzer
**moechte ich** App-spezifische Einstellungen vornehmen koennen,
**damit** ich die App an meine Praeferenzen anpassen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P3-E01-S01 (Dark Mode), Phase 2 E02 (Settings-Screen)
**Parallelisierbar mit**: P3-E10-S02, P3-E02-*, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Neue Sektion "APP" im Einstellungen-Screen (unterhalb der bestehenden Sektionen):
  - **Erscheinungsbild**: Picker mit "System", "Hell", "Dunkel"
  - **Benachrichtigungen**: Toggle fuer ArbZG-Hinweise (Standard: an)
  - **Personalnummer** (optional): Text-Eingabefeld fuer DATEV-Export
  - **Version**: "1.0.0 (Build 42)" (read-only)
  - **Datenschutzerklaerung**: Link oeffnet WebView oder Safari
  - **Impressum**: Link oeffnet WebView oder Safari
  - **Open-Source-Lizenzen**: Link zu Lizenzen-Screen
- [ ] Erscheinungsbild-Auswahl wird sofort angewendet (`.preferredColorScheme()`)
- [ ] Auswahl wird lokal gespeichert (UserDefaults, NICHT in Backend-Settings)
- [ ] Personalnummer wird in Backend-Settings synchronisiert (oder lokal gespeichert)
- [ ] Given der Nutzer waehlt "Dunkel"
  When die Auswahl gespeichert wird
  Then wechselt die App sofort in den Dark Mode
  And beim naechsten App-Start ist der Dark Mode weiterhin aktiv

**Technische Hinweise**:
- Appearance: `UserDefaults` Key "appearance" mit Werten "system"/"light"/"dark"
- SwiftUI: `.preferredColorScheme(viewModel.colorScheme)` auf Root-View
- Version: `Bundle.main.infoDictionary?["CFBundleShortVersionString"]` + `CFBundleVersion`
- Datenschutz/Impressum: `Link(destination: URL(...))` oder `SFSafariViewController`
- Lizenzen: Eigener Screen mit Open-Source-Bibliotheken (MSAL, etc.)

---

### P3-E10-S02: Android App-Einstellungen Sektion

**Als** Nutzer
**moechte ich** App-spezifische Einstellungen vornehmen koennen,
**damit** ich die App an meine Praeferenzen anpassen kann.

**Plattform**: Android
**Abhaengigkeiten**: P3-E01-S02 (Dark Mode), Phase 2 E02 (Settings-Screen)
**Parallelisierbar mit**: P3-E10-S01, P3-E02-*, P3-E05-*, P3-E07-*, P3-E08-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Neue Sektion "APP" im Einstellungen-Screen:
  - **Erscheinungsbild**: Auswahl "System", "Hell", "Dunkel"
  - **Benachrichtigungen**: Toggle fuer ArbZG-Hinweise
  - **Personalnummer**: Text-Eingabefeld
  - **Version**: Build-Info (read-only)
  - **Datenschutzerklaerung**: Link oeffnet CustomTabs oder Browser
  - **Impressum**: Link oeffnet CustomTabs oder Browser
  - **Open-Source-Lizenzen**: Link zu Lizenzen-Screen
- [ ] Erscheinungsbild: `AppCompatDelegate.setDefaultNightMode(MODE_NIGHT_*)` oder Compose Theme Override
- [ ] Auswahl in DataStore gespeichert
- [ ] Given der Nutzer waehlt "Dunkel"
  When die Auswahl gespeichert wird
  Then wechselt die App sofort in den Dark Mode

**Technische Hinweise**:
- Night Mode: `AppCompatDelegate.setDefaultNightMode(MODE_NIGHT_FOLLOW_SYSTEM/YES/NO)`
- Version: `BuildConfig.VERSION_NAME` + `BuildConfig.VERSION_CODE`
- Datenschutz/Impressum: `CustomTabsIntent` fuer In-App-Browser
- Lizenzen: `com.google.android.gms:oss-licenses-plugin` oder manuell

---

### P3-E10-S03: Datenschutzerklaerung & Impressum (Web-Inhalte)

**Als** Nutzer
**moechte ich** die Datenschutzerklaerung und das Impressum in der App einsehen koennen,
**damit** ich weiss wie meine Daten verarbeitet werden (DSGVO-Pflicht).

**Plattform**: Web (Inhalte werden von iOS und Android verlinkt)
**Abhaengigkeiten**: Keine technischen Abhaengigkeiten
**Parallelisierbar mit**: Alle Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Datenschutzerklaerung unter `https://track.fakturus.com/privacy` erreichbar
  - Inhalt: Verantwortliche Stelle, Art der Daten, Zweck, Rechtsgrundlage, Speicherdauer, Betroffenenrechte
  - DSGVO-konform (Art. 13/14 DSGVO)
  - Azure AD B2C Datenverarbeitung erwaehnt
  - Keine Tracking-SDKs -> explizit erwaehnen
- [ ] Impressum unter `https://track.fakturus.com/imprint` erreichbar
  - Inhalt nach TMG / DDG: Name, Anschrift, Kontakt, USt-ID
- [ ] Beide Seiten sind auf Deutsch und Englisch verfuegbar
- [ ] Responsive Design (lesbar auf Mobilgeraeten)
- [ ] URLs in den Apps hinterlegt und erreichbar

**Technische Hinweise**:
- Statische Webseiten (kann in bestehendes Web-Hosting integriert werden)
- App Store und Google Play benoetigen Privacy Policy URL bei App-Einreichung
- Alternativ: Markdown-Dateien in der App bundlen (offline verfuegbar, aber schwerer zu aktualisieren)

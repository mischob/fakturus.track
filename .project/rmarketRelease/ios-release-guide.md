# iOS App Release Guide -- Fakturus Track mit In-App Subscription

**Stand:** April 2026
**App:** Fakturus Track (Zeiterfassung)
**Bundle ID:** `com.fakturus.track`
**Widget Bundle ID:** `com.fakturus.track.widget`
**Deployment Target:** iOS 17.0
**Xcode:** 16.0+, Swift 6.0

---

## Inhaltsverzeichnis

1. [Vorbereitung in App Store Connect](#1-vorbereitung-in-app-store-connect)
2. [Abo (Subscription) einrichten](#2-abo-subscription-einrichten)
3. [App-Informationen pflegen](#3-app-informationen-pflegen)
4. [Build hochladen](#4-build-hochladen)
5. [Review einreichen](#5-review-einreichen)
6. [Nach dem Review](#6-nach-dem-review)

---

## 1. Vorbereitung in App Store Connect

### 1.1 Apple Developer Account pruefen

- [ ] Apple Developer Program Mitgliedschaft aktiv (99 USD/Jahr)
- [ ] Zahlungsinformationen in App Store Connect aktuell (Bankverbindung fuer Abo-Auszahlungen)
- [ ] Steuerliche Vereinbarungen ("Agreements, Tax, and Banking") vollstaendig ausgefuellt
  - **Wichtig:** Fuer bezahlte Apps und In-App Purchases muss der Vertrag "Paid Applications" akzeptiert und mit Bankdaten + Steuerinformationen vervollstaendigt werden. Ohne diesen Vertrag koennen keine Abos angeboten werden.

### 1.2 App in App Store Connect anlegen

1. Oeffne [App Store Connect](https://appstoreconnect.apple.com)
2. Gehe zu "My Apps" > "+" > "New App"
3. Konfiguration:
   - **Platforms:** iOS
   - **Name:** `Fakturus Track` (muss im gesamten App Store eindeutig sein)
   - **Primary Language:** German
   - **Bundle ID:** `com.fakturus.track` auswaehlen (muss vorher im Developer Portal registriert sein)
   - **SKU:** `fakturus-track` (interne Referenz, nicht oeffentlich sichtbar)
   - **User Access:** Full Access (oder Team-spezifisch einschraenken)

### 1.3 Bundle ID und Capabilities im Developer Portal

1. Oeffne [Apple Developer Portal](https://developer.apple.com/account/resources/identifiers/list)
2. Pruefe/erstelle die Bundle IDs:
   - `com.fakturus.track` (Haupt-App)
   - `com.fakturus.track.widget` (WidgetKit Extension)
3. Aktiviere folgende Capabilities fuer `com.fakturus.track`:
   - **App Groups** (Gruppe: `group.com.fakturus.track` -- wird fuer Widget-Kommunikation benoetigt)
   - **In-App Purchase** (muss explizit aktiviert sein fuer Subscriptions)
   - **Keychain Sharing** (fuer MSAL-Auth: `$(AppIdentifierPrefix)com.fakturus.track`)
   - **Background Modes > Background fetch** (fuer Sync)
   - **Associated Domains** (falls Universal Links fuer B2C Login benoetigt)

### 1.4 Signing und Provisioning Profiles

**Empfehlung: Automatic Signing in Xcode verwenden.** Das ist der einfachste Weg und reicht fuer unseren Fall aus.

Falls manuelles Signing noetig ist:

1. **Zertifikate erstellen** (Developer Portal > Certificates):
   - "Apple Distribution" Zertifikat fuer App Store Releases
   - Zertifikat in Keychain auf dem Build-Mac installieren

2. **Provisioning Profiles erstellen** (Developer Portal > Profiles):
   - **App Store Distribution Profile** fuer `com.fakturus.track`
   - **App Store Distribution Profile** fuer `com.fakturus.track.widget`
   - Beide Profile herunterladen und per Doppelklick in Xcode installieren

3. **In Xcode pruefen:**
   - Target "FakturusTrack" > Signing & Capabilities > Team auswaehlen
   - Target "FakturusTrackWidget" > gleiche Einstellungen
   - Sicherstellen, dass alle Entitlements korrekt sind (Entitlements-Datei: `FakturusTrack/FakturusTrack.entitlements`)

---

## 2. Abo (Subscription) einrichten

### 2.1 Subscription Group erstellen

In App Store Connect > App > "Subscriptions" (linke Navigation):

1. Klicke "Create" bei Subscription Groups
2. **Reference Name:** `Fakturus Track Premium`
3. **Subscription Group Localization:**
   - Sprache: Deutsch
   - Display Name: `Fakturus Track Premium`
   - (Optional) App Name Override: leer lassen

**Wichtig:** Alle Abo-Stufen innerhalb einer Gruppe sind austauschbar (Upgrade/Downgrade). Fuer unser Modell reicht eine einzige Gruppe.

### 2.2 Abo-Plaene definieren

Innerhalb der Subscription Group folgende Plaene erstellen:

#### Plan 1: Starter (Monatlich)

| Feld | Wert |
|------|------|
| Reference Name | `starter_monthly` |
| Product ID | `com.fakturus.track.starter.monthly` |
| Subscription Duration | 1 Month |
| Subscription Price | 2,99 EUR (Tier 3) |
| Family Sharing | Aus |
| Offer Codes | Optional spaeter aktivieren |

#### Plan 2: Starter (Jaehrlich)

| Feld | Wert |
|------|------|
| Reference Name | `starter_yearly` |
| Product ID | `com.fakturus.track.starter.yearly` |
| Subscription Duration | 1 Year |
| Subscription Price | 29,99 EUR (~17% Ersparnis gegenueber monatlich) |

#### Plan 3: Pro (Monatlich)

| Feld | Wert |
|------|------|
| Reference Name | `pro_monthly` |
| Product ID | `com.fakturus.track.pro.monthly` |
| Subscription Duration | 1 Month |
| Subscription Price | 4,99 EUR (Tier 5) |

#### Plan 4: Pro (Jaehrlich)

| Feld | Wert |
|------|------|
| Reference Name | `pro_yearly` |
| Product ID | `com.fakturus.track.pro.yearly` |
| Subscription Duration | 1 Year |
| Subscription Price | 49,99 EUR (~17% Ersparnis) |

**Hinweis zum Team-Tier:** Der Team-Tarif (3,99 EUR/User/Monat) ist ein Multi-User-Modell und wird ueber die Web-App via Stripe abgewickelt, nicht ueber Apple IAP. Im App Store werden nur die Einzelnutzer-Tarife (Starter, Pro) angeboten.

### 2.3 Ranking der Abo-Stufen

In App Store Connect kann man die Reihenfolge der Abos innerhalb der Subscription Group festlegen. Diese Reihenfolge bestimmt, was als Upgrade bzw. Downgrade gilt:

1. Pro Yearly (hoechste Stufe)
2. Pro Monthly
3. Starter Yearly
4. Starter Monthly (niedrigste Stufe)

**Upgrade-Regeln (Apple Standard):**
- Upgrade: Sofort wirksam, anteilige Verrechnung
- Downgrade: Wird zum Ende der aktuellen Laufzeit wirksam
- Crossgrade (gleiche Stufe, andere Laufzeit): Zum Ende der Laufzeit

### 2.4 Preise festlegen

Fuer jeden Plan:

1. Klicke auf den Plan > "Subscription Prices"
2. "Add Subscription Price" > Waehle EUR als Basiswaehrung
3. Apple berechnet automatisch aequivalente Preise fuer andere Laender/Waehrungen
4. Pruefe die generierten Preise fuer DE, AT, CH (Kernmaerkte)
5. Bei Bedarf einzelne Laenderpreise manuell anpassen

**Beachte:** Apple behaelt 30% Provision im ersten Jahr, ab dem zweiten Jahr eines durchgehenden Abonnenten 15% (App Store Small Business Program: generell 15% bei unter 1 Mio. USD Jahresumsatz -- Antrag stellen!).

### 2.5 Abo-Beschreibungen und Marketingtexte

Fuer jeden Plan muessen Localized Descriptions hinterlegt werden:

**Starter Monthly:**
- Display Name: `Starter`
- Description: `Zeiterfassung mit Ueberstundenberechnung und Pausen. Monatlich kuendbar.`

**Starter Yearly:**
- Display Name: `Starter Jahresabo`
- Description: `Alle Starter-Funktionen zum Vorteilspreis. Spare 17% gegenueber dem Monatsabo.`

**Pro Monthly:**
- Display Name: `Pro`
- Description: `Alle Funktionen: Zeiterfassung, Urlaub, Export, Feiertage aller Bundeslaender. Monatlich kuendbar.`

**Pro Yearly:**
- Display Name: `Pro Jahresabo`
- Description: `Alle Pro-Funktionen zum Vorteilspreis. Spare 17% gegenueber dem Monatsabo.`

### 2.6 Introductory Offers und Promotional Offers (optional)

Fuer den Launch empfohlen:

- **Free Trial:** 7 Tage kostenlos testen (unter jedem Plan > "Introductory Offers" konfigurierbar)
- Typ: "Free" fuer 7 Tage
- Jeder Nutzer kann ein Free Trial pro Subscription Group nur einmal nutzen

Spaeter moeglich:
- Promotional Offers (spezielle Preise fuer bestimmte Nutzergruppen, benoetigen Server-seitige Signierung)
- Offer Codes (einloesbare Codes fuer Rabatte)

### 2.7 Sandbox-Testing fuer Abos

#### Sandbox-Accounts erstellen

1. App Store Connect > "Users and Access" > "Sandbox" (Tab oben)
2. "+" > Sandbox-Tester anlegen:
   - Vorname, Nachname, E-Mail (darf kein echtes Apple-ID sein)
   - Passwort festlegen
   - Region: Deutschland
3. Mehrere Tester anlegen (mindestens 3-4 fuer verschiedene Szenarien)

#### Sandbox-Testumgebung konfigurieren

In App Store Connect > Sandbox > "Manage Testers":
- **Beschleunigte Erneuerungen aktivieren:** Im Sandbox werden Abo-Zyklen verkuerzt:
  - 1 Monat = 5 Minuten
  - 1 Jahr = 1 Stunde
  - Abos erneuern sich automatisch bis zu 12 Mal, dann stoppen sie
- **Subscription Renewal Rate** auf dem Testgeraet unter Einstellungen > App Store > Sandbox-Account pruefen

#### Test-Szenarien (alle muessen bestanden werden)

| # | Szenario | Erwartetes Verhalten |
|---|----------|---------------------|
| 1 | Neukauf Starter Monthly | Payment Sheet erscheint, Kauf erfolgreich, Features freigeschaltet |
| 2 | Neukauf Pro Monthly | Wie #1, alle Pro-Features verfuegbar |
| 3 | Upgrade Starter > Pro | Sofortige Umstellung, anteilige Verrechnung |
| 4 | Downgrade Pro > Starter | Wirksam zum Ende der Laufzeit, Pro-Features bis dahin aktiv |
| 5 | Kuendigung | Abo laeuft bis Laufzeitende weiter, danach kein Zugang zu Premium-Features |
| 6 | Wiederherstellung (Restore) | "Kaeufe wiederherstellen" stellt Abo auf neuem Geraet wieder her |
| 7 | Ablauf ohne Erneuerung | Nach Ablauf: App funktioniert im Free-Tier weiter, kein Datenverlust |
| 8 | Billing Retry | Bei fehlgeschlagener Zahlung: Billing Grace Period aktiv, Features bleiben |
| 9 | Free Trial > Conversion | Nach 7-Tage-Trial automatische Umwandlung in Bezahl-Abo |
| 10 | Free Trial > Kuendigung | Kuendigung waehrend Trial: Zugang bis Trial-Ende, keine Belastung |

#### StoreKit Testing in Xcode (lokales Testing)

Fuer schnelleres Testen waehrend der Entwicklung:
1. Xcode > File > New > File > "StoreKit Configuration File"
2. Produkte dort spiegeln (gleiche Product IDs wie in App Store Connect)
3. Im Scheme unter "Run" > "StoreKit Configuration" die Datei auswaehlen
4. Vorteil: Kein Sandbox-Account noetig, kein Netzwerk noetig, volle Kontrolle ueber Transaktionen

---

## 3. App-Informationen pflegen

### 3.1 Screenshots

Apple verlangt Screenshots fuer mindestens ein Geraet. Empfehlung: Fuer alle aktuellen Groessen bereitstellen.

**Erforderliche Screenshot-Groessen:**

| Geraet | Aufloesung | Pflicht? |
|--------|-----------|----------|
| iPhone 6.9" (iPhone 16 Pro Max) | 1320 x 2868 px | Pflicht (groesstes iPhone) |
| iPhone 6.3" (iPhone 16 Pro) | 1206 x 2622 px | Optional (skaliert von 6.9") |
| iPhone 6.1" (iPhone 16) | 1179 x 2556 px | Optional |
| iPad Pro 13" | 2064 x 2752 px | Pflicht falls iPad unterstuetzt |

**Empfohlene Screenshot-Inhalte (5-8 Screenshots pro Geraet):**

1. **Timer-Ansicht:** Laufende Zeiterfassung mit Start/Stop -- Kernfunktion sofort sichtbar
2. **Uebersicht/Dashboard:** Monatliche Ueberstundenberechnung und Saldo
3. **Urlaubsverwaltung:** Urlaubstage planen und Restanspruch sehen
4. **Einstellungen:** Bundesland, Arbeitstage, Wochenstunden -- zeigt Anpassbarkeit
5. **Widget:** Home-Screen Widget fuer schnellen Zugriff
6. **Feiertage/Auswertung:** Automatische Feiertagserkennung fuer alle Bundeslaender
7. **Export:** PDF/CSV Export der Arbeitszeiten (wenn in Release enthalten)
8. **Pausen:** Pausenerfassung gemaess Arbeitszeitgesetz

**Tipps fuer gute Screenshots:**
- Realistische Demo-Daten verwenden (keine Platzhalter wie "Max Mustermann")
- Hellen Hintergrund verwenden, der zum App-Design passt
- Optional: Screenshot-Rahmen mit Beschriftungen verwenden (z.B. mit Fastlane Frameit)
- Keine Statusbar-Details sichtbar, die ablenken

### 3.2 App-Beschreibung

**App Name:** Fakturus Track
**Subtitle (max 30 Zeichen):** Zeiterfassung & Urlaub

**Promotional Text (max 170 Zeichen, jederzeit aenderbar):**
```
Gesetzeskonforme Zeiterfassung fuer Deutschland. Ueberstunden, Urlaub und Feiertage aller 16 Bundeslaender automatisch berechnet.
```

**Beschreibung (max 4000 Zeichen):**
```
Fakturus Track ist die einfache und gesetzeskonforme Zeiterfassungs-App fuer Arbeitnehmer in Deutschland.

ZEITERFASSUNG
- Start, Stop und Finish -- erfasse deine Arbeitszeit mit einem Tipp
- Automatische Pausenerfassung gemaess Arbeitszeitgesetz
- Nachtraegliche Korrektur und Ergaenzung von Eintraegen
- Offline-faehig: Alle Daten werden lokal gespeichert und automatisch synchronisiert

UEBERSTUNDEN IM BLICK
- Automatische Berechnung deiner Soll-Stunden basierend auf deinem Arbeitszeitmodell
- Monatliche und jaehrliche Ueberstundenuebersicht
- Feiertage aller 16 Bundeslaender automatisch beruecksichtigt
- Unterstuetzung fuer Vollzeit, Teilzeit und individuelle Arbeitstage

URLAUB VERWALTEN
- Urlaubsanspruch und Resturlaub auf einen Blick
- Urlaubstage planen und verwalten
- Korrekte Berechnung bei Teilzeit nach Bundesurlaubsgesetz

FLEXIBEL ANPASSBAR
- Waehle dein Bundesland fuer korrekte Feiertagsberechnung
- Definiere deine Arbeitstage und Wochenstunden
- Unterstuetze verschiedene Arbeitszeitmodelle

WIDGET
- Home-Screen Widget fuer schnellen Zugriff auf die Zeiterfassung
- Aktueller Status und heutige Arbeitszeit immer im Blick

DATENSCHUTZ
- Deine Daten gehoeren dir
- DSGVO-konform
- Server in Deutschland/EU

Das kostenlose Free-Tier beinhaltet grundlegende Zeiterfassung. Starter und Pro erschliessen erweiterte Funktionen wie Ueberstundenberechnung, Urlaubsverwaltung und Export.

Abo-Informationen:
- Starter: 2,99 EUR/Monat oder 29,99 EUR/Jahr
- Pro: 4,99 EUR/Monat oder 49,99 EUR/Jahr
- Die Zahlung wird ueber dein Apple-ID-Konto abgerechnet
- Das Abo verlaengert sich automatisch, wenn es nicht bis 24 Stunden vor Ablauf gekuendigt wird
- Die automatische Verlaengerung kann in den Apple-ID-Kontoeinstellungen verwaltet werden
```

**Wichtig:** Apple verlangt, dass die Beschreibung die Abo-Bedingungen enthaelt (Preis, Laufzeit, automatische Verlaengerung, Kuendigungsfrist). Ohne diese Angaben wird die App abgelehnt.

### 3.3 Keywords (max 100 Zeichen)

```
Zeiterfassung,Arbeitszeit,Stempeluhr,Ueberstunden,Urlaub,Stunden,Arbeit,Tracking,Feiertage
```

Tipps:
- Keine Kommas zaehlen zum Zeichenlimit
- Den App-Namen nicht als Keyword verwenden (zaehlt automatisch)
- Keine Wettbewerbernamen verwenden (Ablehnungsgrund)
- Einzahl und Mehrzahl werden von Apple zusammengefasst

### 3.4 Datenschutzrichtlinie / Privacy Policy URL

- **Privacy Policy URL:** `https://track.fakturus.com/privacy`
- Diese URL muss oeffentlich erreichbar sein und eine vollstaendige Datenschutzerklaerung enthalten
- **Pflichtinhalt** (siehe Phase 5 Legal Compliance, Epic E03):
  - Welche Daten werden erhoben (Arbeitszeiten, E-Mail, Bundesland)
  - Zweck der Datenverarbeitung
  - Rechtsgrundlage (Art. 6 DSGVO)
  - Speicherdauer und Loeschung
  - Rechte der Betroffenen (Auskunft, Loeschung, Widerspruch)
  - Kontaktdaten des Verantwortlichen
  - Hinweis auf Datenverarbeitung durch Apple (IAP-Transaktionen)
  - Drittanbieter: Microsoft Azure B2C (Authentifizierung)
- Muss vor dem Review live sein und auf Deutsch + Englisch vorliegen

### 3.5 Support-URL

- **Support URL:** `https://track.fakturus.com/support` oder `mailto:support@fakturus.com`
- Muss erreichbar sein und dem Nutzer eine Kontaktmoeglichkeit bieten
- Empfehlung: FAQ-Seite mit Kontaktformular oder E-Mail-Adresse

### 3.6 Altersfreigabe (Content Rights / Age Rating)

Im App Store Connect unter "App Information" > "Age Rating":

Apple stellt einen Fragebogen. Fuer Fakturus Track sind alle Antworten voraussichtlich "None/No":
- Cartoon or Fantasy Violence: None
- Realistic Violence: None
- Sexual Content: None
- Profanity: None
- Drugs: None
- Gambling: None
- Horror: None
- Medical/Treatment Information: None
- Unrestricted Web Access: No (die App hat keinen eingebetteten Browser)

**Ergebnis:** Voraussichtlich Rated 4+ (fuer alle Altersgruppen geeignet)

### 3.7 App Privacy Details (Datenschutz-Labels)

Unter "App Privacy" im App Store Connect muessen die Datenkategorien deklariert werden:

| Datenkategorie | Datentyp | Verwendung | Verknuepft mit Identitaet? | Tracking? |
|---------------|----------|-----------|---------------------------|-----------|
| Contact Info | Email Address | App Functionality (Login) | Ja | Nein |
| Identifiers | User ID | App Functionality | Ja | Nein |
| Usage Data | Product Interaction | App Functionality (Arbeitszeiten) | Ja | Nein |
| Purchases | Purchase History | App Functionality (Abo-Status) | Ja | Nein |

**Wichtig:** Wenn kein Third-Party-Tracking (Analytics, Werbung) eingesetzt wird, kann die App als "Does not track users" deklariert werden. Dies ist ein Marketing-Vorteil.

---

## 4. Build hochladen

### 4.1 Vorbereitung in Xcode

1. **Version und Build-Nummer pruefen:**
   - `MARKETING_VERSION` in project.yml: `1.0.0` (wird im App Store angezeigt)
   - `CURRENT_PROJECT_VERSION`: Hochzaehlen bei jedem Upload (1, 2, 3, ...)
   - Nach Aenderung `xcodegen generate` ausfuehren falls XcodeGen verwendet wird

2. **Scheme auf Release setzen:**
   - Xcode > Product > Scheme > Edit Scheme
   - Run: Debug (fuer lokales Testen)
   - Archive: Release (sollte bereits Standard sein)

3. **Signing pruefen:**
   - Beide Targets (FakturusTrack, FakturusTrackWidget) muessen gueltige Signing-Konfiguration haben
   - Team muss korrekt ausgewaehlt sein
   - Provisioning Profile: Automatic oder manuell das Distribution Profile

4. **StoreKit Configuration entfernen:**
   - Falls StoreKit Testing File im Scheme konfiguriert ist: Vor dem Archive-Build die StoreKit Configuration unter Edit Scheme > Run > StoreKit Configuration auf "None" setzen
   - Sonst werden lokale Produkte statt der echten App Store Connect Produkte verwendet

### 4.2 Archive erstellen

1. **Geraet auf "Any iOS Device (arm64)" stellen** (nicht Simulator)
2. **Xcode > Product > Archive**
3. Warten bis der Build abgeschlossen ist (kann einige Minuten dauern)
4. Nach Erfolg oeffnet sich automatisch der Organizer

**Haeufige Build-Fehler:**
- "No signing certificate found": Zertifikate im Developer Portal pruefen, ggf. in Keychain erneuern
- "Provisioning profile doesn't include capability": In-App Purchase Capability im Developer Portal aktivieren
- Widget-Signing-Fehler: Sicherstellen, dass das Widget-Target das gleiche Team verwendet

### 4.3 Build hochladen

**Option A: Ueber Xcode (empfohlen)**

1. Im Organizer das Archive auswaehlen
2. "Distribute App" klicken
3. Methode: "App Store Connect"
4. Destination: "Upload"
5. Distribution Options:
   - [x] Upload your app's symbols (fuer Crash-Reports)
   - [x] Manage Version and Build Number (Xcode korrigiert automatisch)
6. "Upload" klicken
7. Warten bis der Upload abgeschlossen ist

**Option B: Ueber Kommandozeile (CI/CD)**

```bash
# Archive erstellen
xcodebuild archive \
  -project FakturusTrack.xcodeproj \
  -scheme FakturusTrack \
  -archivePath ./build/FakturusTrack.xcarchive \
  -configuration Release \
  CODE_SIGN_IDENTITY="Apple Distribution" \
  -allowProvisioningUpdates

# Exportieren
xcodebuild -exportArchive \
  -archivePath ./build/FakturusTrack.xcarchive \
  -exportPath ./build/export \
  -exportOptionsPlist ExportOptions.plist \
  -allowProvisioningUpdates

# Hochladen
xcrun altool --upload-app \
  -f ./build/export/FakturusTrack.ipa \
  -t ios \
  -u "apple-id@fakturus.com" \
  -p "@keychain:AC_PASSWORD"
```

### 4.4 Build-Verarbeitung abwarten

- Nach dem Upload dauert die Verarbeitung durch Apple ca. 15-30 Minuten
- Status in App Store Connect > App > "TestFlight" oder "Activity" Tab pruefen
- Der Build durchlaeuft automatische Checks:
  - Code Signing Validierung
  - Entitlements Pruefung
  - Binary Analyse
- **Haeufige Verarbeitungsfehler:**
  - "Missing Compliance": Export-Compliance-Frage beantworten (siehe Abschnitt 5)
  - "Invalid Binary": Meist Signing-Probleme, Fehlermeldung genau lesen

### 4.5 TestFlight (empfohlen vor dem Review)

Vor der Einreichung zum App Store Review den Build ueber TestFlight testen:

1. **Internes Testing:** Sofort verfuegbar (bis 100 Tester, Apple-ID muss im Team sein)
2. **Externes Testing:** Benoetigt kurzes Beta App Review (1-2 Tage)
3. Mindestens die Sandbox-Testszenarien aus Abschnitt 2.7 auf echtem Geraet durchlaufen
4. Abo-Kauf im Sandbox testen (TestFlight-Builds verwenden automatisch die Sandbox-Umgebung)

---

## 5. Review einreichen

### 5.1 Export Compliance

Beim ersten Build wird nach Exportregeln gefragt:

- **Does your app use encryption?** Ja -- die App verwendet HTTPS (TLS) fuer die API-Kommunikation und MSAL fuer die Authentifizierung
- **Is your app exempt?** Ja -- die App faellt unter die Ausnahme fuer Standard-Verschluesselung (HTTPS/TLS)
- Im Info.plist kann man dies dauerhaft hinterlegen:
  ```xml
  <key>ITSAppUsesNonExemptEncryption</key>
  <false/>
  ```
  Dies erspart die Frage bei jedem neuen Build.

### 5.2 Review-Informationen

Unter "App Store" > Version > "App Review Information":

**Kontakt:**
- Vorname, Nachname, Telefonnummer, E-Mail des Ansprechpartners

**Demo-Account (wichtig!):**

Apple-Reviewer muessen die App testen koennen. Bereitstellen:
- **Username:** `review@fakturus.com` (dedizierter Demo-Account in Azure B2C)
- **Password:** Sicheres Passwort angeben
- Der Account muss:
  - Funktionierenden Login ermoeglichen (B2C muss im Review-Netz erreichbar sein)
  - Demo-Daten enthalten (einige Arbeitszeiten, Urlaubstage)
  - Alle Features zeigen koennen, die in der Beschreibung erwaehnt werden

**Sign-in Required:** Ja

### 5.3 Abo-spezifische Review-Hinweise

Im Feld "Notes for Review" folgende Informationen bereitstellen:

```
Subscription Testing:
- The app offers a Free tier (no subscription required) with basic time tracking.
- Starter and Pro subscriptions unlock additional features.
- To test the subscription flow: Tap "Upgrade" in Settings, select a plan, and complete the purchase.
- The demo account has an active Pro subscription for testing all features.
- Free Trial: New users get a 7-day free trial before being charged.

Login:
- The app uses Microsoft Azure AD B2C for authentication.
- Demo credentials: review@fakturus.com / [Password]
- Login via email only (no social login required for review).

Note: This is a time tracking app for the German market. 
It calculates working hours, overtime, and vacation days 
according to German labor law (Arbeitszeitgesetz).
```

### 5.4 Haeufige Ablehnungsgruende und wie man sie vermeidet

#### Grund 1: Fehlende Restore-Funktion (Guideline 3.1.1)
- **Problem:** Apple verlangt einen Button "Kaeufe wiederherstellen"
- **Loesung:** Im Abo-Bereich der App einen "Kaeufe wiederherstellen"-Button implementieren, der `AppStore.sync()` (StoreKit 2) aufruft
- **Wo:** Gut sichtbar auf der Subscription-Seite oder in den Einstellungen

#### Grund 2: Fehlende Abo-Informationen in der App (Guideline 3.1.2)
- **Problem:** Innerhalb der App muessen vor dem Kauf folgende Infos angezeigt werden:
  - Preis und Laufzeit
  - Hinweis auf automatische Verlaengerung
  - Link zur Datenschutzerklaerung
  - Link zu den Nutzungsbedingungen (Terms of Use / EULA)
- **Loesung:** Auf der Paywall/Subscription-Seite alle Infos anzeigen, inkl. Links

#### Grund 3: Unklarer Mehrwert des Abos (Guideline 3.1.1)
- **Problem:** Reviewer versteht nicht, was das Abo bietet vs. die kostenlose Version
- **Loesung:** Klare Feature-Vergleichstabelle auf der Paywall anzeigen (Free vs. Starter vs. Pro)

#### Grund 4: App funktioniert nicht ohne Abo (Guideline 3.1.1)
- **Problem:** Wenn die App ohne Abo komplett unbrauchbar ist, wird sie abgelehnt
- **Loesung:** Das Free-Tier bietet grundlegende Zeiterfassung. Der Reviewer kann die App auch ohne Kauf testen.

#### Grund 5: Login-Probleme (Guideline 2.1)
- **Problem:** Der Reviewer kann sich nicht einloggen (B2C nicht erreichbar, Account-Probleme)
- **Loesung:**
  - Demo-Account vorher testen (auch aus US-Netzwerk)
  - B2C-Endpoint nicht geo-beschraenken
  - Backup: Video des Login-Flows als Attachment mitliefern

#### Grund 6: Datenschutz-Links fehlen oder sind nicht erreichbar (Guideline 5.1.1)
- **Problem:** Privacy Policy und Terms of Use URLs sind nicht erreichbar
- **Loesung:** URLs vor Einreichung testen. Muessen ohne Login oeffentlich erreichbar sein auf `track.fakturus.com/privacy` und `track.fakturus.com/terms`

#### Grund 7: Irregulaere Nutzung von Background Modes (Guideline 2.5.4)
- **Problem:** Die App deklariert Background Fetch, nutzt es aber nicht sinnvoll
- **Loesung:** Background Fetch wird fuer die Datensynchronisation verwendet. Dies in den Review Notes erlaeutern.

#### Grund 8: Fehlende Subscription Management (Guideline 3.1.2(a))
- **Problem:** In der App muss ein Link zu den Apple-Abo-Einstellungen vorhanden sein
- **Loesung:** In den Einstellungen einen "Abo verwalten"-Link einbauen, der direkt zu den Apple-Subscription-Settings fuehrt:
  ```swift
  if let url = URL(string: "https://apps.apple.com/account/subscriptions") {
      UIApplication.shared.open(url)
  }
  ```

### 5.5 Checkliste vor der Einreichung

- [ ] Demo-Account funktioniert (Login testen, auch aus anderem Netzwerk)
- [ ] Privacy Policy URL erreichbar und inhaltlich korrekt
- [ ] Terms of Use URL erreichbar
- [ ] Support URL erreichbar
- [ ] "Kaeufe wiederherstellen"-Button vorhanden und funktioniert
- [ ] Abo-Informationen (Preis, Laufzeit, auto-renewal) in der App vor dem Kauf sichtbar
- [ ] Link zu Privacy Policy und Terms in der App (Paywall) vorhanden
- [ ] Link zu Apple Subscription Management in den Einstellungen
- [ ] Free-Tier funktioniert ohne Abo-Kauf
- [ ] Alle Screenshots aktuell und korrekt
- [ ] App-Beschreibung enthaelt Abo-Bedingungen
- [ ] Export Compliance beantwortet
- [ ] Build in TestFlight getestet
- [ ] Sandbox-Abo-Testszenarien bestanden
- [ ] Keine Crashes in den letzten TestFlight-Versionen

### 5.6 Einreichung zur Pruefung

1. In App Store Connect > App > App Store Tab
2. Die vorbereitete Version auswaehlen
3. Build zuweisen (den verarbeiteten Build auswaehlen)
4. Alle Pflichtfelder ausfuellen (rot markierte Felder)
5. "Add for Review" klicken
6. Release-Option waehlen (siehe Abschnitt 6.1)
7. "Submit to App Review" klicken

**Review-Dauer:**
- Erster Review: In der Regel 24-48 Stunden (kann bis zu 7 Tage dauern)
- Updates: Meist schneller (24 Stunden)
- Ablehnungen: Nach Korrektur erneut einreichen, geht oft schneller
- Status unter "Activity" oder per E-Mail-Benachrichtigung verfolgen

---

## 6. Nach dem Review

### 6.1 Release-Optionen

Bei der Einreichung waehlt man eine der drei Optionen:

| Option | Beschreibung | Empfehlung |
|--------|-------------|------------|
| **Automatically release this version** | Sofort nach Genehmigung im Store verfuegbar | Fuer Updates |
| **Manually release this version** | Nach Genehmigung manuell freigeben | Fuer den Erstrelease empfohlen |
| **Scheduled release** | Freigabe zu einem bestimmten Datum/Uhrzeit | Fuer koordinierte Launches |

**Empfehlung fuer den Erstrelease:** "Manually release" waehlen. So kann man:
- Den Release-Zeitpunkt selbst bestimmen
- Letzte Vorbereitungen treffen (Landingpage, Social Media, etc.)
- Sicherstellen, dass Support erreichbar ist

### 6.2 Nach der Genehmigung

1. **Manuelle Freigabe:** In App Store Connect > App > "Release This Version" klicken
2. **Propagation:** Nach Freigabe dauert es ca. 2-24 Stunden, bis die App weltweit im Store erscheint
3. **App Store Listing pruefen:** Die App im App Store suchen und pruefen:
   - Wird der Name korrekt angezeigt?
   - Sind alle Screenshots sichtbar?
   - Stimmt die Beschreibung?
   - Funktioniert die Datenschutz-URL?

### 6.3 Monitoring nach Release

#### Sofortiges Monitoring (erste 48 Stunden)

- [ ] **Crash-Reports pruefen:** Xcode Organizer > Crashes, oder App Store Connect > Analytics
- [ ] **Abo-Transaktionen pruefen:** App Store Connect > "Sales and Trends" (verfuegbar ab Tag nach Release)
- [ ] **Kundenbewertungen lesen:** App Store Connect > "Ratings and Reviews" -- schnell auf negative Bewertungen reagieren
- [ ] **Login funktioniert:** B2C-Endpoints erreichbar, keine erhoehte Fehlerrate
- [ ] **API-Health pruefen:** Backend-Monitoring auf api.track.fakturus.com
- [ ] **Sandbox vs. Production:** Sicherstellen, dass die App die Production-Umgebung nutzt (nicht Sandbox)

#### Laufendes Monitoring

- **App Store Connect Analytics:** Downloads, aktive Geraete, Abo-Conversions, Churn Rate
- **Financial Reports:** Umsatz, Apple-Provision, Auszahlungen (monatlich)
- **Subscription Dashboard:** Aktive Abos, Trial Conversions, Kuendigungen, Billing Issues
- **Crash Reports:** Regelmaessig pruefen, Crash-freie Nutzerrate sollte >99.5% sein

#### Server Notifications fuer Subscriptions (empfohlen)

Apple bietet App Store Server Notifications v2, um ueber Abo-Ereignisse informiert zu werden:

1. In App Store Connect > App > "App Information" > "App Store Server Notifications"
2. Production URL hinterlegen: `https://api.track.fakturus.com/v1/apple/notifications`
3. Sandbox URL hinterlegen: `https://api.track.fakturus.com/v1/apple/notifications/sandbox`
4. Version: V2 (empfohlen)

Wichtige Notification-Typen:
- `DID_RENEW` -- Abo wurde erfolgreich verlaengert
- `DID_FAIL_TO_RENEW` -- Zahlung fehlgeschlagen
- `EXPIRED` -- Abo abgelaufen
- `REFUND` -- Rueckerstattung durch Apple
- `SUBSCRIBED` -- Neues Abo abgeschlossen
- `GRACE_PERIOD_EXPIRED` -- Billing Grace Period abgelaufen

Diese Notifications ermoeglichen es dem Backend, den Abo-Status der Nutzer aktuell zu halten, ohne auf Client-seitige Validierung allein zu vertrauen.

### 6.4 Wichtige nachgelagerte Schritte

- [ ] **App Store Small Business Program beantragen** (falls Umsatz < 1 Mio. USD/Jahr): Reduziert Apple-Provision von 30% auf 15%. Antrag unter [developer.apple.com/programs/small-business](https://developer.apple.com/app-store/small-business-program/)
- [ ] **Phased Release erwaegen** fuer zukuenftige Updates: Rollout ueber 7 Tage an zunehmendem Nutzeranteil, um Probleme frueh zu erkennen
- [ ] **Bewertungs-Prompt einbauen:** `SKStoreReviewController.requestReview()` nach positiver Interaktion (z.B. nach 7 Tagen Nutzung), maximal 3x pro Jahr
- [ ] **In-App Events** in App Store Connect pflegen (z.B. "Jetzt 7 Tage kostenlos testen") fuer bessere Sichtbarkeit im Store

---

## Anhang: Zeitplan und Abhaengigkeiten

### Vorbedingungen fuer den Release

| Abhaengigkeit | Status | Blockierend? |
|--------------|--------|-------------|
| Apple Developer Account aktiv | Pruefen | Ja |
| Paid Applications Agreement | Pruefen | Ja (fuer Abos) |
| Privacy Policy live auf track.fakturus.com/privacy | Phase 5 / Phase 6 | Ja |
| Terms of Use live auf track.fakturus.com/terms | Phase 5 / Phase 6 | Ja |
| Imprint live auf track.fakturus.com/imprint | Phase 5 / Phase 6 | Ja (fuer DE Markt) |
| Demo-Account fuer Review | Erstellen | Ja |
| Backend Server Notifications Endpoint | Backend-Arbeit | Nein (kann nach Launch) |
| App Store Small Business Program Antrag | Administrativ | Nein (aber empfohlen vor Launch) |

### Geschaetzter Zeitaufwand

| Schritt | Dauer |
|---------|-------|
| App Store Connect Setup + Abo-Konfiguration | 1-2 Tage |
| Screenshots erstellen | 1 Tag |
| Texte und Metadaten pflegen | 0.5 Tage |
| Sandbox-Testing aller Abo-Szenarien | 1-2 Tage |
| Build + Upload + TestFlight | 0.5 Tage |
| Review-Vorbereitung + Einreichung | 0.5 Tage |
| Apple Review (Wartezeit) | 1-7 Tage |
| **Gesamt (ohne Review-Wartezeit)** | **4-6 Arbeitstage** |

### Bei Ablehnung

1. Ablehnungsgrund genau lesen (Resolution Center in App Store Connect)
2. Im Resolution Center Rueckfragen stellen, falls der Grund unklar ist
3. Problem beheben (Code-Aenderung oder Metadaten-Anpassung)
4. Neuen Build hochladen (falls Code-Aenderung) oder nur Metadaten aktualisieren
5. Erneut einreichen -- erneute Reviews sind in der Regel schneller
6. Tipp: Bei wiederholten Ablehnungen kann ein "App Review Board Appeal" eingereicht werden

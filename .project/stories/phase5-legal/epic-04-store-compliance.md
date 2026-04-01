# EPIC 04: Store Compliance

## Ziel

Sicherstellung, dass alle rechtlichen Anforderungen der App Stores (Apple App Store und Google Play Store) in Bezug auf Datenschutz, AGB und Transparenz erfuellt sind. Ohne diese Compliance wird die App beim Review abgelehnt.

## Abhaengigkeiten

- **E01**: Legal Pages muessen live und erreichbar sein (URLs werden in den Stores hinterlegt)
- **E03**: Texte muessen final sein (Content wird referenziert)
- **Phase 4 E05/E06**: Store-Listings muessen bereits angelegt sein

---

## Stories

### P5-E04-S01: Apple App Store Privacy Compliance

**Als** App-Anbieter
**moechte ich** alle Apple-Datenschutzanforderungen erfuellen,
**damit** die App den Review-Prozess besteht und im App Store veroeffentlicht werden kann.

**Plattform**: App Store Connect
**Abhaengigkeiten**: E01-S01 (Privacy URL live), E03-S01 (Datenschutztext final)
**Parallelisierbar mit**: P5-E04-S02 (Google)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Privacy Policy URL** ist in App Store Connect hinterlegt
  - URL: `https://track.fakturus.com/privacy`
  - URL ist oeffentlich erreichbar und liefert gueltiges HTML
- [ ] **App Privacy Details** (Datenschutz-Label) sind vollstaendig ausgefuellt:
  - "Data Used to Track You": Nein (kein Cross-App-Tracking)
  - "Data Linked to You":
    - Contact Info: E-Mail (fuer Account)
    - Identifiers: User ID
  - "Data Not Linked to You": Keine
  - Korrekte Zuordnung aller Datenkategorien zu Zwecken:
    - "App Functionality" fuer Arbeitszeiten, Pausen, Urlaub
    - "Product Personalization" fuer Einstellungen (Bundesland, Arbeitstage)
- [ ] **App Tracking Transparency (ATT)**: Nicht erforderlich (kein Tracking-SDK)
  - Bestaetigung: App enthaelt KEINEN Code der `ATTrackingManager` benoetigt
- [ ] **Guideline 5.1.1 (Data Collection and Storage)**:
  - [ ] Privacy Policy erklaert welche Daten erhoben werden
  - [ ] Privacy Policy erklaert wie Daten gespeichert werden
  - [ ] Datenloeschung ist moeglich (Account-Loeschung in der App)
- [ ] **Guideline 5.1.2 (Data Use and Sharing)**:
  - [ ] Keine Datenweitergabe an Dritte (ausser Azure fuer Hosting)
  - [ ] Keine Nutzung der Daten fuer Werbung
- [ ] **Account Deletion Requirement** (seit 2022):
  - [ ] App bietet Account-Loeschung innerhalb der App an
  - [ ] Loeschung loescht alle serverseitig gespeicherten Nutzerdaten
  - [ ] In den Einstellungen unter "Konto" erreichbar

**Technische Hinweise**:
- App Store Connect: "App Privacy" Tab in der App-Version
- Apple prueft die Privacy Details manuell -- falsche Angaben fuehren zur Ablehnung
- Account-Deletion: Muss als eigener Button in der App sichtbar sein (nicht nur per E-Mail)
- Kein `NSUserTrackingUsageDescription` Key in Info.plist noetig (kein ATT)

---

### P5-E04-S02: Google Play Store Privacy Compliance

**Als** App-Anbieter
**moechte ich** alle Google Play Datenschutzanforderungen erfuellen,
**damit** die App im Play Store veroeffentlicht werden kann.

**Plattform**: Google Play Console
**Abhaengigkeiten**: E01-S01 (Privacy URL live), E03-S01 (Datenschutztext final)
**Parallelisierbar mit**: P5-E04-S01 (Apple)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Privacy Policy URL** ist in der Google Play Console hinterlegt
  - URL: `https://track.fakturus.com/privacy`
  - Hinterlegt unter: Store Listing > App Details > Privacy Policy
- [ ] **Data Safety Section** ist vollstaendig ausgefuellt:
  - "Does your app collect or share any of the required user data types?": Ja
  - Datentypen:
    - **Personal info > Email address**: Collected, not shared, required, encrypted in transit
    - **Personal info > Name**: Collected (optional), not shared, optional, encrypted in transit
    - **App activity > App interactions**: Collected (Arbeitszeiten), not shared, required, encrypted in transit
    - **Device or other IDs > Device ID**: Not collected
  - Datensicherheit:
    - "Is all of the user data collected by your app encrypted in transit?": Ja (TLS)
    - "Do you provide a way for users to request that their data is deleted?": Ja
  - Keine Datenweitergabe an Dritte
- [ ] **Data Deletion Policy**:
  - URL oder In-App-Methode fuer Datenloeschung angegeben
  - Beschreibung: "Users can delete their account and all data within the app under Settings > Account > Delete Account"
- [ ] **Content Rating** (IARC): Ist korrekt ausgefuellt (keine sensiblen Inhalte)
- [ ] **Target Audience**: Korrekt gesetzt (16+, konsistent mit AGB-Mindestalter, kein COPPA relevant)
- [ ] **Ads Declaration**: "App does not contain ads"

**Technische Hinweise**:
- Google Play Console: "App content" > "Data safety"
- Google hat ein strukturiertes Formular -- Antworten muessen exakt zur App passen
- Data Safety Deklaration wird im Store angezeigt -- Inkonsistenz mit tatsaechlichem Verhalten fuehrt zur Ablehnung
- "Target audience" MUSS 18+ sein, wenn Arbeitsrecht-bezogene Daten verarbeitet werden

---

### P5-E04-S03: AGB-Link in Paywall und Store Listings

**Als** Nutzer der ein Abo abschliessen moechte
**moechte ich** vor dem Kauf die AGB und Datenschutzerklaerung einsehen koennen,
**damit** ich informiert ueber die Vertragsbedingungen entscheide.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: E01-S01 (URLs muessen funktionieren), E03-S02 (AGB-Text final)
**Parallelisierbar mit**: P5-E04-S01, P5-E04-S02
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] In der PaywallView (bereits vorhanden) zeigen die Links auf funktionierende URLs:
  - "Nutzungsbedingungen" -> `https://track.fakturus.com/terms` (bereits verlinkt)
  - "Datenschutz" -> `https://track.fakturus.com/privacy` (bereits verlinkt)
- [ ] Links oeffnen den System-Browser (nicht In-App WebView -- Apple Guideline)
- [ ] Links sind VOR dem Kauf-Button sichtbar (ohne Scrollen, falls moeglich)
- [ ] Given ein Nutzer tippt auf "Nutzungsbedingungen" in der Paywall
  Then oeffnet sich der System-Browser mit den AGB
  And die Paywall bleibt im Hintergrund erhalten (Nutzer kann zurueckkehren)
- [ ] Auto-Renewal Text ist sichtbar (Apple Guideline 3.1.2):
  - "Abo verlaengert sich automatisch monatlich. Kuendigung jederzeit ueber [Apple/Google] Einstellungen bis 24h vor Ablauf."
- [ ] Auf Android: Zusaetzlich in der Google Play Store-Beschreibung verlinkt

**Technische Hinweise**:
- Die PaywallView.swift enthaelt bereits Links zu `/terms` und `/privacy` -- diese Story stellt sicher, dass die URLs tatsaechlich funktionieren
- Auto-Renewal Notice: Ist teilweise implementiert (`paywall_auto_renew_notice` Localization Key), muss auf Vollstaendigkeit geprueft werden
- Apple Review: "Clearly describe the length of the subscription, the price, and that payment will be charged to the iTunes account"

---

### P5-E04-S04: Account-Loeschung in der App

**Als** Nutzer
**moechte ich** mein Konto und alle zugehoerigen Daten aus der App heraus loeschen koennen,
**damit** ich mein Recht auf Loeschung (DSGVO Art. 17) ausueben kann und die App Store Anforderungen erfuellt sind.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: Backend Account-Deletion Endpoint (falls noch nicht vorhanden)
**Parallelisierbar mit**: Alle E04-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] In den Einstellungen unter "Konto" gibt es einen Button "Konto loeschen" (rot, destructive style)
- [ ] Given ein Nutzer tippt "Konto loeschen"
  Then erscheint ein Bestatigungsdialog:
  "Dein Konto und alle Daten werden unwiderruflich geloescht. Aktive Abos muessen separat ueber [Apple/Google] gekuendigt werden. Moechtest du fortfahren?"
  And der Nutzer muss "LOESCHEN" eintippen zur Bestaetigung (Double-Opt-Out)
- [ ] Given der Nutzer bestaetigt die Loeschung
  Then wird das Backend aufgerufen (DELETE `/api/account`)
  And das Konto wird zur Loeschung markiert (30 Tage Aufbewahrungsfrist)
  And alle lokalen Daten werden sofort geloescht
  And der Nutzer wird ausgeloggt
  And eine Bestaetigung wird angezeigt: "Dein Konto wurde zur Loeschung vorgemerkt und wird innerhalb von 30 Tagen endgueltig entfernt."
- [ ] Der Nutzer wird darauf hingewiesen, sein Abo vorher zu kuendigen
- [ ] Given der Nutzer hat ein aktives Abo
  Then wird ein zusaetzlicher Warnhinweis angezeigt: "Du hast ein aktives Abo. Bitte kuendige es ueber die [Apple/Google] Einstellungen, um weitere Zahlungen zu vermeiden."
- [ ] Given die Loeschung schlaegt fehl (Netzwerkfehler)
  Then wird eine Fehlermeldung angezeigt und der Nutzer kann es erneut versuchen
  Or der Nutzer wird auf eine Support-E-Mail hingewiesen

**Technische Hinweise**:
- Apple Requirement seit Juni 2022: Apps mit Account-Erstellung muessen Account-Loeschung anbieten
- Google Play erfordert dasselbe seit Dezember 2023
- Backend: Soft-Delete mit 30-Tage-Frist (gibt dem Nutzer Moeglichkeit zum Widerruf)
- Consent-Daten bleiben fuer die Audit-Frist erhalten (30 Tage), werden dann ebenfalls geloescht
- Hinweis: Abo-Kuendigung geht NUR ueber Apple/Google -- die App kann das Abo nicht kuendigen

---

## Zusammenfassung

| Story | Titel | Aufwand | Prioritaet |
|-------|-------|---------|------------|
| S01 | Apple App Store Privacy | M | Must-Have |
| S02 | Google Play Privacy | M | Must-Have |
| S03 | AGB-Link in Paywall + Store | S | Must-Have |
| S04 | Account-Loeschung | M | Must-Have |

**Gesamt**: ~0.5 Wochen (vieles ist Konfiguration, nicht Code)

**Alle Stories sind Must-Have**: Ohne diese Compliance wird die App in beiden Stores abgelehnt.

---

## Checkliste vor Store-Submission

- [ ] Privacy Policy URL ist live und erreichbar: `https://track.fakturus.com/privacy`
- [ ] Terms URL ist live und erreichbar: `https://track.fakturus.com/terms`
- [ ] Imprint URL ist live und erreichbar: `https://track.fakturus.com/imprint`
- [ ] App Store Connect: App Privacy Details ausgefuellt
- [ ] App Store Connect: Privacy Policy URL hinterlegt
- [ ] Google Play Console: Data Safety Section ausgefuellt
- [ ] Google Play Console: Privacy Policy URL hinterlegt
- [ ] Consent-Screen erscheint bei Erstnutzung
- [ ] Account-Loeschung funktioniert in der App
- [ ] Auto-Renewal Text in Paywall vorhanden
- [ ] AGB-Link in Paywall funktioniert
- [ ] Datenschutz-Link in Paywall funktioniert
- [ ] Alle Texte in DE + EN verfuegbar
- [ ] Juristische Pruefung abgeschlossen

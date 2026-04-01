# EPIC 02: Consent-Flow & Consent-Tracking

## Ziel

Die App verfuegt ueber einen vollstaendigen Consent-Mechanismus, der sicherstellt, dass kein Nutzer die App verwenden kann, ohne den AGB und der Datenschutzerklaerung aktiv zugestimmt zu haben. Bei Aenderungen der rechtlichen Dokumente wird erneut Zustimmung eingeholt. Die Zustimmung wird lokal und serverseitig nachweisbar gespeichert.

## Rechtlicher Hintergrund

- **BGB §305 Abs. 2**: AGB werden nur Vertragsbestandteil, wenn der Verwender bei Vertragsschluss (1) die andere Partei ausdruecklich auf sie hinweist und (2) der anderen Partei die Moeglichkeit verschafft, in zumutbarer Weise von ihrem Inhalt Kenntnis zu nehmen, und die andere Partei mit ihrer Geltung einverstanden ist.
- **DSGVO Art. 7**: Die Einwilligung muss nachweisbar sein. Der Verantwortliche muss belegen koennen, dass die betroffene Person eingewilligt hat.
- **DSGVO Art. 7 Abs. 3**: Die betroffene Person hat das Recht, ihre Einwilligung jederzeit zu widerrufen. Der Widerruf muss so einfach wie die Erteilung sein.
- **BGB §305b**: Kein vorausgefuelltes Haekchen -- die Zustimmung muss eine aktive Handlung sein.

## Abhaengigkeiten

- **E01-S03**: Legal Document Version API (App muss wissen, welche Version aktuell ist)
- **E01-S04**: Consent Storage API (Zustimmung muss serverseitig gespeichert werden)
- **Phase 1 E02**: Auth muss funktionieren (Consent wird mit UserId verknuepft)

---

## Stories

### P5-E02-S01: First-Launch Consent Screen (iOS)

**Als** neuer Nutzer auf iOS
**moechte ich** beim ersten Start der App ueber Datenschutz und AGB informiert werden und aktiv zustimmen koennen,
**damit** meine Zustimmung rechtswirksam ist und ich informiert die App nutze.

**Plattform**: iOS
**Abhaengigkeiten**: E01-S03 (Version API), E01-S01 (Legal Pages muessen existieren)
**Parallelisierbar mit**: P5-E02-S02 (Android-Pendant)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Nach erfolgreichem Login wird geprueft, ob alle erforderlichen Consents vorliegen
- [ ] Falls Consents fehlen, wird ein modaler Consent-Screen angezeigt (NICHT dismissable)
- [ ] Der Consent-Screen zeigt:
  - Ueberschrift: "Nutzungsbedingungen & Datenschutz" / "Terms & Privacy"
  - Kurzfassung (2-3 Saetze) was der Nutzer akzeptiert
  - Verlinkung zum vollstaendigen Text (oeffnet In-App-Browser oder System-Browser):
    - "Datenschutzerklaerung lesen" -> `/privacy`
    - "Nutzungsbedingungen lesen" -> `/terms`
  - EINE aktive Checkbox + EINE Kenntnisnahme (Rechtsgrundlagen-Trennung!):
    - [ ] "Ich akzeptiere die Nutzungsbedingungen." (aktive Checkbox, BGB §305 -- Vertragszustimmung)
    - "Ich habe die Datenschutzerklaerung zur Kenntnis genommen." (Link-Klick/Kenntnisnahme, KEINE Einwilligungs-Checkbox -- Rechtsgrundlage ist Art. 6 Abs. 1 lit. b Vertragsdurchfuehrung, NICHT Einwilligung)
  - Button "Zustimmen und fortfahren" (nur aktiv wenn AGB-Checkbox gesetzt UND Datenschutz-Link mindestens einmal geoeffnet wurde)
  - **WICHTIG**: Finale Formulierung MUSS vom Anwalt freigegeben werden (Abgrenzung Einwilligung vs. Kenntnisnahme)
  - Link "Ablehnen" (kleiner, aber sichtbar)
- [ ] AGB-Checkbox ist standardmaessig NICHT gesetzt (BGB §305b)
- [ ] Button "Zustimmen" ist disabled solange AGB-Checkbox nicht aktiv und Datenschutz-Link nicht geoeffnet
- [ ] Given ein Nutzer setzt beide Checkboxen und tippt "Zustimmen"
  Then wird der Consent lokal gespeichert (UserDefaults/Keychain)
  And der Consent wird an POST `/api/legal/consent` gesendet
  And der Nutzer gelangt zur Hauptansicht der App
- [ ] Given ein Nutzer tippt "Ablehnen"
  Then erscheint ein Erklaerungsdialog: "Ohne Zustimmung kann die App nicht genutzt werden. Moechtest du dich abmelden?"
  And Optionen: "Zurueck" (zurueck zum Consent-Screen) oder "Abmelden" (Logout)
- [ ] Der Consent-Screen ist NICHT per Swipe oder Back-Button wegwischbar
- [ ] Der Screen wird in der Sprache des Systems angezeigt (DE/EN)
- [ ] Accessibility: Alle Checkboxen und Buttons sind per VoiceOver bedienbar

**Technische Hinweise**:
- Neuer ConsentView.swift (oder ConsentScreen.swift)
- Consent-Check als Gate in der App-Navigation: Nach Auth-Check, vor Main-Content
- Lokaler Consent-Status: `@AppStorage("consent_privacy_version")` und `@AppStorage("consent_terms_version")`
- Bei Netzwerkfehler: Consent lokal speichern, Backend-Sync bei naechster Gelegenheit nachholen (Queue)

---

### P5-E02-S02: First-Launch Consent Screen (Android)

**Als** neuer Nutzer auf Android
**moechte ich** beim ersten Start der App ueber Datenschutz und AGB informiert werden und aktiv zustimmen koennen,
**damit** meine Zustimmung rechtswirksam ist und ich informiert die App nutze.

**Plattform**: Android
**Abhaengigkeiten**: E01-S03 (Version API), E01-S01 (Legal Pages muessen existieren)
**Parallelisierbar mit**: P5-E02-S01 (iOS-Pendant)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Identisches Verhalten wie P5-E02-S01 (iOS), adaptiert fuer Android:
  - Nach erfolgreichem Login: Consent-Check
  - Modaler Consent-Screen (nicht per Back-Button schliessbar)
  - Zwei separate, nicht vorausgefuellte Checkboxen
  - "Zustimmen und fortfahren" nur bei beiden Checkboxen aktiv
  - "Ablehnen" fuehrt zu Erklaerungsdialog mit Logout-Option
- [ ] Links zu `/privacy` und `/terms` oeffnen im Custom Chrome Tab (oder System-Browser)
- [ ] Consent lokal gespeichert (SharedPreferences oder DataStore)
- [ ] Consent an Backend gesendet (POST `/api/legal/consent`)
- [ ] Back-Button auf dem Consent-Screen ist deaktiviert (kein `onBackPressed` Handler)
- [ ] TalkBack-kompatibel (Android Accessibility)
- [ ] Material Design 3 Komponenten fuer Checkboxen und Buttons

**Technische Hinweise**:
- Neuer ConsentScreen composable (Jetpack Compose)
- Navigation: Consent-Check als Gate im NavGraph, nach Auth, vor Hauptnavigation
- Lokaler Consent-Status: `DataStore<Preferences>` mit Keys fuer jedes Dokument + Version
- `OnBackPressedCallback` mit `isEnabled = true` um Back-Button zu blockieren

---

### P5-E02-S03: Consent-Version-Check beim App-Start

**Als** Bestandsnutzer
**moechte ich** bei AGB- oder Datenschutzaenderungen erneut um Zustimmung gebeten werden,
**damit** meine Zustimmung immer zur aktuellen Version passt.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: E01-S03 (Version API), P5-E02-S01/S02 (Consent Screen muss existieren)
**Parallelisierbar mit**: P5-E02-S04
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Bei jedem App-Start (nach Login) ruft die App GET `/api/legal/versions` auf
- [ ] Die App vergleicht die zurueckgegebenen Versionen mit den lokal gespeicherten Consent-Versionen
- [ ] Given die Version eines Dokuments mit `requiresConsent: true` hat sich geaendert (Major-Version)
  Then wird der Consent-Screen erneut angezeigt
  And der Screen zeigt: "Unsere [Datenschutzerklaerung/AGB] wurde aktualisiert. Bitte pruefe und stimme der neuen Version zu."
  And nur die geaenderten Dokumente muessen erneut bestaetigt werden
- [ ] Given es ist nur eine Minor/Patch-Aenderung
  Then wird KEIN erneuter Consent eingeholt
  But der Nutzer sieht einen dezenten Hinweis in den Einstellungen ("Aktualisiert am ...")
- [ ] Given die App kann den Version-Endpoint nicht erreichen (offline/Netzwerkfehler)
  Then gilt der zuletzt bekannte Consent-Status (lokal gespeichert)
  And die App ist normal nutzbar (kein Blocking bei Netzwerkproblemen)
  And beim naechsten erfolgreichen API-Call wird der Check nachgeholt
- [ ] Given ein Nutzer hat noch nie zugestimmt (App-Update von alter Version ohne Consent)
  Then wird der vollstaendige First-Launch Consent angezeigt (alle Dokumente)
- [ ] Die Versions-Pruefung findet VOR dem Zugang zur Hauptnavigation statt
- [ ] Timeout fuer Version-Check: Max. 5 Sekunden, danach Fallback auf lokalen Status

**Technische Hinweise**:
- ConsentManager/ConsentService Klasse die den Lifecycle verwaltet
- Flow: Auth -> ConsentCheck -> MainApp (oder ConsentScreen -> MainApp)
- Semantic Versioning Vergleich: Nur Major-Version-Aenderungen erfordern erneuten Consent
- Lokaler Cache: Letzte bekannte Versionen + Consent-Status
- Wichtig: Bestandsnutzer ohne Consent (Migration) muessen beim naechsten Start zustimmen

---

### P5-E02-S04: Lokaler Consent-Cache & Offline-Handling

**Als** Nutzer mit schlechter Internetverbindung
**moechte ich** die App auch offline nutzen koennen, wenn ich vorher zugestimmt habe,
**damit** meine Produktivitaet nicht von der Netzwerkverfuegbarkeit abhaengt.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P5-E02-S01/S02 (Consent Screen), P5-E02-S03 (Version Check)
**Parallelisierbar mit**: P5-E02-S03
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Consent-Status wird lokal persistent gespeichert:
  - iOS: `UserDefaults` (nicht-sensitiv) oder `Keychain` (sensitiv)
  - Android: `DataStore<Preferences>` (encrypted wenn moeglich)
- [ ] Gespeicherte Daten pro Dokument:
  - DocumentType
  - AcceptedVersion
  - ConsentTimestamp
  - SyncedToBackend (bool)
- [ ] Given der Nutzer hat online zugestimmt und geht offline
  Then kann die App normal genutzt werden
  And der Consent-Status bleibt erhalten
- [ ] Given der Nutzer war offline bei der Zustimmung (Erstnutzung ohne Netz nach Login)
  Then wird der Consent lokal gespeichert mit `SyncedToBackend = false`
  And bei der naechsten Netzverbindung wird der Consent automatisch an das Backend gesendet
- [ ] Given das Backend-Sync des Consents schlaegt fehl
  Then wird ein persistenter Retry-Mechanismus ausgeloest (bei JEDER Netzwerkverbindung, unbegrenzt)
  And der Nutzer wird NICHT erneut zur Zustimmung aufgefordert
  And nach 7 Tagen ohne erfolgreichen Sync: dezenter Hinweis "Bitte stelle eine Internetverbindung her, damit deine Zustimmung gespeichert werden kann."
- [ ] Given der Nutzer loescht die App und installiert neu
  Then wird der Consent-Status vom Backend abgerufen (GET `/api/legal/consent`)
  And falls der Consent aktuell ist, muss NICHT erneut zugestimmt werden
- [ ] Given der Nutzer loescht die App-Daten (Android) oder deinstalliert/reinstalliert (iOS)
  And es gibt keinen Backend-Zugang (offline)
  Then muss der Consent erneut erteilt werden (Fallback auf First-Launch Flow)

**Technische Hinweise**:
- Sync-Queue: Pending Consents in eine Queue einreihen, bei Connectivity-Change abarbeiten
- iOS: `NWPathMonitor` fuer Connectivity-Detection
- Android: `ConnectivityManager` oder `WorkManager` fuer deferred Sync
- Wichtig: Consent ist kein Datenbank-Objekt sondern App-Level-State (vor DB-Init verfuegbar)

---

### P5-E02-S05: Einwilligungsverwaltung & Widerruf

**Als** Nutzer
**moechte ich** meine Einwilligungen einsehen und ggf. widerrufen koennen,
**damit** ich Transparenz ueber meine Zustimmungen habe (DSGVO Art. 7 Abs. 3).

**WICHTIG -- Abgrenzung Einwilligung vs. Vertrag**:
Da die Kernverarbeitung auf Vertragsdurchfuehrung (Art. 6 Abs. 1 lit. b) basiert, betrifft ein Widerruf NUR etwaige einwilligungsbasierte Verarbeitungen (z.B. optionales Crash-Reporting, Analytics). Die Konto-Loeschung ist ein separater Vorgang (siehe E04-S04) und wird NICHT automatisch durch einen Widerruf ausgeloest.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P5-E02-S01/S02, E01-S04 (Consent API)
**Parallelisierbar mit**: P5-E02-S03, P5-E02-S04
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] In den Einstellungen unter "Rechtliches" gibt es einen Eintrag "Einwilligungen verwalten"
- [ ] Der Screen zeigt den aktuellen Consent-Status:
  - Datenschutzerklaerung: Version X.Y.Z, zugestimmt am TT.MM.JJJJ
  - Nutzungsbedingungen: Version X.Y.Z, zugestimmt am TT.MM.JJJJ
- [ ] Es gibt einen Button "Einwilligung widerrufen"
- [ ] Falls einwilligungsbasierte Verarbeitungen existieren (z.B. Crash-Reporting):
  - Toggle pro Verarbeitung (ein/aus)
  - Widerruf deaktiviert nur die spezifische Verarbeitung, NICHT das gesamte Konto
- [ ] Fuer Konto-Loeschung: Verweis auf E04-S04 ("Konto loeschen" in den Einstellungen)
- [ ] Hinweis: "Die Verarbeitung deiner Daten fuer die Zeiterfassung basiert auf der Vertragsdurchfuehrung und erfordert keine separate Einwilligung. Um die Datenverarbeitung vollstaendig zu beenden, loesche dein Konto."
- [ ] Der Widerruf ist genauso einfach wie die Erteilung (DSGVO Art. 7 Abs. 3, Satz 4)
- [ ] Der Widerruf-Button ist NICHT versteckt oder schwer zu finden

**Technische Hinweise**:
- Widerruf = Account-Loeschung einleiten (da ohne Consent keine Datenverarbeitung moeglich)
- Backend: Account-Loeschungs-Queue (30 Tage Aufbewahrungsfrist fuer Audit, dann endgueltige Loeschung)
- Hinweis in der Consent-Verwaltung: "Der Widerruf gilt fuer die Zukunft. Die bisherige Verarbeitung bleibt rechtmaessig (DSGVO Art. 7 Abs. 3, Satz 2)."

---

### P5-E02-S06: Consent-Banner bei AGB-Update (In-App Notification)

**Als** Bestandsnutzer
**moechte ich** klar und verstaendlich darueber informiert werden, wenn sich die AGB oder Datenschutzerklaerung geaendert hat,
**damit** ich weiss warum ich erneut zustimmen muss und was sich geaendert hat.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P5-E02-S03 (Version-Check)
**Parallelisierbar mit**: P5-E02-S04, P5-E02-S05
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Wenn ein Version-Update erkannt wird, zeigt der Consent-Screen eine zusaetzliche Info-Box:
  - "Was hat sich geaendert?" -- Kurze Zusammenfassung der Aenderungen (1-3 Saetze)
  - Link zum vollstaendigen geaenderten Dokument
  - Datum der Aenderung
- [ ] Die Aenderungs-Zusammenfassung kommt aus der Version API (neues Feld `changeSummary`)
- [ ] Given das `changeSummary`-Feld leer ist
  Then wird ein generischer Text angezeigt: "Wir haben unsere [Dokumentname] aktualisiert. Bitte lies die aktuelle Version und bestatige deine Zustimmung."
- [ ] Der Consent-Screen fuer Updates unterscheidet sich visuell leicht vom First-Launch Screen:
  - Ueberschrift: "Aktualisierung der [Dokumentname]" statt "Willkommen"
  - Nur die geaenderten Dokumente werden zur Zustimmung angezeigt
- [ ] Ablehnung bei Update hat denselben Flow wie beim First-Launch (Erklaerung + Logout-Option)

**Technische Hinweise**:
- Version API erweitern um optionales Feld `changeSummary` (DE + EN)
- Dieses Feld wird manuell befuellt wenn eine neue Version veroeffentlicht wird
- UI: Wiederverwendung des ConsentScreen mit einem "mode" Parameter (firstLaunch vs. update)

---

## Edge Cases & Sonderfaelle

| Szenario | Verhalten |
|----------|-----------|
| Nutzer hat alte App-Version ohne Consent | Beim naechsten Start: Vollstaendiger First-Launch Consent |
| Nutzer lehnt ab | Erklaerung + Logout. Kein Zugang zur App. |
| Netzwerk-Timeout beim Version-Check | Lokaler Consent gilt. Naechster Versuch beim naechsten Start. |
| Consent-POST schlaegt fehl | Lokal speichern, Retry-Queue, App nutzbar. |
| Nutzer loescht App-Daten | Backend-Check bei Re-Login. Falls Consent aktuell: kein erneuter Consent. |
| Freemium-Nutzer (FREE Tier) | Consent ist PFLICHT -- auch ohne Bezahlung werden Daten verarbeitet. |
| Mehrere Geraete | Consent gilt accountweit. Backend ist Source of Truth. |
| Sprache wechselt nach Consent | Consent bleibt gueltig (er gilt fuer die Version, nicht die Sprache). |

---

## Zusammenfassung

| Story | Titel | Aufwand | Prioritaet |
|-------|-------|---------|------------|
| S01 | First-Launch Consent (iOS) | L | Must-Have |
| S02 | First-Launch Consent (Android) | L | Must-Have |
| S03 | Consent-Version-Check | M | Must-Have |
| S04 | Lokaler Cache & Offline | M | Must-Have |
| S05 | Consent-Widerruf | M | Must-Have |
| S06 | AGB-Update Banner | S | Should-Have |

**Gesamt**: ~1.5 Wochen bei paralleler iOS/Android-Entwicklung

**Priorisierung**:
- S01-S04 sind **Must-Have** (ohne diese ist die App nicht DSGVO-konform)
- S05 ist **Must-Have** (DSGVO Art. 7 Abs. 3 schreibt Widerrufsmoeglichkeit vor)
- S06 ist **Should-Have** (verbessert UX bei Updates, kann initial mit generischem Text geloest werden)

# EPIC 03: Rechtliche Texte -- Inhalt & Struktur

## Ziel

Alle rechtlich erforderlichen Dokumente (Datenschutzerklaerung, AGB/Nutzungsbedingungen, Impressum) sind inhaltlich erstellt, juristisch geprueft und in DE + EN verfuegbar. Dieses Epic definiert die **Struktur und Gliederung** der Texte -- nicht den vollstaendigen Rechtstext selbst (dieser wird durch einen Anwalt oder spezialisierten Dienstleister erstellt/geprueft).

## Abhaengigkeiten

- **E01-S01**: Backend muss die Seiten ausliefern koennen (Hosting)
- **Feature-Liste**: Muss final sein, da die Datenschutzerklaerung alle erhobenen Daten auflisten muss
- **Technische Architektur**: Azure B2C, Azure Germany Hosting -- fuer Abschnitt "Datenempfaenger"

## Rechtlicher Hinweis

Die hier definierten Strukturen dienen als **Briefing fuer den Rechtsanwalt/Datenschutzbeauftragten**. Der finale Text MUSS juristisch geprueft werden. Ein selbst geschriebener Datenschutztext ohne Pruefung ist ein hoehereres Risiko als gar keiner.

---

## Stories

### P5-E03-S01: Datenschutzerklaerung (Struktur & Entwurf)

**Als** Nutzer
**moechte ich** eine vollstaendige, DSGVO-konforme Datenschutzerklaerung lesen koennen,
**damit** ich weiss, welche meiner Daten wie verarbeitet werden und welche Rechte ich habe.

**Plattform**: Web (gehostet via E01)
**Abhaengigkeiten**: E01-S01 (Hosting), Feature-Freeze (finale Datenliste)
**Parallelisierbar mit**: P5-E03-S02, P5-E03-S03
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Datenschutzerklaerung in Deutsch (rechtlich bindend) und Englisch verfuegbar
- [ ] Erreichbar unter `https://track.fakturus.com/privacy`
- [ ] Inhalt deckt alle DSGVO Art. 13 Pflichtangaben ab (siehe Gliederung unten)
- [ ] Sprache ist verstaendlich (kein reines Juristendeutsch, aber rechtssicher)
- [ ] Dokument enthaelt Versionsnummer und Datum ("Version 1.0.0, Stand: TT.MM.JJJJ")
- [ ] Dokument wurde von einem Anwalt/Datenschutzbeauftragten geprueft
- [ ] Alle genannten Datenverarbeitungen entsprechen der tatsaechlichen Implementierung

**Pflichtgliederung (DSGVO Art. 13)**:

```
1. Verantwortlicher
   - Name, Anschrift, E-Mail des Verantwortlichen
   - Kontakt des Datenschutzbeauftragten (falls bestellt)

2. Ueberblick der Datenverarbeitung
   - Zusammenfassung: Welche Daten, warum, wie lange

3. Erhobene Daten und Zwecke
   3.1 Registrierung & Authentifizierung
       - Daten: E-Mail, Name (optional), OAuth-Token
       - Zweck: Kontoerstellung und Anmeldung
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. b (Vertragsdurchfuehrung)
       - Dienstleister: Azure B2C (Microsoft, EU-Rechenzentrum)
   3.2 Zeiterfassung (Kernfunktion)
       - Daten: Arbeitszeiten, Pausen, Arbeitstage
       - Zweck: Zeiterfassung gemaess ArbZG
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. b (Vertragsdurchfuehrung)
   3.3 Urlaubsverwaltung
       - Daten: Urlaubstage, Krankheitstage
       - Zweck: Urlaubsplanung und -dokumentation
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. b
   3.4 Einstellungen & Praeferenzen
       - Daten: Bundesland, Wochenarbeitsstunden, Arbeitstage
       - Zweck: Korrekte Berechnung von Feiertagen und Soll-Stunden
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. b
   3.5 Synchronisation
       - Daten: Alle o.g. Daten werden verschluesselt uebertragen
       - Zweck: Geraeteuebergreifende Nutzung und Datensicherung
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. b
   3.6 Consent-Speicherung
       - Daten: Zeitpunkt der Zustimmung, IP-Adresse, App-Version, Plattform
       - Zweck: Nachweisbarkeit der Zustimmung (DSGVO Art. 7 Abs. 1)
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. f (berechtigtes Interesse)
       - Speicherdauer: 30 Tage nach Kontodeloeschung
   3.7 In-App-Kaeufe
       - Daten: Transaktions-ID, Abo-Status (KEINE Zahlungsdaten)
       - Zweck: Freischaltung von Premium-Funktionen
       - Rechtsgrundlage: Art. 6 Abs. 1 lit. b
       - Hinweis: Zahlungsabwicklung durch Apple/Google, nicht durch uns

4. Datenempfaenger und Auftragsverarbeiter
   - Microsoft Azure (Hosting, EU-Rechenzentrum Deutschland)
   - Microsoft Azure B2C (Authentifizierung, EU)
   - Apple Inc. (In-App-Kaeufe, nur Transaktions-IDs)
   - Google LLC (In-App-Kaeufe, nur Transaktions-IDs)
   - KEIN Drittland-Transfer ausserhalb EU/EWR (oder: mit Angemessenheitsbeschluss)

5. Speicherdauer
   - Kontodaten: Solange das Konto aktiv ist
   - Zeiterfassungsdaten: Solange das Konto aktiv ist
   - Nach Kontodeloeschung: 30 Tage Aufbewahrung, dann endgueltige Loeschung
   - Consent-Daten: 30 Tage nach Kontodeloeschung (Nachweispflicht)

6. Betroffenenrechte
   - Auskunftsrecht (Art. 15)
   - Recht auf Berichtigung (Art. 16)
   - Recht auf Loeschung (Art. 17)
   - Recht auf Einschraenkung (Art. 18)
   - Recht auf Datenportabilitaet (Art. 20)
   - Widerspruchsrecht (Art. 21)
   - Recht auf Widerruf der Einwilligung (Art. 7 Abs. 3)
   - Beschwerderecht bei der Aufsichtsbehoerde

7. Datensicherheit
   - TLS-Verschluesselung bei Datenuebertragung
   - Verschluesselte Speicherung in Azure
   - Kein Zugriff durch Dritte

8. Cookies und Tracking
   - Die App verwendet KEINE Cookies
   - KEINE Tracking-SDKs (kein Google Analytics, kein Facebook SDK)
   - KEINE Werbung
   - Falls Crashlytics/Sentry: Opt-In, anonymisiert

9. Aenderungen der Datenschutzerklaerung
   - Hinweis auf Versionierung
   - Bei wesentlichen Aenderungen: Erneute Einwilligung erforderlich
   - Aenderungshistorie (Version, Datum, Zusammenfassung)
```

**Technische Hinweise**:
- Als Startpunkt einen DSGVO-Generator verwenden (z.B. e-recht24.de, Datenschutz-Generator.de)
- Generator-Output MUSS manuell angepasst und durch Anwalt geprueft werden
- HTML-Dokument mit klaren Ueberschriften, Ankerlinks und Inhaltsverzeichnis
- Versionsnummer und Stand-Datum im Dokument und in der Version API (E01-S03)

---

### P5-E03-S02: Nutzungsbedingungen / AGB (Struktur & Entwurf)

**Als** Nutzer
**moechte ich** die Nutzungsbedingungen einsehen koennen,
**damit** ich weiss, unter welchen Bedingungen ich die App nutze und was meine Rechte und Pflichten sind.

**Plattform**: Web (gehostet via E01)
**Abhaengigkeiten**: E01-S01 (Hosting), Tier-System definiert (Phase 4)
**Parallelisierbar mit**: P5-E03-S01, P5-E03-S03
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] AGB/Nutzungsbedingungen in Deutsch (rechtlich bindend) und Englisch verfuegbar
- [ ] Erreichbar unter `https://track.fakturus.com/terms`
- [ ] Inhalt deckt alle relevanten Bereiche ab (siehe Gliederung)
- [ ] Dokument enthaelt Versionsnummer und Datum
- [ ] Dokument wurde von einem Anwalt geprueft
- [ ] AGB sind konform mit BGB §305 ff. (keine ueberraschenden Klauseln, transparentes Deutsch)
- [ ] Apple App Store und Google Play spezifische Anforderungen sind beruecksichtigt

**Pflichtgliederung (BGB §305 ff. konform)**:

```
1. Geltungsbereich
   - Diese AGB gelten fuer die App "fakturus.track" und zugehoerige Dienste
   - Anbieter: [Name, Anschrift]
   - Zielgruppe: Arbeitnehmer in Deutschland (primaer)

2. Vertragsschluss
   - Vertrag kommt durch Registrierung und Zustimmung zu diesen AGB zustande
   - Kostenlose Nutzung (FREE Tier) stellt einen Vertrag dar
   - Mindestaller: 16 Jahre (DSGVO Art. 8)

3. Leistungsbeschreibung
   3.1 FREE Tier: Timer, Pausen, History (365 Tage), Feiertage, Widgets, Ueberstunden
   3.2 STARTER Tier (2,99 EUR/Monat): + PDF/CSV-Export, Urlaub, Krankheitstage
   3.3 PRO Tier (4,99 EUR/Monat): + DATEV-Export, Schulferien, Kalender
   - Hinweis: Features koennen erweitert werden (Aenderungsvorbehalt)

4. Abo-Bedingungen
   4.1 Abrechnung ueber Apple App Store / Google Play Store
   4.2 Automatische Verlaengerung (monatlich)
   4.3 Kuendigungsfrist: Jederzeit bis 24h vor Ablauf der aktuellen Periode
   4.4 Kuendigung ueber die jeweilige Store-Einstellung (Apple: Einstellungen > Abos / Google: Play Store > Abos)
   4.5 Erstattungen: Gemaess den Richtlinien von Apple/Google
   4.6 Downgrade: Bei Kuendigung bleiben Premium-Daten erhalten, Features werden gesperrt

5. Nutzungsrechte und -pflichten
   - Nutzer erhaelt einfaches, nicht-uebertragbares Nutzungsrecht
   - Nutzer ist fuer die Richtigkeit seiner Daten verantwortlich
   - Untersagt: Reverse Engineering, kommerzielle Weitergabe, Missbrauch

6. Haftung und Gewaehrleistung
   - Keine Gewaehr fuer steuerliche Korrektheit (DATEV-Export ist Hilfsmittel)
   - Keine Gewaehr fuer arbeitsrechtliche Konformitaet (Nutzer muss ArbZG selbst pruefen)
   - Haftungsbeschraenkung auf Vorsatz und grobe Fahrlaessigkeit
   - Keine Haftung fuer Datenverlust bei kostenloser Nutzung (FREE Tier)

7. Datenschutz
   - Verweis auf die Datenschutzerklaerung (Link zu /privacy)

8. Verfuegbarkeit und Wartung
   - Keine Garantie fuer 100% Verfuegbarkeit
   - Wartungsfenster werden angekuendigt (soweit moeglich)
   - Offline-Funktionalitaet: Lokale Daten bleiben bei Serverausfall verfuegbar

9. Konto-Loeschung
   - Nutzer kann Konto jederzeit loeschen (in der App oder per E-Mail)
   - Daten werden innerhalb von 30 Tagen endgueltig geloescht
   - Export vor Loeschung empfohlen

10. Aenderungen der AGB
    - Aenderungen werden 30 Tage vor Inkrafttreten angekuendigt
    - Bei wesentlichen Aenderungen: Erneute Zustimmung erforderlich (in der App)
    - Widerspruchsrecht: Nutzer kann kuendigen statt zuzustimmen

11. Schlussbestimmungen
    - Anwendbares Recht: Deutsches Recht
    - Gerichtsstand: [Sitz des Anbieters] (nur fuer B2B; bei Verbrauchern gilt gesetzlicher Gerichtsstand)
    - Salvatorische Klausel
    - Verbraucherschlichtung: Verweis auf OS-Plattform (EU-Verordnung Nr. 524/2013)
```

**Technische Hinweise**:
- Apple Review Guidelines 3.1.2: Subscription terms muessen "clearly identified" sein
- Apple verlangt: Link zu AGB UND Datenschutz direkt in der Paywall (bereits implementiert in PaywallView.swift)
- BGB §305c: Ueberraschende Klauseln sind unwirksam -- AGB muessen dem durchschnittlichen Nutzer zumutbar sein
- Kuendigungsbutton-Gesetz (Juli 2022): Kuendigungsweg muss einfach sein -- bei uns ueber Apple/Google Store, das erfuellt die Anforderung

---

### P5-E03-S03: Impressum (Inhalt)

**Als** Nutzer in Deutschland
**moechte ich** ein vollstaendiges Impressum einsehen koennen,
**damit** ich weiss, wer hinter der App steht und die DDG-Pflicht erfuellt ist.

**Plattform**: Web (gehostet via E01)
**Abhaengigkeiten**: E01-S01 (Hosting)
**Parallelisierbar mit**: P5-E03-S01, P5-E03-S02
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Impressum erreichbar unter `https://track.fakturus.com/imprint`
- [ ] Impressum auch innerhalb der App erreichbar (max. 2 Klicks: Einstellungen > Impressum)
- [ ] Inhalt gemaess DDG §5 (ehem. TMG):
  - Name und Anschrift des Anbieters (kein Postfach!)
  - E-Mail-Adresse
  - Telefonnummer (empfohlen, nicht zwingend)
  - USt-IdNr. (falls vorhanden)
  - Handelsregistereintrag (falls vorhanden): Registergericht und -nummer
  - Vertretungsberechtigte Person(en)
  - Inhaltlich Verantwortlicher gemaess §18 Abs. 2 MStV (falls redaktionelle Inhalte)
- [ ] Impressum enthaelt Verweis auf EU-Streitschlichtungsplattform:
  - Link: https://ec.europa.eu/consumers/odr
  - Text: "Wir sind nicht bereit oder verpflichtet, an Streitbeilegungsverfahren vor einer Verbraucherschlichtungsstelle teilzunehmen." (oder alternative Formulierung)
- [ ] Seite ist in Deutsch und Englisch verfuegbar
- [ ] Keine Login-Pflicht fuer den Zugriff

**Pflichtgliederung**:

```
1. Angaben gemaess DDG §5
   - [Firmenname / Name des Einzelunternehmers]
   - [Strasse und Hausnummer]
   - [PLZ Ort]
   - Vertreten durch: [Name]

2. Kontakt
   - E-Mail: [kontakt@fakturus.com]
   - Telefon: [optional]

3. Umsatzsteuer-ID
   - USt-IdNr. gemaess §27a UStG: [DE...]

4. Handelsregister
   - [Falls vorhanden: AG [Ort], HRB [Nummer]]

5. EU-Streitschlichtung
   - Plattform der EU-Kommission: https://ec.europa.eu/consumers/odr
   - Bereitschaft zur Teilnahme: [Ja/Nein + Begruendung]

6. Haftung fuer Inhalte
   - Standard-Haftungsklausel (DDG §7)

7. Haftung fuer Links
   - Standard-Disclaimer fuer externe Links
```

**Technische Hinweise**:
- Einfachste der drei Seiten -- reiner Text mit fester Struktur
- Kann als Template befuellt werden, sobald die Firmendaten feststehen
- DDG §5 verlangt "leicht erkennbar, unmittelbar erreichbar" -- 2-Klick-Regel

---

### P5-E03-S04: Aenderungshistorie und Versionierung der Texte

**Als** Nutzer
**moechte ich** nachvollziehen koennen, wann und wie sich die rechtlichen Dokumente geaendert haben,
**damit** ich Transparenz ueber die Aenderungen habe.

**Plattform**: Web (gehostet via E01)
**Abhaengigkeiten**: E01-S01 (Hosting), P5-E03-S01, P5-E03-S02
**Parallelisierbar mit**: P5-E02-S06
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Jedes rechtliche Dokument enthaelt am Ende eine Aenderungshistorie:
  ```
  Aenderungshistorie:
  - Version 1.0.0 (TT.MM.JJJJ): Erstveroeffentlichung
  - Version 1.1.0 (TT.MM.JJJJ): [Beschreibung der Aenderung]
  ```
- [ ] Versionsnummer und Stand-Datum stehen am Anfang jedes Dokuments
- [ ] Aenderungen sind so beschrieben, dass ein Laie versteht, was sich geaendert hat
- [ ] Bei Major-Aenderungen (neue Rechtsgrundlage, neue Datenverarbeitung, etc.) steigt die Major-Version
- [ ] Versionsnummer im Dokument stimmt mit der Version in der API (E01-S03) ueberein
- [ ] Alte Versionen sind NICHT mehr oeffentlich abrufbar (nur die aktuelle) -- aber im Git-Repository nachvollziehbar

**Technische Hinweise**:
- Versionsnummer wird manuell gepflegt (nicht automatisch)
- Git-History der HTML-Dateien dient als Audit-Trail fuer aeltere Versionen
- Bei Version-Update: Gleichzeitig die `legal-versions.json` (oder DB) aus E01-S03 aktualisieren

---

### P5-E03-S05: Juristische Pruefung (Prozess-Story)

**Als** Produktverantwortlicher
**moechte ich** sicherstellen, dass alle rechtlichen Texte von einem qualifizierten Anwalt geprueft werden,
**damit** die Texte rechtssicher sind und kein Abmahnrisiko besteht.

**Plattform**: Prozess (kein Code)
**Abhaengigkeiten**: P5-E03-S01, P5-E03-S02, P5-E03-S03
**Parallelisierbar mit**: Nein (erst nach Entwurf)
**Geschaetzter Aufwand**: M (Wartezeit auf externen Anwalt, nicht Entwicklungsaufwand)

**Akzeptanzkriterien**:
- [ ] Ein auf IT-Recht / Datenschutzrecht spezialisierter Anwalt hat alle drei Dokumente geprueft
- [ ] Pruefung umfasst:
  - DSGVO-Konformitaet der Datenschutzerklaerung
  - BGB §305 ff. Konformitaet der AGB
  - DDG §5 Konformitaet des Impressums
  - Konsistenz zwischen den Dokumenten
- [ ] Anpassungen aus der Pruefung sind eingearbeitet
- [ ] Anwalt hat finale Version schriftlich freigegeben
- [ ] Kosten fuer die Pruefung sind budgetiert (ca. 500-1500 EUR fuer App-AGB + Datenschutz)

**Hinweise**:
- Empfohlene Kanzleien: Spezialisierung auf IT-Recht, App-Recht, DSGVO
- Alternative: Spezialisierte Online-Dienste (z.B. e-recht24 Premium, IT-Recht Kanzlei)
- Zeitrahmen: 1-2 Wochen Bearbeitungszeit beim Anwalt einplanen

---

### P5-E03-S06: Auftragsverarbeitungsvertrag (AVV) mit Microsoft Azure

**Als** Produktverantwortlicher
**moechte ich** sicherstellen, dass ein AVV mit Microsoft Azure existiert und dokumentiert ist,
**damit** die Datenuebermittlung an Azure DSGVO-konform ist (Art. 28 DSGVO).

**Plattform**: Prozess (kein Code)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Microsoft Data Processing Addendum (DPA) ist akzeptiert und dokumentiert
  - URL: https://www.microsoft.com/licensing/docs/view/Microsoft-Products-and-Services-Data-Protection-Addendum-DPA
- [ ] Dokumentation enthaelt: Datum der Akzeptanz, betroffene Azure Services, Speicherort EU
- [ ] AVV-Status wird in der Datenschutzerklaerung referenziert (Abschnitt "Datenempfaenger")
- [ ] Pruefung durch Anwalt im Rahmen von E03-S05

**Hinweis**: Microsoft bietet den DPA standardmaessig an -- muss aber aktiv akzeptiert werden. Ohne AVV ist jede Datenuebermittlung an Azure ein DSGVO-Verstoss.

---

### P5-E03-S07: Verzeichnis der Verarbeitungstaetigkeiten (VVT)

**Als** Verantwortlicher im Sinne der DSGVO
**moechte ich** ein Verzeichnis der Verarbeitungstaetigkeiten fuehren,
**damit** ich der Pflicht aus DSGVO Art. 30 nachkomme und bei Anfragen der Aufsichtsbehoerde vorbereitet bin.

**Plattform**: Prozess (kein Code)
**Abhaengigkeiten**: E03-S01 (Datenliste muss bekannt sein)
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] VVT als internes Dokument erstellt (z.B. `.project/legal/vvt.md` oder Tabelle)
- [ ] Enthaelt fuer jede Verarbeitungstaetigkeit:
  - Bezeichnung und Zweck
  - Kategorien betroffener Personen und personenbezogener Daten
  - Empfaenger der Daten
  - Uebermittlungen in Drittlaender (keine bei uns)
  - Loeschfristen
  - Technische und organisatorische Massnahmen
- [ ] Wird vom Anwalt im Rahmen von E03-S05 mitgeprueft
- [ ] Template einer Landesbehoerde als Grundlage verwenden (z.B. LfDI BW)

**Hinweis**: Das VVT ist das ERSTE Dokument, das eine Aufsichtsbehoerde bei einer Pruefung anfordert. Es ist kein oeffentliches Dokument, aber muss auf Anfrage vorgelegt werden koennen.

---

## Zusammenfassung

| Story | Titel | Aufwand | Prioritaet |
|-------|-------|---------|------------|
| S01 | Datenschutzerklaerung | L | Must-Have |
| S02 | Nutzungsbedingungen / AGB | L | Must-Have |
| S03 | Impressum | S | Must-Have |
| S04 | Aenderungshistorie & Versionierung | S | Must-Have |
| S05 | Juristische Pruefung | M (extern) | Must-Have |
| S06 | AVV mit Microsoft Azure | S | Must-Have |
| S07 | Verzeichnis der Verarbeitungstaetigkeiten (VVT) | S | Must-Have |

**Gesamt**: ~1 Woche Entwicklung + 1-2 Wochen Wartezeit fuer Anwalt

**Alle Stories sind Must-Have**: Ohne rechtlich gepruefte Texte und AVV/VVT ist ein Launch in Deutschland nicht vertretbar.

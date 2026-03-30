# EPIC 06: Google Play Store Vorbereitung (Android)

## Ziel

Alle erforderlichen Materialien und Metadaten fuer die Google Play Store Veroeffentlichung sind vorbereitet. Screenshots, Store Listing, Data Safety Section und Content Rating entsprechen den Google Play Policies.

## Abhaengigkeiten

- **Phase 3 abgeschlossen**: Finale UI fuer Screenshots
- **E07 (Privacy Policy)**: Privacy Policy URL muss stehen

---

## Stories

### P4-E06-S01: Google Play Screenshots erstellen

**Als** Product Owner
**moechte ich** professionelle Play Store Screenshots,
**damit** potenzielle Nutzer die App-Qualitaet sofort erkennen.

**Plattform**: Android
**Abhaengigkeiten**: Phase 3 (finale UI)
**Parallelisierbar mit**: P4-E05-*, P4-E01-*, P4-E07-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Screenshots fuer mindestens 2 Geraeteklassen:
  - Phone (1080x1920 oder hoeher) -- Pflicht
  - 7-Zoll Tablet (optional, empfohlen)
  - 10-Zoll Tablet (optional)
- [ ] Mindestens 5 Screenshots (identische Reihenfolge und Motive wie iOS, siehe E05-S01):
  1. Timer-Screen (laufende Session)
  2. History (Monatsgruppierung)
  3. Urlaub-Kalender
  4. Gesamt-Tab (Ueberstunden-Dashboard)
  5. Export (PDF/CSV)
- [ ] Screenshots in DE (primaer) und EN (sekundaer)
- [ ] Marketing-Text-Overlays (konsistent mit iOS)
- [ ] Feature Graphic (1024x500px): App-Logo + Tagline "Arbeitszeit erfassen. Einfach. Ueberall."

**Technische Hinweise**:
- Android Emulator Screenshots oder `adb shell screencap`
- Gleiche Demo-Daten wie iOS verwenden (Konsistenz)
- Google Play erlaubt min. 2, max. 8 Screenshots

---

### P4-E06-S02: Google Play Store Listing

**Als** Product Owner
**moechte ich** ein optimiertes Play Store Listing,
**damit** die App gut auffindbar ist und Nutzer zum Download motiviert.

**Plattform**: Android (Google Play Console)
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **App-Name**: "Fakturus Track -- Zeiterfassung" (max. 30 Zeichen)
- [ ] **Kurzbeschreibung** (max. 80 Zeichen):
  - DE: "Zeiterfassung fuer Deutschland. ArbZG-konform. Offline-first. DATEV-Export."
  - EN: "Time tracking for Germany. Compliant. Offline-first. DATEV export."
- [ ] **Vollstaendige Beschreibung** (max. 4000 Zeichen):
  - Identischer Inhalt wie iOS-Beschreibung (angepasst an Play Store Format)
  - HTML-Formatierung erlaubt (<b>, <br>, Bullet Points)
- [ ] **Kategorie**: "Business"
- [ ] **Tags**: "Zeiterfassung", "Arbeitszeit", "Stempeluhr"
- [ ] **Kontakt-Details**: Support-E-Mail, Website URL

**Technische Hinweise**:
- Google Play erlaubt HTML-Tags in der Beschreibung
- Kurzbeschreibung ist das Aequivalent zum iOS-Untertitel -- hoehere Sichtbarkeit

---

### P4-E06-S03: Data Safety Section & Content Rating

**Als** Product Owner
**moechte ich** die Data Safety Section und Content Rating korrekt ausfuellen,
**damit** die App den Google Play Policies entspricht und Nutzer Transparenz ueber die Datenverarbeitung haben.

**Plattform**: Android (Google Play Console)
**Abhaengigkeiten**: E07 (Privacy Policy)
**Parallelisierbar mit**: P4-E05-*, P4-E01-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Data Safety Section** vollstaendig ausgefuellt:
  - Datentypen die erhoben werden:
    - Persoenliche Daten: Name, E-Mail (Azure B2C)
    - Arbeitszeiten, Pausen, Urlaubstage, Krankheitstage (App-Funktionalitaet)
  - Datentypen die NICHT erhoben werden:
    - Standort: Nein
    - Fotos/Videos: Nein
    - Kontakte: Nein
    - Finanzinformationen: Nein (Zahlungen ueber Google Play)
  - Datenweitergabe: Nein (keine Weitergabe an Dritte)
  - Verschluesselung: Ja (HTTPS, Keystore)
  - Loeschmoeglichkeit: Ja (Account-Loeschung ueber Support oder In-App)
- [ ] **Content Rating** (IARC-Fragebogen):
  - Keine Gewalt, keine sexuellen Inhalte, keine Drogen
  - Ergebnis: PEGI 3 / Everyone
- [ ] **Target Audience**: "Alle ab 18" (Business-App, keine Kinder-Zielgruppe)
  - WICHTIG: Kein COPPA-relevanter Inhalt
- [ ] **Ads Declaration**: "Nein, diese App enthaelt keine Werbung"
- [ ] Privacy Policy URL verlinkt

**Technische Hinweise**:
- Data Safety Section ist seit 2022 Pflicht
- Content Rating ueber IARC-Fragebogen in der Play Console
- Google prueft die Data Safety Angaben -- Inkonsistenzen fuehren zur Ablehnung

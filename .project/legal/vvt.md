# Verzeichnis der Verarbeitungstaetigkeiten (VVT)

**gemaess Art. 30 Abs. 1 DSGVO**

---

## Angaben zum Verantwortlichen

| Feld | Wert |
|------|------|
| **Name des Verantwortlichen** | Fakturus GmbH |
| **Anschrift** | Florian Geyer Str. 28, 71034 Boeblingen |
| **Vertreter** | Mike Schober (Geschaeftsfuehrer) |
| **Kontakt Datenschutz** | datenschutz@fakturus.com |
| **Datenschutzbeauftragter** | Nicht bestellt (< 20 Personen regelmaessig mit Verarbeitung beschaeftigt, keine Kerntaetigkeit in der Verarbeitung besonderer Kategorien, Art. 37 DSGVO / §38 BDSG) |

---

## Verarbeitungstaetigkeit 1: Registrierung & Authentifizierung

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Benutzerregistrierung und Anmeldung |
| **Zweck** | Erstellung und Verwaltung von Benutzerkonten, Authentifizierung |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. b DSGVO (Vertragsdurchfuehrung) |
| **Kategorien betroffener Personen** | Nutzer der App fakturus.track (Arbeitnehmer, Freiberufler) |
| **Kategorien personenbezogener Daten** | E-Mail-Adresse, Name (optional), OAuth-Token, Azure AD B2C User-ID |
| **Empfaenger** | Microsoft Azure B2C (Auftragsverarbeiter, EU-Rechenzentrum) |
| **Drittlandtransfer** | Nein (Verarbeitung ausschliesslich in EU/EWR) |
| **Loeschfrist** | 30 Tage nach Kontodeloeschung |
| **TOM (Technische und organisatorische Massnahmen)** | TLS-Verschluesselung, OAuth 2.0 / OpenID Connect, Token-basierte Auth, kein Passwort-Speicher auf eigenen Servern |

---

## Verarbeitungstaetigkeit 2: Zeiterfassung (Kernfunktion)

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Erfassung und Speicherung von Arbeitszeiten |
| **Zweck** | Arbeitszeiterfassung gemaess ArbZG, Ueberstundenberechnung, Pausenerfassung |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. b DSGVO (Vertragsdurchfuehrung) |
| **Kategorien betroffener Personen** | Nutzer der App |
| **Kategorien personenbezogener Daten** | Arbeitsdatum, Startzeit, Endzeit, Pausendauer (Minuten), Sync-Status, Geraete-UUID |
| **Empfaenger** | Microsoft Azure (Auftragsverarbeiter, EU-Rechenzentrum Deutschland) |
| **Drittlandtransfer** | Nein |
| **Loeschfrist** | 30 Tage nach Kontodeloeschung |
| **TOM** | TLS-Verschluesselung bei Uebertragung, verschluesselte Speicherung in Azure PostgreSQL, Zugriff nur mit authentifiziertem Token, Daten-Isolation per User-ID |

---

## Verarbeitungstaetigkeit 3: Urlaubsverwaltung

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Verwaltung von Urlaubs- und Krankheitstagen |
| **Zweck** | Urlaubsplanung, Resturlaubsberechnung, Dokumentation von Krankheitstagen |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. b DSGVO (Vertragsdurchfuehrung) |
| **Kategorien betroffener Personen** | Nutzer der App |
| **Kategorien personenbezogener Daten** | Urlaubstage (Datum), Krankheitstage (Datum) |
| **Besondere Kategorie** | Krankheitstage koennten als Gesundheitsdaten (Art. 9 DSGVO) gelten. Die Verarbeitung beschraenkt sich auf das Datum -- keine Diagnosen, keine Krankheitsart. Rechtsgrundlage: Art. 9 Abs. 2 lit. b (Arbeitsrecht) bzw. Art. 6 Abs. 1 lit. b bei reiner Datumserfassung. **Klaerung durch Anwalt empfohlen.** |
| **Empfaenger** | Microsoft Azure (Auftragsverarbeiter, EU) |
| **Drittlandtransfer** | Nein |
| **Loeschfrist** | 30 Tage nach Kontodeloeschung |
| **TOM** | Wie Verarbeitungstaetigkeit 2 |

---

## Verarbeitungstaetigkeit 4: Benutzereinstellungen

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Speicherung von Benutzereinstellungen |
| **Zweck** | Korrekte Berechnung von Soll-Stunden, Feiertagen, Schulferien |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. b DSGVO (Vertragsdurchfuehrung) |
| **Kategorien betroffener Personen** | Nutzer der App |
| **Kategorien personenbezogener Daten** | Bundesland, Wochenarbeitsstunden, Arbeitstage (Bitmask Mo-So), Urlaubstage pro Jahr, Kalender-URL (optional), Personalnummer (optional, fuer DATEV-Export) |
| **Empfaenger** | Microsoft Azure (Auftragsverarbeiter, EU) |
| **Drittlandtransfer** | Nein |
| **Loeschfrist** | 30 Tage nach Kontodeloeschung |
| **TOM** | Wie Verarbeitungstaetigkeit 2 |

---

## Verarbeitungstaetigkeit 5: Synchronisation

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Geraeteuebergreifende Datensynchronisation |
| **Zweck** | Nutzung auf mehreren Geraeten, Datensicherung, Offline-Faehigkeit |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. b DSGVO (Vertragsdurchfuehrung) |
| **Kategorien betroffener Personen** | Nutzer der App |
| **Kategorien personenbezogener Daten** | Alle in Verarbeitungstaetigkeiten 2-4 genannten Daten |
| **Empfaenger** | Microsoft Azure (Auftragsverarbeiter, EU) |
| **Drittlandtransfer** | Nein |
| **Loeschfrist** | 30 Tage nach Kontodeloeschung |
| **TOM** | TLS 1.2+ bei Uebertragung, JWT Bearer Token Authentifizierung, Sync-Intervall 30 Sekunden, Konfliktloesung: Server als Source of Truth |

---

## Verarbeitungstaetigkeit 6: Zustimmungsspeicherung (Consent-Tracking)

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Speicherung der Zustimmung zu AGB und Kenntnisnahme der Datenschutzerklaerung |
| **Zweck** | Nachweis der Zustimmung gemaess DSGVO Art. 7 Abs. 1 und BGB §305 |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. f DSGVO (berechtigtes Interesse: Nachweisbarkeit) |
| **Kategorien betroffener Personen** | Nutzer der App |
| **Kategorien personenbezogener Daten** | User-ID, Dokumenttyp, Dokumentversion, Zustimmungszeitpunkt (UTC), IP-Adresse, App-Version, Plattform (iOS/Android) |
| **Empfaenger** | Microsoft Azure (Auftragsverarbeiter, EU) |
| **Drittlandtransfer** | Nein |
| **Loeschfrist** | 30 Tage nach Kontodeloeschung (Aufbewahrung fuer Nachweis) |
| **TOM** | Append-Only Audit-Trail (kein Ueberschreiben), Server-generierter Zeitstempel |

---

## Verarbeitungstaetigkeit 7: In-App-Kaeufe (Abo-Verwaltung)

| Feld | Beschreibung |
|------|-------------|
| **Bezeichnung** | Verwaltung von Abonnements und Premium-Funktionen |
| **Zweck** | Freischaltung von Premium-Features basierend auf Abo-Status |
| **Rechtsgrundlage** | Art. 6 Abs. 1 lit. b DSGVO (Vertragsdurchfuehrung) |
| **Kategorien betroffener Personen** | Nutzer mit STARTER- oder PRO-Abo |
| **Kategorien personenbezogener Daten** | Transaktions-ID (Apple/Google), Abo-Status, Tier (FREE/STARTER/PRO) |
| **Empfaenger** | Apple Inc. (App Store, gemeinsam Verantwortlicher fuer iOS-Transaktionen), Google LLC (Play Store, gemeinsam Verantwortlicher fuer Android-Transaktionen) |
| **Drittlandtransfer** | Apple/Google verarbeiten Zahlungsdaten in den USA -- die App selbst erhaelt KEINE Zahlungsdaten, nur Transaktions-IDs und Abo-Status. Angemessenheitsbeschluss EU-US Data Privacy Framework. |
| **Loeschfrist** | Abo-Status lokal bei Kontodeloeschung geloescht. Apple/Google behalten eigene Transaktionsdaten gemaess eigener Richtlinien. |
| **TOM** | Keine Zahlungsdaten auf eigenen Servern, StoreKit/Billing Library als Vermittler |

---

## Auftragsverarbeiter (Art. 28 DSGVO)

| Auftragsverarbeiter | Zweck | Standort | AVV-Status |
|---------------------|-------|----------|------------|
| **Microsoft Azure** (Microsoft Ireland Operations Ltd.) | Hosting, Datenbank (PostgreSQL), Key Vault | EU-Rechenzentrum Deutschland | Microsoft DPA (Data Processing Addendum) -- **muss aktiv akzeptiert und dokumentiert werden** |
| **Microsoft Azure AD B2C** (Microsoft Ireland Operations Ltd.) | Authentifizierung, Identity Management | EU | Im Azure DPA enthalten |
| **Apple Inc.** | In-App-Kaeufe (iOS) | USA (Angemessenheitsbeschluss) | Gemeinsam Verantwortliche (kein AVV, sondern Apple Media Services AGB) |
| **Google LLC** | In-App-Kaeufe (Android) | USA (Angemessenheitsbeschluss) | Gemeinsam Verantwortliche (kein AVV, sondern Google Play Developer Distribution Agreement) |

### Handlungsbedarf AVV

- [ ] Microsoft DPA im Azure Portal akzeptieren und Akzeptanzdatum dokumentieren
- [ ] Dokumentation des DPA-Status in diesem Verzeichnis aktualisieren

---

## Technische und organisatorische Massnahmen (TOM) -- Uebersicht

### Vertraulichkeit
- Zugriffskontrolle: JWT Bearer Token, Azure AD B2C, rollenbasiert (User-ID Isolation)
- Verschluesselung: TLS 1.2+ bei Uebertragung, Azure Storage Encryption at Rest
- Keine gemeinsamen Datenbanken zwischen Nutzern (logische Trennung per User-ID)

### Integritaet
- Eingabevalidierung: FluentValidation auf allen API-Endpoints
- Audit-Trail: Consent-Historie als Append-Only
- Aenderungsprotokoll: `updatedAt` Timestamp auf allen Entitaeten

### Verfuegbarkeit
- Azure-Hosting mit automatischen Backups
- Offline-First Architektur: App funktioniert ohne Serververbindung
- Automatische Datensynchronisation bei Verbindungswiederherstellung

### Belastbarkeit
- Rate Limiting auf API-Endpoints
- Automatische Datenbank-Migrationen beim Deployment
- Healthcheck-Endpoint fuer Monitoring

### Datensparsamkeit
- Kein Tracking, keine Analytics, keine Werbung
- Keine Cookies auf Webseiten
- Minimale Datenerhebung: nur fuer die Kernfunktion erforderliche Daten

---

## Regelmaessige Ueberpruefung

Dieses Verzeichnis wird ueberprueft und aktualisiert bei:
- Einfuehrung neuer Features die personenbezogene Daten betreffen
- Aenderung von Auftragsverarbeitern oder Hosting-Infrastruktur
- Aenderung der Rechtsgrundlagen
- Mindestens einmal jaehrlich

---

**Erstellt:** 01.04.2026
**Letzte Aktualisierung:** 01.04.2026
**Verantwortlich:** Mike Schober (Geschaeftsfuehrer, Fakturus GmbH)
**Naechste planmaessige Ueberpruefung:** 01.04.2027

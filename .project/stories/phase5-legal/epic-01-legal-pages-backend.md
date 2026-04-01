# EPIC 01: Legal Pages Backend

## Ziel

Das Backend liefert die rechtlich erforderlichen Seiten (Datenschutzerklaerung, Nutzungsbedingungen/AGB, Impressum) unter oeffentlich erreichbaren URLs aus. Zusaetzlich stellt eine API versionierte Metadaten bereit, damit die App pruefen kann, ob sich rechtliche Dokumente geaendert haben.

## Abhaengigkeiten

- **Keine technischen Abhaengigkeiten**: Kann sofort gestartet werden
- Backend-Projekt: `Fakturus.Track.Backend/`
- Bestehende URL-Referenzen in der App:
  - `https://track.fakturus.com/privacy` (SettingsView.swift, PaywallView.swift)
  - `https://track.fakturus.com/terms` (PaywallView.swift)
  - `https://track.fakturus.com/imprint` (SettingsView.swift)

---

## Stories

### P5-E01-S01: Statische HTML-Seiten fuer Legal Pages

**Als** Nutzer oder Store-Reviewer
**moechte ich** die Datenschutzerklaerung, AGB und das Impressum als Webseite aufrufen koennen,
**damit** ich meine Rechte und die Bedingungen der App-Nutzung jederzeit einsehen kann.

**Plattform**: Backend
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle E01-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] GET `https://track.fakturus.com/privacy` liefert eine HTML-Seite mit der Datenschutzerklaerung
- [ ] GET `https://track.fakturus.com/terms` liefert eine HTML-Seite mit den AGB/Nutzungsbedingungen
- [ ] GET `https://track.fakturus.com/imprint` liefert eine HTML-Seite mit dem Impressum
- [ ] Alle Seiten sind **ohne Authentifizierung** erreichbar (oeffentlich)
- [ ] Alle Seiten haben ein einheitliches, minimales Design (Logo, Firmenfarben, responsive)
- [ ] Seiten laden in < 2 Sekunden (statischer Content, ggf. CDN/Caching)
- [ ] HTML ist semantisch korrekt (`<h1>`, `<h2>`, `<p>`, `<ul>`) fuer Barrierefreiheit
- [ ] Meta-Tags fuer SEO und Social Sharing sind gesetzt (title, description, og:tags)
- [ ] Seiten enthalten KEIN Tracking (keine Cookies, kein Analytics-Script)

**Technische Hinweise**:
- Optionen: (a) Statische HTML-Dateien im wwwroot, (b) Razor Pages, (c) Minimal API mit HTML-Response
- Empfehlung: Statische Dateien unter `wwwroot/legal/` mit Middleware fuer URL-Rewriting (`/privacy` -> `/legal/privacy.html`)
- Alternativ: Einfache Razor Pages fuer Layout-Sharing
- Content-Type: `text/html; charset=utf-8`
- Cache-Control: `public, max-age=86400` (1 Tag) -- rechtliche Texte aendern sich selten

---

### P5-E01-S02: Sprachumschaltung (DE/EN)

**Als** englischsprachiger Nutzer
**moechte ich** die rechtlichen Seiten auf Englisch lesen koennen,
**damit** ich die Bedingungen in meiner Sprache verstehe.

**Plattform**: Backend
**Abhaengigkeiten**: P5-E01-S01
**Parallelisierbar mit**: E01-S03, E01-S04
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Jede Legal Page unterstuetzt `?lang=de` und `?lang=en` als Query-Parameter
- [ ] Ohne Parameter: Sprache wird aus `Accept-Language` Header abgeleitet, Default: Deutsch
- [ ] Auf der Seite gibt es einen sichtbaren Sprachumschalter (DE | EN)
- [ ] Die URL aendert sich beim Sprachwechsel (`/privacy?lang=en`)
- [ ] Alle Pflichtinhalte (Impressum, Datenschutz, AGB) sind in beiden Sprachen vollstaendig
- [ ] Bei fehlender Uebersetzung wird auf die deutsche Version zurueckgefallen (Deutsch ist rechtlich bindend)
- [ ] Given ein iOS-Nutzer mit Systemsprache Englisch oeffnet `/privacy` Then wird die englische Version angezeigt

**Technische Hinweise**:
- Einfachste Loesung: Separate HTML-Dateien (`privacy-de.html`, `privacy-en.html`)
- Oder: Ein Template mit i18n-Strings
- Wichtig: Die **deutsche Version ist rechtlich bindend** -- dies muss in der englischen Version als Hinweis stehen

---

### P5-E01-S03: Legal Document Version API

**Als** App-Entwickler
**moechte ich** per API die aktuelle Version aller rechtlichen Dokumente abfragen koennen,
**damit** die App pruefen kann, ob der Nutzer der aktuellsten Version zugestimmt hat.

**Plattform**: Backend
**Abhaengigkeiten**: Keine
**Parallelisierbar mit**: Alle E01-Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] GET `/api/legal/versions` liefert JSON mit allen Dokumenten und ihren aktuellen Versionen
- [ ] Response-Format:
  ```json
  {
    "documents": [
      {
        "type": "privacy_policy",
        "version": 1,
        "effectiveDate": "2026-04-01",
        "url": "https://track.fakturus.com/privacy",
        "requiresConsent": true
      },
      {
        "type": "terms_of_service",
        "version": 1,
        "effectiveDate": "2026-04-01",
        "url": "https://track.fakturus.com/terms",
        "requiresConsent": true
      },
      {
        "type": "imprint",
        "version": 1,
        "effectiveDate": "2026-04-01",
        "url": "https://track.fakturus.com/imprint",
        "requiresConsent": false
      }
    ]
  }
  ```
- [ ] Endpoint ist **ohne Authentifizierung** erreichbar
- [ ] Versionen sind einfache inkrementelle Nummern (v1, v2, v3 -- kein SemVer)
- [ ] Jede Version hat ein explizites boolean-Feld `requiresReConsent` das manuell gesetzt wird
- [ ] `requiresConsent: true` bedeutet: Nutzer muss aktiv zustimmen (AGB)
- [ ] `effectiveDate` gibt an, ab wann die Version gilt
- [ ] Response enthaelt `Cache-Control: public, max-age=3600` (1 Stunde)

**Technische Hinweise**:
- Versionen koennten in einer Config-Datei (`legal-versions.json`) oder in der Datenbank gespeichert werden
- Empfehlung: Config-Datei im Repository (Aenderungen sind nachvollziehbar via Git-History)
- Bei Major-Version-Aenderung muss die App den Consent-Flow erneut triggern

---

### P5-E01-S04: Consent Storage API

**Als** App
**moechte ich** die Zustimmung des Nutzers zu rechtlichen Dokumenten an das Backend uebermitteln koennen,
**damit** die Zustimmung rechtssicher und nachweisbar gespeichert ist.

**Plattform**: Backend
**Abhaengigkeiten**: P5-E01-S03 (Versions-Schema muss definiert sein)
**Parallelisierbar mit**: E01-S01, E01-S02
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] POST `/api/legal/consent` akzeptiert die Zustimmung eines authentifizierten Nutzers
- [ ] Request-Format:
  ```json
  {
    "consents": [
      {
        "documentType": "privacy_policy",
        "documentVersion": "1.0.0",
        "consentGiven": true
      },
      {
        "documentType": "terms_of_service",
        "documentVersion": "1.0.0",
        "consentGiven": true
      }
    ]
  }
  ```
- [ ] Backend speichert pro Consent-Eintrag:
  - UserId (aus Auth-Token)
  - DocumentType
  - DocumentVersion
  - ConsentGiven (bool)
  - ConsentTimestamp (UTC, Server-generiert)
  - IPAddress (fuer Nachweisbarkeit)
  - AppVersion (aus Request-Header)
  - Platform (ios/android, aus Request-Header)
- [ ] GET `/api/legal/consent` liefert den aktuellen Consent-Status des authentifizierten Nutzers
- [ ] Response-Format:
  ```json
  {
    "consents": [
      {
        "documentType": "privacy_policy",
        "documentVersion": "1.0.0",
        "consentGiven": true,
        "consentTimestamp": "2026-04-01T10:30:00Z"
      }
    ],
    "allRequiredConsentsGiven": true,
    "pendingConsents": []
  }
  ```
- [ ] `allRequiredConsentsGiven: false` und `pendingConsents` listet Dokumente, bei denen der Consent fehlt oder veraltet ist
- [ ] Consent-Historie wird NICHT ueberschrieben -- jede Zustimmung wird als neuer Eintrag gespeichert (Audit-Trail)
- [ ] Endpoint erfordert Authentifizierung (Bearer Token)
- [ ] Bei Account-Loeschung werden Consent-Daten nach 30 Tagen geloescht (DSGVO Art. 17)
- [ ] Validation: `consentGiven: false` wird akzeptiert aber als "abgelehnt" gespeichert (der Nutzer darf ablehnen, aber die App blockiert dann)

**Technische Hinweise**:
- Neue Datenbank-Tabelle `UserConsents` mit Feldern: Id, UserId, DocumentType, DocumentVersion, ConsentGiven, ConsentTimestamp, IpAddress, AppVersion, Platform
- Index auf (UserId, DocumentType) fuer schnelle Abfragen
- Die IP-Adresse ist fuer den Nachweis der Zustimmung relevant (DSGVO Art. 7 Abs. 1)
- Consent-Daten sind personenbezogen -- bei DSGVO-Auskunftsanfrage (Art. 15) mit ausliefern
- Rate Limiting: Max. 10 POST-Requests pro User pro Minute (Schutz vor Missbrauch)

---

### P5-E01-S05: Datenbank-Migration fuer UserConsents

**Als** Backend-Entwickler
**moechte ich** eine saubere Datenbank-Migration fuer die Consent-Tabelle haben,
**damit** die Consent-Daten persistent und performant gespeichert werden.

**Plattform**: Backend
**Abhaengigkeiten**: P5-E01-S04 (Schema muss definiert sein)
**Parallelisierbar mit**: E01-S01, E01-S02
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] EF Core Migration erstellt die Tabelle `UserConsents`
- [ ] Spalten:
  - `Id` (Guid, Primary Key)
  - `UserId` (string, not null, Foreign Key auf Users)
  - `DocumentType` (string, not null) -- Werte: "privacy_policy", "terms_of_service"
  - `DocumentVersion` (string, not null) -- z.B. "1.0.0"
  - `ConsentGiven` (bool, not null)
  - `ConsentTimestamp` (DateTimeOffset, not null)
  - `IpAddress` (string, nullable)
  - `AppVersion` (string, nullable)
  - `Platform` (string, nullable) -- "ios", "android"
- [ ] Index auf `(UserId, DocumentType)` fuer schnelle Abfragen
- [ ] Index auf `UserId` fuer Account-Loeschung
- [ ] Migration laeuft ohne Datenverlust auf bestehender Datenbank
- [ ] Rollback-Migration ist definiert (DropTable)

**Technische Hinweise**:
- EF Core Migration mit `dotnet ef migrations add AddUserConsentsTable`
- Kein Cascade-Delete auf UserId -- Consent-Daten muessen explizit bei Account-Loeschung behandelt werden (30-Tage Frist)

---

## Zusammenfassung

| Story | Titel | Aufwand | Prioritaet |
|-------|-------|---------|------------|
| S01 | Statische HTML-Seiten | M | Must-Have |
| S02 | Sprachumschaltung DE/EN | S | Must-Have |
| S03 | Legal Document Version API | M | Must-Have |
| S04 | Consent Storage API | L | Must-Have |
| S05 | DB-Migration UserConsents | S | Must-Have |

**Gesamt**: ~1 Woche bei einem Entwickler

**Alle Stories sind Must-Have**: Ohne Backend-Endpoints zeigen die bereits in der App verlinkten URLs auf 404-Fehler. Ohne Consent-API kann kein nachweisbarer Consent gespeichert werden.

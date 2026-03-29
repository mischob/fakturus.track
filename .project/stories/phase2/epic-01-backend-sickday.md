# EPIC 01: Backend -- SickDay Entity & Endpoints

## Ziel

Das Backend um eine neue Entity `SickDay` erweitern, die Krankheitstage als eigenstaendigen Abwesenheitstyp abbildet. Die API-Endpoints folgen dem gleichen Muster wie VacationDay (CRUD + Sync). Diese Arbeit ist Voraussetzung fuer die Frontend-Integration der Krankheitstage in E04.

## Abhaengigkeiten

- **Phase 1 abgeschlossen**: Backend laeuft produktiv mit WorkSession, VacationDay, Settings
- **Keine Frontend-Abhaengigkeit**: Backend-Aenderungen koennen unabhaengig von iOS/Android entwickelt werden

## Design-Entscheidung

**Separate SickDay-Entity** (nicht Erweiterung von VacationDay mit Typ-Feld):
- Kein Breaking Change an bestehender VacationDay-Entity/API
- YAGNI -- weitere Abwesenheitstypen spaeter bei Bedarf
- Haelt Sync-Logik einfach und parallel zu VacationDay
- Siehe `marketing-integration-changelog.md` Entscheidung #2

---

## Stories

### P2-E01-S01: SickDay Entity & Datenbank-Migration

**Als** Backend-Entwickler
**moechte ich** eine SickDay-Entity im Datenmodell haben,
**damit** Krankheitstage persistiert und abgefragt werden koennen.

**Plattform**: Backend (ASP.NET Core)
**Abhaengigkeiten**: Keine (Phase 1 Backend ist Baseline)
**Parallelisierbar mit**: Alle E02/E05 Stories (unabhaengig)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Entity `SickDay` erstellt mit Properties:
  - `Id` (Guid, PK)
  - `UserId` (string, FK zu B2C User)
  - `Date` (DateOnly)
  - `CreatedAt` (DateTime)
  - `UpdatedAt` (DateTime)
  - `SyncedAt` (DateTime?)
- [ ] EF Core Migration erstellt und anwendbar
- [ ] Given die Migration wird ausgefuehrt
  When die Datenbank geprueft wird
  Then existiert die Tabelle `SickDays` mit allen Spalten
- [ ] Index auf `UserId` fuer performante Abfragen

**Technische Hinweise**:
- Analog zu `VacationDay`-Entity implementieren
- `dotnet ef migrations add AddSickDayEntity`
- DbContext um `DbSet<SickDay> SickDays` erweitern

---

### P2-E01-S02: SickDay CRUD-Endpoints

**Als** Mobile-App
**moechte ich** Krankheitstage ueber die API erstellen und loeschen koennen,
**damit** Krankheitstage serverseitig persistiert werden.

**Plattform**: Backend
**Abhaengigkeiten**: P2-E01-S01 (Entity muss existieren)
**Parallelisierbar mit**: Alle E02/E05 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `GET /v1/sick-days?from={date}&to={date}` implementiert:
  - Gibt alle SickDays des authentifizierten Users im Zeitraum zurueck
  - Response: Array von SickDayDTO
  - Given ein User hat 3 Krankheitstage im Maerz 2026
    When GET /v1/sick-days?from=2026-03-01&to=2026-03-31 aufgerufen wird
    Then werden genau 3 SickDays zurueckgegeben
- [ ] `POST /v1/sick-days` implementiert:
  - Erstellt neuen SickDay
  - Request: `{ "Date": "2026-03-15" }`
  - Response: 201 Created mit erstelltem SickDayDTO
  - Given ein valides Datum wird gesendet
    When POST /v1/sick-days aufgerufen wird
    Then wird der Krankheitstag erstellt und zurueckgegeben
- [ ] `DELETE /v1/sick-days/{id}` implementiert:
  - Loescht SickDay des authentifizierten Users
  - Response: 204 No Content
  - Given ein SickDay mit ID existiert
    When DELETE aufgerufen wird
    Then wird der Eintrag geloescht
  - Given die ID existiert nicht oder gehoert einem anderen User
    When DELETE aufgerufen wird
    Then wird 404 zurueckgegeben
- [ ] Alle Endpoints erfordern Bearer Token
- [ ] User kann nur eigene SickDays sehen/aendern

**Technische Hinweise**:
- FastEndpoints nutzen (wie bestehende Endpoints)
- Swagger-Dokumentation automatisch via FastEndpoints
- Validierung: Datum darf nicht in der Zukunft liegen (optional, je nach Geschaeftslogik)

---

### P2-E01-S03: SickDay Sync-Endpoint

**Als** Mobile-App
**moechte ich** Krankheitstage via Bulk-Sync synchronisieren koennen,
**damit** die Offline-First-Architektur auch fuer Krankheitstage funktioniert.

**Plattform**: Backend
**Abhaengigkeiten**: P2-E01-S02 (CRUD-Endpoints)
**Parallelisierbar mit**: Alle E02/E03/E05 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `POST /v1/sick-days/sync` implementiert:
  - Request: `{ "SickDays": [{ "Id": "guid", "Date": "2026-03-15", "CreatedAt": "...", "UpdatedAt": "...", "SyncedAt": "..." }] }`
  - Response: `{ "ServerSickDays": [...], "DeletedIds": ["guid1", "guid2"] }`
- [ ] Sync-Logik identisch zu VacationDay-Sync:
  - Client sendet ALLE lokalen SickDays (nicht nur pending)
  - Backend vergleicht gesendete Liste mit Server-Liste
  - Neue Eintraege werden auf dem Server erstellt
  - Fehlende Eintraege werden als geloescht erkannt
  - `DeletedIds` enthaelt IDs der auf dem Server geloeschten Tage
  - `ServerSickDays` enthaelt die aktuelle Server-Liste
- [ ] Given Client sendet 3 SickDays, Server hat 4 (einer wurde lokal geloescht)
  When POST /v1/sick-days/sync aufgerufen wird
  Then enthaelt DeletedIds den geloeschten SickDay
  And ServerSickDays enthaelt 3 Eintraege
- [ ] Given Client sendet 4 SickDays, Server hat 3 (einer ist neu auf dem Client)
  When POST /v1/sick-days/sync aufgerufen wird
  Then wird der neue SickDay auf dem Server erstellt
  And ServerSickDays enthaelt 4 Eintraege
- [ ] SyncedAt wird bei erfolgreicher Synchronisation gesetzt

**Technische Hinweise**:
- Implementierung analog zu `VacationDaySyncEndpoint`
- ACHTUNG: Client sendet ALLE Tage, nicht nur pending -- das Backend leitet Loeschungen aus dem Vergleich ab
- Transaktionale Verarbeitung (alles oder nichts)

---

### P2-E01-S04: OvertimeSummary um Krankheitstage erweitern

**Als** Mobile-App
**moechte ich** in der Overtime-Summary auch Krankheitstage sehen,
**damit** Soll-Stunden korrekt berechnet werden und Krankheitstage sichtbar sind.

**Plattform**: Backend
**Abhaengigkeiten**: P2-E01-S01 (SickDay Entity)
**Parallelisierbar mit**: P2-E01-S02/S03, alle E02/E05 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `GET /v1/overtime-summary?year=2026` Response um neue Felder erweitert:
  - `SickDaysTaken: int` -- Anzahl Krankheitstage im Jahr
  - `MonthlyOvertime[].SickDays: int` -- Krankheitstage pro Monat (optional, nice-to-have)
- [ ] Krankheitstage reduzieren die Soll-Stunden:
  - Given ein Nutzer hat 40h/Woche (Mo-Fr = 8h/Tag) und 2 Krankheitstage im Maerz
    When die Overtime-Summary fuer Maerz abgerufen wird
    Then sind die ExpectedHours um 16h (2 * 8h) reduziert
- [ ] Krankheitstage werden NICHT vom Urlaubskontingent abgezogen:
  - Given 2 Krankheitstage und 3 Urlaubstage
    When die Summary abgerufen wird
    Then VacationDaysTaken = 3 (nicht 5)
    And SickDaysTaken = 2
- [ ] Abwaertskompatibilitaet: Bestehende Response-Felder bleiben unveraendert

**Technische Hinweise**:
- Die bestehende Soll-Stunden-Berechnung muss SickDays beruecksichtigen (analog zu Feiertagen und Urlaubstagen)
- Formel: `ExpectedHours = Arbeitstage_im_Monat * Stunden_pro_Tag - (Feiertage + Urlaubstage + Krankheitstage) * Stunden_pro_Tag`
- Neues Feld ist additiv (kein Breaking Change fuer bestehende Clients)

---

### P2-E01-S05: Backend: Settings-Endpoint um UpdatedAt erweitern

**Als** Mobile-App
**moechte ich** im Settings-Endpoint ein `UpdatedAt`-Feld haben,
**damit** Last-Write-Wins-Sync auf Basis eines zuverlaessigen Server-Timestamps funktioniert.

**Plattform**: Backend (ASP.NET Core)
**Abhaengigkeiten**: Phase 1 (Settings-Endpoint existiert)
**Parallelisierbar mit**: Alle E02/E03/E04/E05 Stories
**Geschaetzter Aufwand**: S
**Prerequisite fuer**: E02 Last-Write-Wins Settings-Sync

**Akzeptanzkriterien**:
- [ ] `GET /v1/settings` Response um `UpdatedAt` erweitert:
  - Beispiel: `"UpdatedAt": "2026-03-29T10:00:00Z"`
  - Neues Feld in Settings-DTO, nicht-nullable
- [ ] `PUT /v1/settings` setzt `UpdatedAt` automatisch auf Server-Timestamp:
  - Client sendet KEIN `UpdatedAt` im Request
  - Server setzt `UpdatedAt = DateTime.UtcNow` bei jedem PUT
  - Given Settings werden via PUT aktualisiert
    When GET /v1/settings aufgerufen wird
    Then ist `UpdatedAt` >= Zeitpunkt des PUT-Aufrufs
- [ ] Abwaertskompatibilitaet: Bestehende Request-/Response-Felder bleiben unveraendert
- [ ] Migration setzt `UpdatedAt` fuer bestehende Eintraege auf `DateTime.UtcNow`

**Technische Hinweise**:
- EF Core Migration: `ALTER TABLE Settings ADD UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()`
- Settings-Entity um `UpdatedAt`-Property erweitern
- Im PUT-Handler: `settings.UpdatedAt = DateTime.UtcNow` vor `SaveChangesAsync()`

# Backend-Integration -- Native Apps

## Bestehendes Backend

Das Backend ist produktionsreif und benoetigt fuer Phase 1 **keine Aenderungen**. Die nativen Apps nutzen exakt die gleichen API-Endpunkte wie die bestehende MAUI-App und das Web-Frontend.

### Basis-Konfiguration

| Parameter | Wert |
|-----------|------|
| Base URL (Produktion) | `https://api.track.fakturus.com` |
| Base URL (Entwicklung) | `https://localhost:7001` |
| API Version | v1 (URL-Segment: `/v1/...`) |
| Content-Type | `application/json` |
| Auth | Bearer Token (Azure AD B2C JWT) |
| JSON-Format | PascalCase (ASP.NET Standard) |

### Swagger/OpenAPI
Verfuegbar unter `https://api.track.fakturus.com/swagger` im Development-Modus.

---

## API-Endpunkte

### 1. Work Sessions

#### GET /v1/work-sessions
Alle Arbeitssitzungen des authentifizierten Benutzers.

**Response:** `200 OK`
```json
[
  {
    "Id": "guid",
    "UserId": "string",
    "Date": "2026-03-29",
    "StartTime": "2026-03-29T08:00:00Z",
    "StopTime": "2026-03-29T17:00:00Z",
    "PauseMinutes": 30,
    "CreatedAt": "2026-03-29T08:00:00Z",
    "UpdatedAt": "2026-03-29T17:00:00Z",
    "SyncedAt": "2026-03-29T17:01:00Z"
  }
]
```

> **Hinweis:** `CalendarEventId` ist ein rein lokales Feld (nur auf dem Geraet gespeichert, nicht vom Backend synchronisiert). Es taucht NICHT in der API-Response auf. Siehe MAUI-SyncService Kommentar: "not synced from backend".

#### GET /v1/work-sessions/{id}
Einzelne Arbeitssitzung.

#### POST /v1/work-sessions
Neue Arbeitssitzung erstellen.

**Request:**
```json
{
  "Id": "guid (vom Client generiert)",
  "Date": "2026-03-29",
  "StartTime": "2026-03-29T08:00:00Z",
  "StopTime": "2026-03-29T17:00:00Z",
  "PauseMinutes": 30
}
```

#### PUT /v1/work-sessions/{id}
Arbeitssitzung aktualisieren.

**Request:**
```json
{
  "Date": "2026-03-29",
  "StartTime": "2026-03-29T08:00:00Z",
  "StopTime": "2026-03-29T17:00:00Z",
  "PauseMinutes": 30
}
```

#### DELETE /v1/work-sessions/{id}
Arbeitssitzung loeschen. Response: `204 No Content`

#### POST /v1/work-sessions/sync
Bulk-Sync: Lokale Sessions hochladen und alle Backend-Sessions zurueckbekommen.

**Request:**
```json
{
  "WorkSessions": [
    {
      "Id": "guid",
      "Date": "2026-03-29",
      "StartTime": "2026-03-29T08:00:00Z",
      "StopTime": "2026-03-29T17:00:00Z",
      "PauseMinutes": 30
    }
  ]
}
```

**Response:** `200 OK` -- Alle Sessions des Benutzers (nach Merge)

### 2. Vacation Days

#### GET /v1/vacation-days
Alle Urlaubstage des Benutzers.

**Response:**
```json
[
  {
    "Id": "guid",
    "UserId": "string",
    "Date": "2026-07-15",
    "CreatedAt": "...",
    "UpdatedAt": "...",
    "SyncedAt": "..."
  }
]
```

#### POST /v1/vacation-days
Urlaubstag erstellen.

#### DELETE /v1/vacation-days/{id}
Urlaubstag loeschen.

#### POST /v1/vacation-days/sync
Bulk-Sync fuer Urlaubstage.

**Request:**
```json
{
  "VacationDays": [
    {
      "Id": "guid",
      "Date": "2026-07-15",
      "CreatedAt": "...",
      "UpdatedAt": "...",
      "SyncedAt": "..."
    }
  ]
}
```

**Response:** `200 OK`
```json
{
  "ServerVacationDays": [...],
  "DeletedIds": ["guid1", "guid2"]
}
```

> **Hinweis:** `DeletedIds` enthaelt die IDs der Urlaubstage, die auf dem Server geloescht wurden. Die nativen Apps MUESSEN diese IDs nutzen, um die entsprechenden lokalen Eintraege zu entfernen. Dies ist effizienter als der Set-Differenz-Ansatz der MAUI-App.

### 3. Settings

#### GET /v1/settings
Benutzereinstellungen abrufen.

**Response:**
```json
{
  "CalendarUrl": "webcal://...",
  "VacationDaysPerYear": 30,
  "WorkHoursPerWeek": 40.00,
  "WorkDays": 31,
  "Bundesland": "NW"
}
```

#### PUT /v1/settings
Benutzereinstellungen aktualisieren.

**Request:** Gleiche Struktur wie Response.

#### GET /v1/overtime-summary?year=2026
Ueberstunden-Zusammenfassung.

**Response:**
```json
{
  "TotalOvertimeHours": 12.50,
  "MonthlyOvertime": [
    {
      "Year": 2026,
      "Month": 1,
      "MonthName": "Januar",
      "OvertimeHours": 3.25,
      "WorkedHours": 171.25,
      "ExpectedHours": 168.00
    }
  ],
  "VacationDaysTaken": 5,
  "VacationDaysRemaining": 25,
  "VacationDaysPerYear": 30,
  "HolidaysTaken": 3,
  "SchoolHolidayHoursNotWorked": 24.00
}
```

### 4. Calendar

#### GET /v1/calendar
Kalender-Events aus dem iCal-Feed.

**Response:**
```json
[
  {
    "Uid": "string",
    "Summary": "Arbeit",
    "StartTime": "2026-03-29T08:00:00Z",
    "EndTime": "2026-03-29T17:00:00Z",
    "Description": "string|null",
    "Location": "string|null"
  }
]
```

### 5. School Holidays

#### GET /v1/school-holidays?year=2026
#### POST /v1/school-holidays
#### PUT /v1/school-holidays/{id}
#### DELETE /v1/school-holidays/{id}

### 6. Sick Days

#### GET /v1/sick-days?from=2026-01-01&to=2026-12-31
Alle Krankheitstage des Benutzers im angegebenen Zeitraum.

**Response:** `200 OK`
```json
[
  {
    "Id": "guid",
    "UserId": "string",
    "Date": "2026-03-15",
    "CreatedAt": "...",
    "UpdatedAt": "...",
    "SyncedAt": "..."
  }
]
```

#### POST /v1/sick-days
Krankheitstag erstellen.

**Request:**
```json
{
  "Date": "2026-03-15"
}
```

#### DELETE /v1/sick-days/{id}
Krankheitstag loeschen. Response: `204 No Content`

#### POST /v1/sick-days/sync
Bulk-Sync fuer Krankheitstage (analog VacationDay-Sync: alle lokalen senden, Response mit Server-Liste + DeletedIds).

**Request:**
```json
{
  "SickDays": [
    {
      "Id": "guid",
      "Date": "2026-03-15",
      "CreatedAt": "...",
      "UpdatedAt": "...",
      "SyncedAt": "..."
    }
  ]
}
```

**Response:** `200 OK`
```json
{
  "ServerSickDays": [...],
  "DeletedIds": ["guid1", "guid2"]
}
```

> **Hinweis:** SickDay-Sync funktioniert identisch zum VacationDay-Sync -- ALLE lokalen Krankheitstage senden, nicht nur pending. Das Backend vergleicht die gesendete Liste mit seiner eigenen und liefert `DeletedIds` zurueck.

### 7. Utility

#### GET /v1/version
```json
{
  "Version": "1.0.0",
  "Environment": "Production"
}
```

#### GET /health
Health Check Endpunkt.

---

## Sync-Strategie

### Grundprinzip: Offline-first mit Server-wins

```
┌─────────┐     ┌─────────┐     ┌──────────┐
│  Lokale  │────>│  Sync   │────>│ Backend  │
│   DB     │<────│ Manager │<────│   API    │
└─────────┘     └─────────┘     └──────────┘
     │               │
     │  pending=true  │  Online?
     │  synced=false  │  Auth OK?
```

### Sync-Ablauf WorkSessions

1. **Lokale Aenderungen sammeln**: Alle Eintraege mit `isPendingSync=true` und `isFinished=true`
2. **Hochladen**: Via `/sync` Endpunkt -- nur pending Sessions senden
3. **Backend-Antwort verarbeiten**: Komplette Liste aller Server-Sessions
4. **Merge**:
   - Neue Backend-Eintraege lokal anlegen
   - Bestehende aktualisieren (Server-wins)
   - Lokal geloeschte Eintraege, die nicht mehr im Backend sind, entfernen (Set-Differenz)
5. **Status aktualisieren**: `isPendingSync=false`, `isSynced=true`

### Sync-Ablauf VacationDays (ACHTUNG: anders als WorkSessions!)

1. **ALLE lokalen Urlaubstage sammeln** (synced + pending, nicht nur pending!)
2. **Hochladen**: Via `/sync` Endpunkt -- komplette lokale Liste senden. Das Backend vergleicht die gesendete Liste mit seiner eigenen und leitet daraus Loeschungen ab.
3. **Backend-Antwort verarbeiten**: `ServerVacationDays` + `DeletedIds`
4. **Merge**:
   - `DeletedIds` nutzen um lokale Eintraege gezielt zu loeschen
   - Neue Backend-Eintraege lokal anlegen
   - Bestehende aktualisieren (Server-wins)
5. **Status aktualisieren**: `isPendingSync=false`, `isSynced=true`

> **Warum der Unterschied?** WorkSessions werden einzeln erstellt/geloescht (CRUD). VacationDays werden als "Menge von Tagen" verwaltet -- der Nutzer markiert/demarkiert Tage. Das Backend braucht die komplette Liste, um zu erkennen welche Tage entfernt wurden.

### Sync-Ablauf SickDays (identisch zu VacationDays)

SickDays nutzen exakt den gleichen Sync-Algorithmus wie VacationDays:

1. **ALLE lokalen Krankheitstage sammeln** (synced + pending, nicht nur pending!)
2. **Hochladen**: Via `/v1/sick-days/sync` -- komplette lokale Liste senden
3. **Backend-Antwort verarbeiten**: `ServerSickDays` + `DeletedIds`
4. **Merge**:
   - `DeletedIds` nutzen um lokale Eintraege gezielt zu loeschen
   - Neue Backend-Eintraege lokal anlegen
   - Bestehende aktualisieren (Server-wins)
5. **Status aktualisieren**: `isPendingSync=false`, `isSynced=true`

### Sync-Trigger

| Trigger | Plattform | Intervall |
|---------|-----------|-----------|
| App-Start | iOS + Android | Sofort |
| Netzwerk-Wiederherstellung | iOS + Android | Sofort |
| Session beendet (Finish) | iOS + Android | Sofort |
| Pull-to-Refresh | iOS + Android | Manuell |
| Background Refresh | iOS | ~15-30min (systemgesteuert) |
| WorkManager | Android | 15min (Minimum) |
| In-App Timer | iOS + Android | 30s (wenn App aktiv) |

---

## Authentifizierung

Siehe [auth-concept.md](auth-concept.md) fuer Details.

Fuer die API-Integration relevant:
- Jeder Request benoetigt `Authorization: Bearer {token}` Header
- Token wird via MSAL Silent Token Acquisition erneuert
- Bei 401-Response: Token erneuern und Request wiederholen (1x Retry)
- Bei erneutem 401: Nutzer zum erneuten Login auffordern

---

## JSON-Handling

### PascalCase-Konvention

Das Backend liefert PascalCase JSON (ASP.NET Standard). Die nativen Apps muessen dies beruecksichtigen:

**iOS (Swift):**
```swift
let decoder = JSONDecoder()
decoder.keyDecodingStrategy = .custom { keys in
    // PascalCase zu camelCase
    let key = keys.last!.stringValue
    return AnyCodingKey(stringValue: key.prefix(1).lowercased() + key.dropFirst())!
}
```

**Android (Kotlin):**
```kotlin
// Gson
val gson = GsonBuilder()
    .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
    .create()

// oder Kotlinx.serialization mit @SerialName Annotationen
```

### Datum/Zeit-Format
- Alle Zeiten in **UTC** (ISO 8601: `2026-03-29T08:00:00Z`)
- `DateOnly` Felder als String: `2026-03-29`
- Konvertierung in lokale Zeitzone nur fuer die Anzeige

---

## Moegliche Backend-Erweiterungen (Phase 2+)

Diese Aenderungen sind **nicht blockierend** fuer Phase 1, wuerden aber die nativen Apps verbessern:

1. **Push Notifications**: Benachrichtigung bei 10h Arbeitszeit (erfordert Backend-Push-Service)
2. **Delta Sync**: Nur geaenderte Eintraege seit letztem Sync (reduziert Datentransfer)
3. **Pagination**: Fuer die History-Ansicht (derzeit werden alle Sessions geladen)
4. **Batch Delete**: Mehrere Sessions gleichzeitig loeschen
5. **User Profile Endpoint**: Name und E-Mail des B2C-Benutzers zurueckgeben

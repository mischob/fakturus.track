# Tech-Spec: EPIC 01 -- Backend SickDay Entity & Endpoints

## Dateien

### Neue Dateien (Backend)

| Datei | Beschreibung |
|-------|-------------|
| `Entities/SickDay.cs` | EF Core Entity |
| `Endpoints/SickDays/GetSickDaysEndpoint.cs` | GET mit Datumfilter |
| `Endpoints/SickDays/CreateSickDayEndpoint.cs` | POST einzelner Tag |
| `Endpoints/SickDays/DeleteSickDayEndpoint.cs` | DELETE by ID |
| `Endpoints/SickDays/SyncSickDaysEndpoint.cs` | POST Bulk-Sync |
| `Migrations/YYYYMMDDHHMMSS_AddSickDayEntity.cs` | EF Core Migration |

### Modifizierte Dateien (Backend)

| Datei | Aenderung |
|-------|-----------|
| `AppDbContext.cs` | +DbSet\<SickDay\> |
| `Endpoints/Overtime/OvertimeSummaryEndpoint.cs` | SickDays in Berechnung |
| `Models/OvertimeSummaryResponse.cs` | +SickDaysTaken |

---

## API-Contracts mit JSON-Beispielen

### GET /v1/sick-days?from=2026-03-01&to=2026-03-31

**Response 200:**
```json
[
  {
    "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "UserId": "user-b2c-id",
    "Date": "2026-03-15",
    "CreatedAt": "2026-03-15T08:00:00Z",
    "UpdatedAt": "2026-03-15T08:00:00Z",
    "SyncedAt": "2026-03-15T08:01:00Z"
  }
]
```

### POST /v1/sick-days

**Request:**
```json
{
  "Date": "2026-03-15"
}
```

**Response 201:**
```json
{
  "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "UserId": "user-b2c-id",
  "Date": "2026-03-15",
  "CreatedAt": "2026-03-15T08:00:00Z",
  "UpdatedAt": "2026-03-15T08:00:00Z",
  "SyncedAt": null
}
```

### DELETE /v1/sick-days/{id}

**Response 204** (Erfolg) oder **404** (nicht gefunden / nicht der eigene).

### POST /v1/sick-days/sync

**Request:**
```json
{
  "SickDays": [
    {
      "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "Date": "2026-03-15",
      "CreatedAt": "2026-03-15T08:00:00Z",
      "UpdatedAt": "2026-03-15T08:00:00Z",
      "SyncedAt": "2026-03-15T08:01:00Z"
    },
    {
      "Id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "Date": "2026-03-20",
      "CreatedAt": "2026-03-20T07:30:00Z",
      "UpdatedAt": "2026-03-20T07:30:00Z",
      "SyncedAt": null
    }
  ]
}
```

**Response 200:**
```json
{
  "ServerSickDays": [
    {
      "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "UserId": "user-b2c-id",
      "Date": "2026-03-15",
      "CreatedAt": "2026-03-15T08:00:00Z",
      "UpdatedAt": "2026-03-15T08:00:00Z",
      "SyncedAt": "2026-03-29T10:00:00Z"
    },
    {
      "Id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "UserId": "user-b2c-id",
      "Date": "2026-03-20",
      "CreatedAt": "2026-03-20T07:30:00Z",
      "UpdatedAt": "2026-03-29T10:00:00Z",
      "SyncedAt": "2026-03-29T10:00:00Z"
    }
  ],
  "DeletedIds": ["c3d4e5f6-a7b8-9012-cdef-123456789012"]
}
```

### GET /v1/overtime-summary?year=2026 (erweiterte Response)

**Neue Felder im Response:**
```json
{
  "TotalOvertimeHours": 12.5,
  "SickDaysTaken": 3,
  "MonthlyOvertime": [
    {
      "Year": 2026,
      "Month": 3,
      "MonthName": "Maerz",
      "OvertimeHours": 5.25,
      "WorkedHours": 168.0,
      "ExpectedHours": 162.75,
      "SickDays": 2
    }
  ],
  "VacationDaysTaken": 5,
  "VacationDaysRemaining": 25,
  "VacationDaysPerYear": 30,
  "HolidaysTaken": 2,
  "SchoolHolidayHoursNotWorked": 0.0
}
```

---

## SickDay Entity (Backend)

```csharp
public class SickDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }
}
```

---

## Sync-Algorithmus (Backend-Seite)

```
POST /v1/sick-days/sync:
1. Client-SickDays aus Request lesen
2. Server-SickDays fuer diesen User laden
3. Client-ID-Set = { alle IDs aus Request }
4. Server-ID-Set = { alle IDs aus DB }
5. Neue auf Client (nicht auf Server): INSERT in DB
6. Geloescht auf Client (auf Server, nicht in Request): In DeletedIds sammeln, DELETE aus DB
7. Bestehende: UpdatedAt aktualisieren, SyncedAt = now()
8. Response: { ServerSickDays: aktuelle DB-Liste, DeletedIds: geloeschte IDs }
```

---

## Soll-Stunden-Formel (aktualisiert)

```
ExpectedHours = Arbeitstage_im_Monat * Stunden_pro_Tag
              - (Feiertage + Urlaubstage + Krankheitstage) * Stunden_pro_Tag
```

Wobei `Stunden_pro_Tag = WorkHoursPerWeek / Anzahl_Arbeitstage_pro_Woche`.

---

## Testbare Kriterien

1. SickDay CRUD: Erstellen, Lesen, Loeschen -- Response-Codes pruefen
2. Sync mit 0 lokalen Tagen -> leere ServerSickDays, leere DeletedIds
3. Sync mit 2 neuen Tagen -> Server hat danach 2 Tage
4. Sync mit 1 Loeschung -> DeletedIds enthaelt 1 ID
5. OvertimeSummary: SickDaysTaken = Anzahl SickDays im Jahr
6. OvertimeSummary: ExpectedHours reduziert um SickDays * StundenProTag
7. Abwaertskompatibilitaet: bestehende Response-Felder unveraendert

---

## Risiken und Fallbacks

| Risiko | Wahrscheinlichkeit | Fallback |
|--------|-------------------|----------|
| Backend-Deploy blockiert Frontend-Entwicklung | Mittel | Frontend kann mit Mock-API starten; SickDay-Sync erst nach Deploy aktivieren |
| OvertimeSummary-Berechnung falsch nach Erweiterung | Niedrig | Unit-Tests fuer Berechnungslogik; Feature-Flag fuer SickDays |
| Breaking Change in OvertimeSummary-Response | Niedrig | Neues Feld ist additiv, `ignoreUnknownKeys = true` im Client |

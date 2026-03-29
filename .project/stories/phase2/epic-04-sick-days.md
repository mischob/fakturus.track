# EPIC 04: Krankheitstage (Frontend + Sync)

## Ziel

Krankheitstage als eigenstaendiger Abwesenheitstyp im Urlaub-Kalender. Nutzer koennen per Long-Press einen Tag als "Krank" markieren. Krankheitstage reduzieren die Soll-Stunden, werden aber NICHT vom Urlaubskontingent abgezogen. Die Sync-Logik folgt dem VacationDay-Muster (ALLE lokalen Tage senden).

## Abhaengigkeiten

- **E01**: Backend SickDay Entity + Endpoints muessen existieren
- **E03**: VacationCalendar-Komponente muss existieren (wird um Krankheitstage erweitert)
- **Phase 1**: SyncEngine (wird um syncSickDays() erweitert)

## Design-Entscheidung

**Long-Press mit Kontext-Menue** fuer Abwesenheitstyp-Auswahl:
- Tap = Urlaub (haeufigste Aktion, Rueckwaertskompatibilitaet)
- Long-Press auf leeren Tag = Kontext-Menue ("Urlaub" / "Krank")
- Long-Press auf markierten Tag = Kontext-Menue ("Typ wechseln" / "Entfernen")
- Siehe `ux-flows.md` Flow 8c fuer vollstaendige Spezifikation

---

## Stories

### P2-E04-S01: iOS SickDay Model + DTO + APIClient-Erweiterung

**Als** Entwickler
**moechte ich** das SickDay-Datenmodell und die API-Anbindung haben,
**damit** Krankheitstage lokal gespeichert und synchronisiert werden koennen.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E01-S03 (Backend Sync-Endpoint muss definiert sein)
**Parallelisierbar mit**: P2-E04-S02 (Android), alle E05/E06 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SickDay` SwiftData Model in `Models/` (wie in data-layer.md definiert):
  - id: UUID, userId: String, date: Date
  - createdAt, updatedAt, syncedAt
  - isPendingSync, isSynced
- [ ] DTOs in `Models/DTOs.swift` ergaenzt:
  - `SickDaySyncItem` (Encodable)
  - `SickDayDTO` (Decodable)
  - `SyncSickDaysRequest`, `SyncSickDaysResponse`
- [ ] APIClient um SickDay-Methoden erweitert:
  - `getSickDays(from: Date, to: Date) -> [SickDayDTO]`
  - `createSickDay(date: Date) -> SickDayDTO`
  - `deleteSickDay(id: String)`
  - `syncSickDays(request: SyncSickDaysRequest) -> SyncSickDaysResponse`
- [ ] SwiftData Schema-Version auf V2 aktualisiert (SickDay hinzugefuegt)
- [ ] Given ein SickDay wird erstellt
  When toDTO() aufgerufen wird
  Then wird ein korrektes SickDaySyncItem zurueckgegeben

**Technische Hinweise**:
- SickDay Model ist strukturell identisch zu VacationDay
- Schema-Migration: `VersionedSchema` von V1 auf V2 (AddSickDay)
- APIClient-Methoden analog zu VacationDay-Methoden

---

### P2-E04-S02: Android SickDay Entity + DTO + APIClient-Erweiterung

**Als** Entwickler
**moechte ich** das SickDay-Datenmodell und die API-Anbindung haben,
**damit** Krankheitstage lokal gespeichert und synchronisiert werden koennen.

**Plattform**: Android
**Abhaengigkeiten**: P2-E01-S03 (Backend Sync-Endpoint)
**Parallelisierbar mit**: P2-E04-S01 (iOS), alle E05/E06 Stories
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SickDayEntity` in Room (wie in data-layer.md definiert)
- [ ] `SickDayDao` mit Methoden: getAllOrderedByDate(), getAll(), getPendingDays(), insert(), delete(), deleteById()
- [ ] DTOs in `DTOs.kt` ergaenzt (mit @SerialName Annotationen)
- [ ] APIClient um SickDay-Methoden erweitert
- [ ] Room Schema-Migration von Version 1 auf 2 (SickDay-Tabelle hinzufuegen)
- [ ] AppDatabase um `sickDayDao()` erweitert

**Technische Hinweise**:
- Room Migration: `MIGRATION_1_2` mit `CREATE TABLE sick_days (...)`
- DAO-Pattern identisch zu VacationDayDao
- `@Entity(tableName = "sick_days")`

---

### P2-E04-S03: iOS SyncEngine um SickDay-Sync erweitern

**Als** App
**moechte ich** Krankheitstage automatisch mit dem Backend synchronisieren,
**damit** Daten auf allen Geraeten konsistent sind.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E04-S01 (SickDay Model + API), Phase 1 (SyncEngine)
**Parallelisierbar mit**: P2-E04-S04 (Android)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SyncEngine.syncSickDays()` implementiert (wie in shared-concepts.md definiert):
  1. ALLE lokalen SickDays sammeln (synced + pending, NICHT nur pending!)
  2. POST /v1/sick-days/sync mit ALLEN lokalen Tagen
  3. Response verarbeiten: DeletedIds loeschen, ServerSickDays upserten
  4. Alle als synced markieren
- [ ] `syncAll()` ruft jetzt auch `syncSickDays()` auf
- [ ] Given 3 lokale SickDays (1 pending, 2 synced) existieren
  When syncSickDays() aufgerufen wird
  Then werden ALLE 3 SickDays an den Server gesendet
  And die Response wird korrekt verarbeitet
- [ ] Given der Server hat einen SickDay geloescht (in DeletedIds)
  When syncSickDays() die Response verarbeitet
  Then wird der lokale Eintrag ebenfalls geloescht

**Technische Hinweise**:
- ACHTUNG: Gleicher Algorithmus wie VacationDay-Sync (ALLE senden, nicht nur pending!)
- Bestehende SyncEngine erweitern, NICHT neue Klasse erstellen
- Code aus `syncVacationDays()` als Vorlage nutzen

---

### P2-E04-S04: Android SyncEngine um SickDay-Sync erweitern

**Als** App
**moechte ich** Krankheitstage automatisch mit dem Backend synchronisieren,
**damit** Daten auf allen Geraeten konsistent sind.

**Plattform**: Android
**Abhaengigkeiten**: P2-E04-S02 (SickDay Entity + API), Phase 1 (SyncEngine)
**Parallelisierbar mit**: P2-E04-S03 (iOS)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] `SyncEngine.syncSickDays()` implementiert (analog zu VacationDay-Sync)
- [ ] `syncAll()` ruft `syncSickDays()` auf
- [ ] Gleiche Testszenarien wie iOS

**Technische Hinweise**:
- Code aus `syncVacationDays()` als Vorlage
- Room DAO: `sickDayDao.getAll()` fuer alle lokalen Tage

---

### P2-E04-S05: iOS Kalender um Krankheitstage erweitern (Long-Press)

**Als** Nutzer
**moechte ich** per Long-Press einen Tag als Krankheitstag markieren koennen,
**damit** ich verschiedene Abwesenheitstypen im Kalender erfassen kann.

**Plattform**: iOS
**Abhaengigkeiten**: P2-E03-S01 (VacationCalendar), P2-E04-S01 (SickDay Model), P2-E04-S03 (SickDay Sync)
**Parallelisierbar mit**: P2-E04-S06 (Android)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Krankheitstage im Kalender mit rotem Hintergrund-Kreis (`sick-day` Farbe #EF4444) dargestellt
- [ ] Long-Press auf leeren Arbeitstag zeigt Kontext-Menue:
  - "Urlaub" (Cyan Icon)
  - "Krank" (Rot Icon)
  - Given Long-Press auf den 15. Maerz (leerer Arbeitstag)
    When "Krank" ausgewaehlt wird
    Then wird ein SickDay mit date = 15.03. erstellt
    And der Tag wird rot markiert
    And Soll-Stunden werden reduziert (Anzeige in Gesamt-Tab)
    And Urlaubskontingent bleibt unveraendert
- [ ] Tap auf Krankheitstag = entfernen:
  - Given der 15. Maerz ist als Krankheitstag markiert
    When der Nutzer darauf tippt
    Then wird der SickDay geloescht
    And der Tag wird wieder normal dargestellt
- [ ] Long-Press auf markierten Tag (Urlaub ODER Krank) zeigt Kontext-Menue:
  - "Typ wechseln" -- wechselt zwischen Urlaub und Krank
  - "Entfernen" -- entfernt die Markierung komplett
  - Given der 15. Maerz ist als Urlaub markiert
    When Long-Press und "Typ wechseln" gewaehlt wird
    Then wird VacationDay geloescht und SickDay erstellt
    And der Tag wechselt von cyan zu rot
    And Resturlaub wird um 1 erhoeht
- [ ] Feiertage und Wochenenden: Long-Press wird ignoriert
- [ ] Kalender-Legende um "Krank" (roter Punkt) erweitert

**Technische Hinweise**:
- iOS: `.contextMenu { }` oder `.onLongPressGesture` + custom Popover
- `.contextMenu` ist der idiomatischere Weg auf iOS
- SickDay und VacationDay als separate Sets im ViewModel verwalten
- Beim Typ-Wechsel: Loesch- und Erstell-Operation in einer Transaktion

---

### P2-E04-S06: Android Kalender um Krankheitstage erweitern (Long-Press)

**Als** Nutzer
**moechte ich** per Long-Press einen Tag als Krankheitstag markieren koennen,
**damit** ich verschiedene Abwesenheitstypen im Kalender erfassen kann.

**Plattform**: Android
**Abhaengigkeiten**: P2-E03-S02 (VacationCalendar), P2-E04-S02 (SickDay Entity), P2-E04-S04 (SickDay Sync)
**Parallelisierbar mit**: P2-E04-S05 (iOS)
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] Gleiche Funktionalitaet wie iOS (Long-Press Kontext-Menue, Typ-Wechsel, Entfernen)
- [ ] Roter Hintergrund-Kreis fuer Krankheitstage
- [ ] Material 3 DropdownMenu als Kontext-Menue bei Long-Press

**Technische Hinweise**:
- `Modifier.combinedClickable(onLongClick = { showMenu = true }, onClick = { toggle() })`
- `DropdownMenu(expanded = showMenu)` mit `DropdownMenuItem` fuer "Urlaub" und "Krank"
- Haptic Feedback bei Long-Press: `performHapticFeedback(HapticFeedbackType.LongPress)`

---

### P2-E04-S07: VacationViewModel um Krankheitstage erweitern

**Als** Entwickler
**moechte ich** das VacationViewModel um Krankheitstage-Logik erweitern,
**damit** der Urlaub-Screen beide Abwesenheitstypen korrekt verwaltet.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P2-E03-S07/S08 (VacationViewModel), P2-E04-S01/S02 (SickDay Model)
**Parallelisierbar mit**: P2-E04-S05/S06 (parallel zur UI-Arbeit, wenn Interfaces klar sind)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] iOS VacationViewModel erweitert um:
  - `sickDays: [SickDay]` (via @Query in View oder Set im VM)
  - `sickDayCount: Int` (Anzahl im aktuellen Jahr)
  - `toggleSickDay(date: Date)` Methode
  - `switchAbsenceType(date: Date)` Methode (Urlaub <-> Krank)
- [ ] Android VacationViewModel erweitert um:
  - `sickDays: StateFlow<List<SickDayEntity>>`
  - `sickDayCount: StateFlow<Int>`
  - `toggleSickDay(date: LocalDate)` suspend
  - `switchAbsenceType(date: LocalDate)` suspend
- [ ] Given ein Tag ist als Urlaub markiert und switchAbsenceType wird aufgerufen
  When die Operation abgeschlossen ist
  Then existiert kein VacationDay fuer diesen Tag mehr
  And ein SickDay fuer diesen Tag existiert
  And isPendingSync ist true fuer beide Entities (Loeschung + Erstellung)
- [ ] Given ein Tag ist als Krank markiert und toggleSickDay wird aufgerufen
  When die Operation abgeschlossen ist
  Then existiert kein SickDay fuer diesen Tag mehr

**Technische Hinweise**:
- Typ-Wechsel ist eine atomare Operation (delete + insert in einer DB-Transaktion)
- iOS: `modelContext.transaction { ... }` (oder einfach sequentiell, SwiftData batched automatisch)
- Android: `@Transaction` Annotation oder `withTransaction { }`

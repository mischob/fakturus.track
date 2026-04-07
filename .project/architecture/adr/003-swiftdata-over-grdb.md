# ADR-003: SwiftData statt GRDB/Core Data

## Status
Akzeptiert

## Kontext
Die iOS-App braucht eine lokale Datenbank fuer Offline-Support. Optionen: SwiftData, Core Data, GRDB (SQLite Wrapper).

## Entscheidung
Wir verwenden **SwiftData** als lokale Datenbank.

## Begruendung
1. **iOS 17 Minimum**: SwiftData ist ab iOS 17 verfuegbar, unser Minimum ist iOS 17
2. **@Query Macro**: Deklarative Datenabfragen direkt in SwiftUI Views (automatische UI-Updates)
3. **@Model Macro**: Minimaler Boilerplate fuer Datenmodelle
4. **Kein Persistenz-Layer noetig**: SwiftData uebernimmt Container-Setup, Migrations, Change-Tracking
5. **Native Integration**: Kein Drittanbieter-Framework, weniger Dependencies
6. **AI-freundlich**: Deklarativer Code (@Model, @Query) ist leicht lesbar und modifizierbar

## Proof-of-Concept (Woche 3)

**Konkreter PoC-Test in Woche 3** um SwiftData-Tauglichkeit fuer die Sync-Engine zu validieren:

1. `syncWorkSessions()` mit SwiftData implementieren (wie in `shared-concepts.md` beschrieben)
2. **Performance-Test**: 500+ WorkSessions in einer In-Memory SwiftData DB anlegen, dann den kompletten Sync-Zyklus (Fetch pending, Upsert Server-Sessions, Delete local-only) ausfuehren. Akzeptabel: < 1 Sekunde.
3. **API-Limitierungen testen**:
   - `#Predicate` mit UUID-Vergleich (`$0.id == someUUID`)
   - Batch-Fetch: Alle Sessions mit `isSynced=true` laden
   - Batch-Delete: Mehrere Sessions auf einmal loeschen
4. **Ergebnis bis Ende Woche 3**: Go/No-Go Entscheidung fuer SwiftData

Falls eines dieser Kriterien nicht erfuellt wird: **Sofortiger Switch zu GRDB** (nicht erst nach Phase 1).

## Fallback
Falls der PoC in Woche 3 zeigt, dass SwiftData nicht ausreicht:
- Migration zu **GRDB** (SQLite Wrapper) -- bietet volle SQL-Kontrolle
- Die Models bleiben gleich, nur der Persistenz-Mechanismus aendert sich
- Der Switch ist in Woche 3-4 noch kostenguenstig (wenig Code geschrieben)

## Konsequenzen
- Abhaengigkeit von iOS 17+ (ist bereits entschieden)
- SwiftData ist relativ neu -- moegliche Bugs oder fehlende Features
- Kein roher SQL-Zugriff (koennte fuer komplexe Sync-Queries limitierend sein)
- PoC in Woche 3 reduziert das Risiko einer spaeten Migration erheblich

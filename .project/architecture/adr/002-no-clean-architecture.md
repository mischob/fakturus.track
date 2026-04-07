# ADR-002: Flache Architektur statt Clean Architecture

## Status
Akzeptiert

## Kontext
Der PO-Plan fuer Android sieht Clean Architecture vor (data/domain/ui Schichten, Repository Pattern, UseCase-Klassen). Wir muessen entscheiden ob das fuer diese App angemessen ist.

## Entscheidung
Wir verwenden eine **flache 2-Schichten-Architektur** (UI + Services) ohne Clean Architecture, Repository Pattern oder UseCase-Klassen.

## Begruendung
1. **App-Groesse**: 4 Screens, ~8 API-Endpunkte, 3 DB-Entities. Clean Architecture ist fuer groessere Apps konzipiert.
2. **AI-Entwicklung**: Ein AI-Agent muss bei Clean Architecture 5+ Dateien lesen um einen Flow zu verstehen (Controller -> UseCase -> Repository -> DataSource -> Entity). Bei unserer Architektur: ViewModel + View = 2 Dateien.
3. **Keine Domain-Logik**: Die App ist ein CRUD-Frontend mit Sync. Es gibt keine komplexe Business-Logik die eigene Domain-Models oder UseCases rechtfertigt.
4. **Repository-Overhead**: Room und SwiftData sind bereits Abstraktionen ueber SQLite. Ein Repository darueber ist ein Wrapper ueber einen Wrapper.
5. **DTO-Mapping-Overhead**: Die API-DTOs sind nahezu identisch mit den DB-Entities. Separate Domain-Models erzeugen nur Mapping-Code.

## Was wir stattdessen machen
- ViewModels sprechen direkt mit der Datenbank (SwiftData @Query / Room DAO)
- ViewModels sprechen direkt mit dem APIClient (fuer Online-Operationen)
- SyncEngine orchestriert DB + API (eine einzige Klasse, nicht 3 Repositories)
- DTOs werden direkt in DB-Entities konvertiert (toEntity() / update(from:))

## Konsequenzen
- Weniger Dateien, weniger Indirektionen
- ViewModels sind "dicker" (mehr Verantwortung)
- Bei starkem Wachstum (>10 Screens, komplexe Business-Logik) muesste refactored werden
- Fuer die aktuelle App-Groesse: optimal

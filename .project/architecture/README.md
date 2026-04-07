# Solution SW Concept -- Fakturus Track Native Apps

## Architektur-Entscheidungsgrundlage

Dieses Dokument beschreibt die Software-Architektur fuer die nativen iOS- und Android-Apps von Fakturus Track. Es basiert auf der PO-Planung (`.project/plan/`) und den Design-Spezifikationen (`.project/design/`).

### Leitprinzip: AI-optimierte Einfachheit

Die Architektur folgt einem fundamentalen Prinzip: **Einfachheit und Direktheit ueber Abstraktion.** Das bedeutet konkret:

- **Flache Strukturen**: Maximal 2 Ebenen zwischen UI und Netzwerk
- **Feature-Kolokation**: Alles was zu einem Feature gehoert, liegt zusammen
- **Keine unnuetzen Interfaces**: Nur bei echten multiplen Implementierungen
- **Expliziter Code**: Lieber 10 Zeilen mehr als eine clevere Abstraktion
- **Wenige Dateien pro Feature**: Ein Feature = ViewModel + View + ggf. Subviews

### Anti-Patterns (bewusst vermieden)

| Vermieden | Stattdessen | Begruendung |
|-----------|-------------|-------------|
| Clean Architecture (5 Schichten) | 2 Schichten: UI + Services | AI-Agenten verlieren Kontext ueber viele Schichten |
| Repository Pattern ueber Room/SwiftData | Direkte Nutzung der Datenbank-APIs | Unnoetige Wrapper-Schicht |
| UseCases als eigene Klassen | Methoden im ViewModel | Ein UseCase pro Datei ist Overhead fuer diese App-Groesse |
| DTO-zu-Domain-Mapping | Datenmodelle direkt nutzen | Keine Domain-Logik die eigene Modelle rechtfertigt |
| DI-Container (Hilt) auf Android | Manuelle Konstruktor-Injection | Fakturus Track hat 4 Screens -- kein Hilt noetig |
| Event Bus / Reactive Streams ueberall | Direkte Methodenaufrufe + State | Einfacher nachzuvollziehen |

### Begruendung: Warum nicht wie fakturus.poi?

Das fakturus.poi Projekt dient als **Referenz fuer bewaehrte Patterns** (Auth, APIClient, MSAL-Integration), aber wir uebernehmen nicht blind die Architektur. Folgende Probleme in fakturus.poi vermeiden wir:

1. **Massive App-Datei**: `FakturusPOIApp.swift` hat 480+ Zeilen mit 20+ `@State`-Services und ViewModels. Fuer Track: Service-Initialisierung in eine eigene Klasse auslagern.
2. **Zu viele ViewModels via Environment**: ViewModelEnvironmentsModifier mit 18 Environment-Werten. Fuer Track: ViewModels direkt in den Views erstellen, nur wenige globale Services via Environment.
3. **Keine Offline-Logik**: fakturus.poi ist rein online. Track braucht eine durchdachte Offline-Strategie.

### Was wir von fakturus.poi uebernehmen

- **APIClient** mit PascalCase-zu-camelCase Konvertierung (1:1 bewaehrt)
- **AuthManager** mit MSAL-Integration und Silent Token Renewal
- **Configuration** Enum fuer B2C-Konfiguration
- **@Observable** Pattern fuer State-Management (Swift 6)

---

## Architektur-Ueberblick

```
                        ┌─────────────────────┐
                        │    Native App UI     │
                        │  (SwiftUI / Compose) │
                        └──────────┬──────────┘
                                   │
                        ┌──────────┴──────────┐
                        │    ViewModels        │
                        │  (State + Logic)     │
                        └──────────┬──────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
    ┌─────────┴───────┐  ┌────────┴────────┐  ┌───────┴────────┐
    │    SyncEngine    │  │   APIClient     │  │  AuthManager   │
    │  (Orchestriert)  │  │  (HTTP + JSON)  │  │  (MSAL B2C)   │
    └─────────┬───────┘  └────────┬────────┘  └────────────────┘
              │                    │
    ┌─────────┴───────┐  ┌────────┴────────┐
    │  Lokale DB       │  │  Backend API    │
    │  (SwiftData/Room)│  │  (REST + JSON)  │
    └─────────────────┘  └─────────────────┘
```

Nur **2 Schichten** zwischen User und Backend:
1. **UI-Schicht**: Views + ViewModels (State, User-Interaktion, Praesentation)
2. **Service-Schicht**: APIClient, SyncEngine, AuthManager, lokale DB

---

## Dokument-Uebersicht

| Dokument | Inhalt |
|----------|--------|
| [ios-architecture.md](ios-architecture.md) | iOS-Projektstruktur, Patterns, Code-Beispiele |
| [android-architecture.md](android-architecture.md) | Android-Projektstruktur, Patterns, Code-Beispiele |
| [shared-concepts.md](shared-concepts.md) | Plattformuebergreifende Konzepte (API, Sync, Auth) |
| [data-layer.md](data-layer.md) | Lokale Datenbank, Caching, Datenmodelle |
| [security-concept.md](security-concept.md) | Token Storage, DSGVO, Verschluesselung |
| [web-app-architecture.md](web-app-architecture.md) | Blazor Server Web-App Architektur |
| [adr/](adr/) | Architecture Decision Records |

## ADR-Uebersicht

| ADR | Entscheidung |
|-----|-------------|
| [ADR-001](adr/001-native-over-crossplatform.md) | Native Apps statt Cross-Platform |
| [ADR-002](adr/002-no-clean-architecture.md) | Flache Architektur statt Clean Architecture |
| [ADR-003](adr/003-swiftdata-over-grdb.md) | SwiftData statt GRDB/Core Data |
| [ADR-004](adr/004-no-hilt-on-android.md) | Manuelle DI statt Hilt auf Android |
| [ADR-005](adr/005-sync-engine-design.md) | Sync-Engine nach bewaehrtem MAUI-Muster |
| [ADR-006](adr/006-mvvm-without-usecases.md) | MVVM ohne UseCase-Klassen |
| [ADR-007](adr/007-ktor-over-retrofit.md) | Ktor Client statt Retrofit auf Android |

# ADR-005: Sync-Engine nach bewaehrtem MAUI-Muster

## Status
Akzeptiert

## Kontext
Die bestehende MAUI-App hat eine funktionierende Sync-Logik (`SyncService.cs`, ~700 Zeilen). Wir muessen entscheiden ob wir das Sync-Design uebernehmen, vereinfachen oder neu entwerfen.

## Entscheidung
Wir **uebernehmen den Sync-Algorithmus** der MAUI-App (Schritte 1-8) als bewahre Referenz, vereinfachen aber die Orchestrierung.

## Was wir uebernehmen (bewaehrt, funktioniert in Produktion)
- Server-wins Konfliktstrategie
- Bulk-Sync via `/sync` Endpoint (hochladen + komplette Liste zurueckbekommen)
- 8-Schritt Merge-Algorithmus (pending sammeln, hochladen, server-response mergen, markieren)
- isPendingSync / isSynced Flags auf jedem Entity
- Sync nur fuer beendete Sessions (isFinished=true)

## Was wir aendern (vereinfachen)
| MAUI | Nativ | Begruendung |
|------|-------|-------------|
| Timer-basierter Periodic Sync (30s) | In-App Task + Background Platform APIs | Platform-native Loesung statt eigener Timer |
| Events (SyncCompleted, SyncError) | StateFlow / @Observable | Native State-Propagation |
| IConflictResolver Interface | Direkt im SyncEngine | Nur eine Strategie (Server-wins) |
| Separate SyncService + ConflictResolver + NetworkMonitor | Eine SyncEngine Klasse | Weniger Indirektionen |
| Entity Framework DbContext | SwiftData ModelContext / Room DAO | Native Persistenz |

## Sync-Trigger (vereinfacht)
Statt eines 30s-Timers (der auch ohne Aenderungen feuert):
1. **Sofort**: App-Start, Netzwerk-Wiederherstellung, Session Finish
2. **Manuell**: Pull-to-Refresh
3. **Periodisch**: iOS BGAppRefreshTask (15-30min, systemgesteuert) / Android WorkManager (15min)
4. **In-App**: 30s Timer NUR wenn pending Changes existieren (wie MAUI, aber mit Check)

## Konsequenzen
- Bewaehrter Algorithmus reduziert Sync-Bugs
- Vereinfachte Orchestrierung ist leichter zu debuggen
- Server-wins bedeutet: Konflikte sind selten (Single-User-App), aber wenn, gewinnt das Backend

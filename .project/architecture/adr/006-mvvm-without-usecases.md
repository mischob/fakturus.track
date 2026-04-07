# ADR-006: MVVM ohne UseCase-Klassen

## Status
Akzeptiert

## Kontext
Der PO-Plan fuer Android sieht UseCase-Klassen vor (`StartSessionUseCase`, `StopSessionUseCase`, etc.). Wir muessen entscheiden ob dieser zusaetzliche Layer sinnvoll ist.

## Entscheidung
Wir verwenden **MVVM ohne separate UseCase-Klassen**. Die Logik lebt direkt in den ViewModels.

## Begruendung
1. **Einzeiler-UseCases**: `StartSessionUseCase` wuerde nur `dao.insert(WorkSession(...))` aufrufen. Das ist eine Methode, keine Klasse.
2. **Kein Sharing noetig**: Kein UseCase wird von mehreren ViewModels geteilt. Jeder Screen hat seinen eigenen ViewModel.
3. **Datei-Explosion**: 4 UseCases (Start/Stop/Finish/Toggle) = 4 Dateien mit je ~20 Zeilen. Die gleiche Logik passt in 4 Methoden im ViewModel.
4. **AI-Navigation**: Ein AI-Agent findet `viewModel.startSession()` in einer Datei. Ohne UseCase muss er nicht zu `StartSessionUseCase.kt` springen und dann zurueck.

## Alternative: Wann UseCases sinnvoll waeren
- Wenn Business-Logik komplex ist (Validierung, Berechnung, Multi-Entity-Operationen)
- Wenn ein UseCase von 3+ ViewModels geteilt wird
- Wenn die App 10+ Screens hat und ViewModels zu gross werden

Nichts davon trifft auf Fakturus Track V1 zu.

## Konsequenzen
- ViewModels enthalten mehr Logik (~50-100 Zeilen statt ~30)
- Bei Wachstum koennten ViewModels zu gross werden (dann refactoren)
- Einfacher zu lesen und zu navigieren fuer die aktuelle App-Groesse

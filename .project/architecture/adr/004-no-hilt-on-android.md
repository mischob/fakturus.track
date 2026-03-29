# ADR-004: Manuelle DI statt Hilt auf Android

## Status
Akzeptiert

## Kontext
Der PO-Plan fuer Android sieht Hilt (Dagger) als DI-Framework vor. Das ist der Android-Industriestandard, aber wir muessen pruefen ob es fuer unsere App-Groesse sinnvoll ist.

## Entscheidung
Wir verwenden **manuelle Konstruktor-Injection** ueber einen `ServiceContainer` statt Hilt.

## Begruendung
1. **App-Groesse**: 1 Activity, 4 Screens, 4 ViewModels, ~5 Services. Das ist ein Dependency-Graph den man in 10 Zeilen manuell aufbauen kann.
2. **AI-Verstaendlichkeit**: Hilt generiert Code (`_Hilt_MainActivity`, `DaggerFakturusTrackApp_HiltComponents`), verteilt Konfiguration ueber Annotationen (`@Inject`, `@Module`, `@Provides`, `@HiltViewModel`, `@AndroidEntryPoint`) und macht den tatsaechlichen Konstruktions-Flow unsichtbar. Ein AI-Agent muss den generierten Code und die Annotation-Processing-Regeln "kennen" um den Dependency-Graph zu verstehen.
3. **Explizitheit**: Bei manueller DI sieht man in einer Datei (`ServiceContainer.kt`) exakt welches Objekt mit welchen Dependencies erstellt wird. Kein implizites Verhalten.
4. **Build-Zeit**: Hilt/Dagger erhoehen die Build-Zeit durch Annotation Processing. Bei unserer kleinen App spuerbar.
5. **Konsistenz**: iOS nutzt auch manuelle Injection (Swift hat kein DI-Framework-Aequivalent). Gleicher Ansatz auf beiden Plattformen.

## Konsequenzen
- ViewModelFactory muss manuell geschrieben werden (ca. 10 Zeilen pro ViewModel)
- Bei Wachstum (>10 ViewModels, >15 Services) sollte auf Hilt migriert werden
- Kein `@HiltViewModel` -- ViewModels werden ueber ViewModelProvider.Factory erstellt

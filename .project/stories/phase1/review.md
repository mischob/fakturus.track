# Devils Advocate Review -- Phase 1 Detailplanung

**Datum**: 2026-03-29
**Reviewer**: Devils Advocate Agent
**Scope**: Alle Phase-1-Dateien (10 EPICs, 48 Stories, 10 Tech-Specs, Execution-Waves, Parallel-Guide)

---

## Gesamturteil: [WARNING] Empfohlen mit Aenderungen

Die Planung ist beeindruckend detailliert und durchdacht. Die Architektur-Entscheidungen sind fuer den Scope angemessen (kein Over-Engineering). Allerdings gibt es **zwei kritische Probleme** (PauseMinutes nicht im Backend, SickDays-Inkonsistenz), **mehrere mittelschwere Luecken** (fehlende Ordnerstruktur-Konsistenz, iOS Concurrency-Risiken, Delete-Sync-Gap) und **einige Hinweise** die die Umsetzung erleichtern wuerden.

---

## Kritische Befunde

### [ROT] 1. PauseMinutes existiert nicht im aktuellen Backend/MAUI

**Problem**: Die gesamte Pausenerfassung (EPIC 08, 7 Stories) baut darauf auf, dass das Backend `PauseMinutes` als Feld in WorkSessions kennt und synchronisiert. Reality-Check des MAUI-Codes zeigt:

- `WorkSessionEntity.cs` hat **kein** PauseMinutes-Feld
- `WorkSessionModel.cs` hat **kein** PauseMinutes-Feld
- `CreateWorkSessionRequest` hat **kein** PauseMinutes-Feld
- `SyncWorkSessionsRequest` basiert auf `CreateWorkSessionRequest` -- ebenfalls kein PauseMinutes
- Der SyncService synct kein PauseMinutes

Die `backend-integration.md` listet `PauseMinutes` in der API-Response, aber das spiegelt einen **geplanten** Zustand wider, nicht den aktuellen. Die Memory notiert bereits: "Backend docs NOT yet updated for PauseMinutes field".

**Risiko**: Wenn die native App PauseMinutes zum Backend synct, wird das Feld entweder ignoriert (silent data loss) oder das Backend wirft einen Fehler. EPIC 08 ist blockiert bis das Backend PauseMinutes unterstuetzt.

**Alternative**:
1. **Sofort klaeren**: Ist PauseMinutes bereits im Backend-Schema? Gibt es ein Backend-Deployment das noch aussteht?
2. **Plan B**: Wenn nicht im Backend: Backend-Aenderung als explizite Voraussetzung in EPIC 08 dokumentieren. Die Planung behauptet "Backend benoetigt keine Aenderungen" (backend-integration.md Zeile 6) -- das stimmt moeglicherweise nicht.
3. **Minimal**: PauseMinutes rein lokal speichern und erst in Phase 2 synchen. Das funktioniert fuer Offline-First, aber der Nutzer verliert Pausendaten bei Geraetewechsel.

---

### [ROT] 2. SickDays: Widerspruch zwischen Architektur-Docs und Phase-1-Stories

**Problem**: Die Architektur-Docs (`shared-concepts.md`, `data-layer.md`) definieren `SickDay`/`SickDayEntity` komplett mit Sync-Algorithmus, DTOs und DAOs. Die SyncEngine-Codeskizzen in `shared-concepts.md` rufen `syncSickDays()` auf. ABER:

- Kein einziges EPIC in Phase 1 erwaehnt SickDays
- Die Phase-1 Tech-Specs (tech-epic-03, tech-epic-07) listen SickDays NICHT in den zu erstellenden Dateien
- `PersistenceManager.swift` in tech-epic-03 registriert nur WorkSession, VacationDay, UserSettings -- kein SickDay
- Die Room Database in tech-epic-03 registriert ebenfalls kein SickDayEntity
- Der bestehende MAUI-Code hat keine SickDays

**Risiko**:
- Agent liest `shared-concepts.md` und implementiert SickDay-Sync -> unnoetige Arbeit, Scope Creep
- Agent liest Phase-1-Stories und laesst SickDays weg -> Inkonsistenz mit Architektur-Docs, Verwirrung
- Die `data-layer.md` definiert ein `SickDayDao` das nirgendwo in Phase 1 erstellt wird

**Alternative**:
1. Entscheidung treffen: Sind SickDays Phase 1 oder Phase 2?
2. Wenn Phase 2: SickDays aus `shared-concepts.md` und `data-layer.md` explizit als "Phase 2" markieren oder in ein separates Dokument auslagern
3. SyncEngine-Codeskizzen in `shared-concepts.md` an Phase-1-Scope anpassen (kein `syncSickDays()` Aufruf)

---

## Mittelschwere Befunde

### [GELB] 3. VacationDay-Sync: MAUI-Code zeigt subtilen Unterschied zum Plan

**Problem**: Die Phase-1-Planung sagt korrekt "ALLE lokalen VacationDays senden". Aber der MAUI-SyncService hat eine Nuance: Er sendet alle lokalen Tage nur **wenn pending vorhanden sind** (SyncService.cs Zeile 442-505). Wenn keine pending vorhanden sind, ruft er `GetVacationDaysAsync()` auf (nur GET, kein POST). Die Phase-1-Specs (tech-epic-07) decken diesen Pfad nicht ab -- der Sync sendet IMMER einen POST.

Die native Version vereinfacht dies und sendet bei jedem Sync einen POST mit allen lokalen Tagen. Das ist funktional korrekt, erzeugt aber mehr Netzwerk-Traffic als noetig.

**Risiko**: Geringfuegig. Funktional kein Problem, aber bei vielen VacationDays (z.B. 50+ ueber mehrere Jahre) ist der unnoetige POST bei jedem 30s-Sync-Zyklus unelegant.

**Alternative**: Den MAUI-Ansatz uebernehmen -- nur POST wenn pending vorhanden, sonst GET. Das ist in der Codeskizze bereits fuer WorkSessions so implementiert, aber fuer VacationDays nicht.

---

### [GELB] 4. Ordnerstruktur-Inkonsistenz: Stories vs. Tech-Blueprint

**Problem**: EPIC 01 Story S01 definiert die iOS-Ordnerstruktur als:
```
FakturusTrack/
  App/
  Models/
  Services/
  ViewModels/    <-- existiert im EPIC
  Views/          <-- existiert im EPIC
  Extensions/
  Resources/
```

Der Tech-Blueprint (`tech-blueprint.md`) und die Implementation-Checkliste definieren:
```
FakturusTrack/
  App/
  Models/
  Services/
  Features/      <-- statt ViewModels + Views
  Shared/        <-- zusaetzlich
  Extensions/
  Resources/
```

ViewModels sind im Blueprint unter `Features/TimeTracking/`, nicht unter einem eigenen `ViewModels/` Ordner. `Views/` existiert im Blueprint gar nicht -- stattdessen `Features/` und `Shared/`.

**Risiko**: Agent liest E01-S01 und erstellt `ViewModels/` und `Views/` Ordner. Spaetere Stories referenzieren `Features/TimeTracking/` und `Shared/`. Merge-Chaos.

**Alternative**: E01-S01 Akzeptanzkriterium fuer Ordnerstruktur an den Tech-Blueprint anpassen:
```
App/, Models/, Services/, Features/, Shared/, Extensions/, Resources/
```

---

### [GELB] 5. iOS SyncEngine actor + SwiftData ModelContext: Hohes Risiko

**Problem**: Die Tech-Spec definiert SyncEngine als Swift `actor`. SwiftData `ModelContext` ist **nicht thread-safe** und muss auf dem gleichen Actor/Thread genutzt werden, auf dem er erstellt wurde. Die Codeskizze zeigt:

```swift
actor SyncEngine {
    private var modelContext: ModelContext?
    func setModelContext(_ context: ModelContext) { ... }
}
```

Ein ModelContext der auf dem Main-Thread erstellt und dann in einen separaten Actor injiziert wird, ist ein **Concurrency-Verstoss**. SwiftData in iOS 17 ist hier notorisch fehleranfaellig.

Die Risiko-Tabelle in tech-epic-07 erwaehnt dies ("Hohes Risiko"), aber die Codeskizze zeigt trotzdem den problematischen Ansatz.

**Risiko**: Crashes, Data Races, unvorhersagbares Verhalten -- besonders bei Background-Sync wo die App nicht aktiv beobachtet wird.

**Alternative**: `@ModelActor` Macro verwenden (verfuegbar ab iOS 17). Damit erstellt der Actor seinen eigenen ModelContext:

```swift
@ModelActor
actor SyncEngine {
    // modelContext wird automatisch im Actor-Kontext erstellt
}
```

Die Codeskizze sollte dies zeigen, nicht den manuellen `setModelContext()` Ansatz.

---

### [GELB] 6. Delete-Sync hat eine Race Condition

**Problem**: E07-S05 definiert Delete-Handling als:
- Online: Sofort DELETE API-Call + lokal loeschen
- Offline: Lokal loeschen, beim naechsten Sync "wird sie nicht mehr im Upload sein"

Der zweite Fall hat eine Luecke: Wenn der Nutzer offline eine Session loescht und danach online geht, passiert:
1. Sync startet
2. Keine pending Sessions (die geloeschte war ja nicht pending oder ist weg)
3. GET /work-sessions -> Server liefert die Session zurueck (sie existiert noch auf dem Server!)
4. Upsert: Session wird lokal wieder eingefuegt

Die Session ist untot -- sie kommt bei jedem Sync zurueck.

**Risiko**: Nutzer loescht eine Session offline, sie taucht nach dem naechsten Sync wieder auf. Sehr frustrierend.

**Alternative**:
1. "Soft Delete" Ansatz: Geloeschte Sessions mit einem `isDeleted` Flag markieren statt physisch zu loeschen. Beim Sync: Geloeschte mit Flag online per DELETE API loeschen, dann physisch entfernen.
2. Oder: Geloeschte IDs in einer separaten lokalen Tabelle/Liste speichern ("pending deletes"). Beim naechsten Sync: DELETE API-Calls fuer diese IDs, dann aus der Liste entfernen.
3. Mindestens: Das Problem in den Stories dokumentieren und eine bewusste Entscheidung treffen.

---

### [GELB] 7. 401-Retry im iOS APIClient: Token wird nicht wirklich refreshed

**Problem**: Die Codeskizze fuer den iOS APIClient (tech-epic-04) zeigt:

```swift
if httpResponse.statusCode == 401 {
    var retryRequest = request
    let newToken = try await authManager.acquireTokenSilently()
    retryRequest.setValue("Bearer \(newToken)", forHTTPHeaderField: "Authorization")
    return try await session.data(for: retryRequest)
}
```

`acquireTokenSilently()` gibt moeglicherweise den gleichen (abgelaufenen) Token aus dem Cache zurueck. Es fehlt ein `forceRefresh: true` Parameter. Ohne Force-Refresh ist der Retry sinnlos.

Das Story-Dokument E04-S01 erwaehnt korrekt "bei 401: Token refreshen via `acquireTokenSilently(forceRefresh: true)`", aber die Codeskizze implementiert das nicht.

**Risiko**: Endlosschleife oder permanentes 401 bei Token-Ablauf.

**Alternative**: Codeskizze anpassen:
```swift
let newToken = try await authManager.acquireTokenSilently(forceRefresh: true)
```
Und `AuthManager.acquireTokenSilently()` einen optionalen `forceRefresh` Parameter geben.

---

### [GELB] 8. Android AuthManager: Callback-basierte MSAL API wird falsch als suspend modelliert

**Problem**: Die Codeskizze fuer Android `AuthManager.kt` (tech-epic-02) zeigt `acquireTokenInteractively` als `suspend` Funktion, aber der Code darin nutzt Callbacks (`withCallback(object : AuthenticationCallback ...)`). Die Callbacks werfen Exceptions (`throw AuthException.Cancelled`), aber ein `throw` in einem Callback wird von der aufrufenden Coroutine nicht gefangen.

```kotlin
.withCallback(object : AuthenticationCallback {
    override fun onCancel() {
        throw AuthException.Cancelled  // <-- wird NICHT von der Coroutine gefangen!
    }
})
```

**Risiko**: Unbehandelte Exceptions, App-Crash bei Login-Abbruch.

**Alternative**: `suspendCancellableCoroutine` verwenden (die Implementation-Checkliste erwaehnt dies sogar unter "Haeufige Fehler"):

```kotlin
suspend fun acquireTokenInteractively(activity: Activity, provider: LoginProvider): String =
    suspendCancellableCoroutine { continuation ->
        val params = AcquireTokenParameters.Builder()
            // ...
            .withCallback(object : AuthenticationCallback {
                override fun onSuccess(result: IAuthenticationResult) {
                    continuation.resume(result.accessToken)
                }
                override fun onCancel() {
                    continuation.resumeWithException(AuthException.Cancelled)
                }
                override fun onError(exception: MsalException) {
                    continuation.resumeWithException(AuthException.Failed(exception.message ?: ""))
                }
            })
            .build()
        msalApp!!.acquireToken(params)
    }
```

---

## Hinweise

### [GRUEN] 9. Execution-Wave Timing: Optimistisch aber mit Puffer

Die 7-Wochen-kritischer-Pfad-Berechnung mit 3.5 Wochen Puffer ist realistisch. Die Story-Counts (48 Stories in 10.5 Wochen) sind fuer 2 parallele Agents machbar. Positiv: Die Wellen sind logisch aufgebaut und die Parallelisierungs-Matrix ist korrekt.

Einzige Sorge: Welle 4 (API+Sync) und Welle 5 (Pausen) haben die meiste technische Unsicherheit. Wenn der Sync 3 statt 2.5 Wochen braucht UND PauseMinutes ein Backend-Update benoetigt, schrumpft der Puffer auf unter 1 Woche.

---

### [GRUEN] 10. Manuelle Nacherfassung fehlt als explizite Story

Der Scope erwaehnt "manuelle Nacherfassung" (README.md: "Session-Verwaltung: Bearbeiten, Loeschen, manuelle Nacherfassung"). Aber keine Story definiert explizit das Erstellen einer neuen Session fuer ein vergangenes Datum. E06-S05/S06 (SessionDetailSheet) erlaubt das Bearbeiten existierender Sessions. Aber: Wie erstellt ein Nutzer manuell eine Session fuer gestern?

Der "Starten"-Button in ActiveSessionCard erstellt immer eine Session mit `startTime = Date()` (also jetzt). Eine manuelle Erstellung fuer ein vergangenes Datum ist nirgends spezifiziert.

**Moegliche Loesung**: Ein "+"-Button im History-Bereich oder ein "Manuell erfassen"-Button in der ActiveSessionCard (wenn idle). Dies koennte in E06 als zusaetzliche Story aufgenommen werden, oder es wird bewusst als "nicht in Phase 1" deklariert.

---

### [GRUEN] 11. Lokales Pause-Tracking: App-Kill verliert laufende Pause

E08-S01 dokumentiert korrekt: "`currentPauseStart` ist in-memory, geht bei App-Kill verloren". Der "Alternative" Abschnitt erwaehnt DB-Persistierung als "nice-to-have".

Bewertung: Fuer Phase 1 akzeptabel, aber der Nutzer sollte informiert werden. Wenn ein Nutzer die Pause startet, die App gekillt wird (iOS Background-Kill ist haeufig), und die App spaeter geoeffnet wird, ist die Pause "verschwunden" und die Arbeitszeit laeuft weiter.

**Empfehlung**: Mindestens `currentPauseStart` in UserDefaults/SharedPreferences speichern (5 Zeilen Code). Kein DB-Schema-Change noetig.

---

### [GRUEN] 12. WorkHoursPerWeek: Int vs Double Inkonsistenz

`backend-integration.md` zeigt `WorkHoursPerWeek: 40.00` (Double). `UserSettingsDTO` in tech-epic-03 definiert `workHoursPerWeek: Double`. `UserSettingsEntity` in data-layer.md definiert `workHoursPerWeek: Double`. Konsistent -- gut.

ABER: `VacationDaysPerYear` ist `Int` im Backend und in den DTOs. Die `backend-integration.md` zeigt `30` (ohne Dezimalpunkt). Kein Problem, nur zur Bestaetigung.

---

### [GRUEN] 13. SchoolHolidayPeriod ist in data-layer.md aber nicht in Phase 1

`data-layer.md` definiert ein `SchoolHolidayPeriod` SwiftData-Model. Kein EPIC in Phase 1 verwendet es. Das `PersistenceManager.swift` in tech-epic-03 listet es nicht. Kein Problem, aber konsistent mit SickDays sollte es als "Phase 2+" markiert werden.

---

### [GRUEN] 14. Debug-API-URL: localhost vs 10.0.2.2

iOS Configuration: `https://localhost:7001`
Android Configuration: `https://10.0.2.2:7001`

Korrekt fuer Simulator/Emulator. Aber: `https` auf localhost/10.0.2.2 erfordert SSL-Zertifikat-Handling. Die Tech-Spec erwaehnt "trust-all fuer Debug" als Fallback fuer Ktor, aber nicht fuer iOS URLSession. Fuer iOS muesste `NSAppTransportSecurity` mit `NSExceptionDomains` fuer localhost konfiguriert werden.

**Empfehlung**: In E01-S01 als Akzeptanzkriterium ergaenzen: "Debug-Konfiguration erlaubt localhost HTTPS ohne Zertifikatsvalidierung".

---

## Positives

1. **Angemessene Architektur**: Kein Over-Engineering. Kein Hilt, keine UseCase-Layer, kein Repository-Pattern fuer 4 Screens und 8 Endpoints. Das ist die richtige Entscheidung.

2. **Sync-Algorithmus gut dokumentiert**: Der 7-Schritt-Algorithmus fuer WorkSessions ist klar beschrieben und stimmt mit dem MAUI-SyncService ueberein (verifiziert gegen den tatsaechlichen Code).

3. **Parallelisierungs-Guide ist exzellent**: Die Merge-Reihenfolge, Konflikt-Dateien-Matrix und Mock-Strategien sind praxistauglich. Besonders gut: "Eine Datei = Ein Verantwortlicher" Regel.

4. **Contracts-First Ansatz**: Die Definition von Datenmodell-, DTO-, API- und ViewModel-Contracts vor der Implementierung ist genau richtig fuer parallele AI-Agent-Entwicklung.

5. **VacationDay-Sync korrekt modelliert**: Der Unterschied zu WorkSessions (alle senden vs. nur pending) ist korrekt dokumentiert und mehrfach hervorgehoben.

6. **Implementation-Checkliste**: Das "Haeufige Fehler" Kapitel ist Gold wert. Nested LazyColumn, MSAL Callback vs Coroutine, ISO8601 Fallback -- das sind genau die Fallen in die AI-Agents tappen.

7. **Test-Strategie pragmatisch**: Pflicht-Tests nur fuer ViewModel-Logik und Sync-Algorithmus. Kein 100%-Coverage-Zwang. Das ist realistisch.

8. **ArbZG-Hinweise korrekt als informativ markiert**: "Informativ, nicht einschraenkend" -- der Timer laeuft weiter. Das ist rechtlich und UX-technisch korrekt.

---

## Empfehlung: Konkrete naechste Schritte

### Vor Start der Implementierung (blockierend):

1. **PauseMinutes im Backend klaeren**: Existiert das Feld bereits in der Backend-Datenbank und API? Wenn nein: Backend-Aenderung planen und als Voraussetzung fuer EPIC 08 dokumentieren. Die Aussage "Backend benoetigt keine Aenderungen" muss korrigiert werden.

2. **SickDays-Scope entscheiden**: Phase 1 oder Phase 2? Architektur-Docs (`shared-concepts.md`, `data-layer.md`) entsprechend bereinigen.

3. **E01-S01 Ordnerstruktur korrigieren**: `ViewModels/` und `Views/` durch `Features/` und `Shared/` ersetzen (konsistent mit Tech-Blueprint).

### Vor Start von EPIC 07 (Sync):

4. **Delete-Sync-Strategie definieren**: Offline-Delete muss gehandelt werden. Soft-Delete oder Pending-Deletes-Tabelle. Die aktuelle Loesung ("wird nicht mehr im Upload sein") fuehrt zu Zombie-Sessions.

5. **iOS SyncEngine Codeskizze ueberarbeiten**: `@ModelActor` statt manueller ModelContext-Injection.

### Vor Start von EPIC 04 (API):

6. **401-Retry mit forceRefresh**: Codeskizzen in tech-epic-04 und tech-epic-02 korrigieren (forceRefresh Parameter, suspendCancellableCoroutine).

### Nice-to-have:

7. **Manuelle Nacherfassung**: Entscheiden ob Phase 1 oder Phase 2, und im Scope dokumentieren.
8. **Pause-Persistierung**: `currentPauseStart` in UserDefaults/SharedPreferences speichern.
9. **Debug SSL**: Localhost-HTTPS-Handling in E01-S01 aufnehmen.

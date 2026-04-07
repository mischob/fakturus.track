# Devils Advocate Review

**Gesamturteil**: Empfohlen mit Aenderungen -- Solide Planung mit einigen kritischen Luecken zwischen Dokumentation und Realitaet, sowie einem unterschaetzten TCO-Risiko.

Die drei Agents (PO, Design, Architektur) haben grundsaetzlich gute Arbeit geleistet. Die Dokumente sind umfangreich, konsistent untereinander und zeigen ein klares Bild. Die Architektur-Entscheidung fuer Einfachheit ueber Abstraktion ist lobenswert. Dennoch gibt es mehrere Befunde, die vor Beginn der Implementierung adressiert werden muessen.

---

## Befunde

### KRITISCH: Falscher API-Endpunkt fuer Overtime-Summary dokumentiert

- **Problem**: Die Backend-Integration (`backend-integration.md`) dokumentiert den Overtime-Endpunkt als `GET /v1/settings/overtime?year=2026`. Der tatsaechliche Endpunkt im Code (`GetOvertimeSummaryEndpoint.cs`, Zeile 19) ist `GET /v1/overtime-summary?year=2026`. Der gleiche Fehler zieht sich durch die Architektur-Dokumente -- in `ios-architecture.md` wird `apiClient.getOvertimeSummary()` mit dem Pfad `/v1/settings/overtime` aufgerufen.
- **Risiko**: Jeder AI-Agent, der auf Basis dieser Dokumentation implementiert, wird den falschen Endpunkt aufrufen. Das ist ein 404-Bug, der erst zur Laufzeit auffaellt.
- **Aktion**: Alle Dokumente korrigieren auf `/v1/overtime-summary`. Oder den Backend-Endpunkt anpassen -- aber konsistent sein.

### KRITISCH: SyncVacationDaysResponse hat ein undokumentiertes Feld

- **Problem**: Die Backend-DTO (`VacationDayDto.cs`, Zeile 17) definiert `SyncVacationDaysResponse` mit zwei Feldern: `ServerVacationDays` UND `DeletedIds`. Die Dokumentation in `backend-integration.md` zeigt nur `ServerVacationDays`. Der Architektur-Code (sowohl Swift als auch Kotlin) verarbeitet `DeletedIds` nicht.
- **Risiko**: Geloeschte Urlaubstage werden moeglicherweise nicht korrekt lokal entfernt. Der aktuelle MAUI-Sync-Code ignoriert `DeletedIds` ebenfalls und nutzt stattdessen einen Set-Differenz-Ansatz (alle Server-IDs vergleichen). Das funktioniert zwar, aber die Dokumentation sollte die tatsaechliche Response-Struktur widerspiegeln.
- **Aktion**: `DeletedIds` in der Backend-Integration dokumentieren. Entscheiden ob es in der nativen Sync-Engine genutzt wird (effizienter als Set-Differenz) oder ob der MAUI-Ansatz beibehalten wird.

### KRITISCH: WorkSession-DTO fehlt CalendarEventId

- **Problem**: Die Backend-DTO (`WorkSessionDto.cs`) hat KEIN `CalendarEventId`-Feld. Die Dokumentation in `backend-integration.md` listet es aber als Response-Feld auf (`"CalendarEventId": "string|null"`). Im MAUI-SyncService (Zeile 399) wird `CalendarEventId` explizit als "not synced from backend" kommentiert.
- **Risiko**: Ein Implementierer koennte erwarten, dass `CalendarEventId` vom Backend kommt und entsprechenden Parsing-Code schreiben, der dann fehlschlaegt oder `null` liefert.
- **Aktion**: `CalendarEventId` aus der API-Response-Dokumentation entfernen. In den nativen DTOs gar nicht erst als servertisches Feld modellieren.

### BEDENKEN: MAUI-App hat keinen MSAL-Login mit Social Providers -- aber Plaene setzen ihn voraus

- **Problem**: Die MAUI-App (`OfflineAuthService.cs`) nutzt `AcquireTokenInteractive` mit `Prompt.SelectAccount`, aber OHNE Domain Hints. Es gibt keine Social Login Buttons (Apple, Google). Die Policy ist `B2C_1_BetaSignInOnly` -- eine Sign-In-Only Policy ohne Self-Registration. Die Plaene gehen davon aus, dass Social Login Buttons (Apple, Google, E-Mail) wie in fakturus.poi implementiert werden.
- **Risiko**: Kein direktes Risiko, aber die PO-Dokumentation suggeriert, dass Social Login ein bestehendes Feature ist (`[IST]`). In Wahrheit ist es ein neues Feature, das in der B2C-Policy konfiguriert werden muss. Die aktuelle Policy `B2C_1_BetaSignInOnly` unterstuetzt moeglicherweise keine Social Providers. Das Auth-Konzept erwaehnt korrekt, dass eine neue Policy (`B2C_1_TrackSignUpSignIn`) fuer Produktion erstellt werden soll, aber Phase 1 soll die bestehende Policy nutzen.
- **Aktion**: Klaeren ob `B2C_1_BetaSignInOnly` Social Providers konfiguriert hat. Falls nicht: Social Login Buttons in Phase 1 weglassen und den B2C-Standard-Login-Screen nutzen. Feature-Liste (`features.md`) korrigieren -- Social Login ist `[NEU]`, nicht `[IST][VERBESSERT]`.

### BEDENKEN: MAUI Redirect-URI weicht vom geplanten nativen Format ab

- **Problem**: Die MAUI-App nutzt `msal{clientId}://auth` als Redirect-URI (Zeile 393 in `OfflineAuthService.cs`). Das Auth-Konzept plant fuer iOS `msauth.com.fakturus.track://auth` und fuer Android `msauth://com.fakturus.track/{hash}`. Das ist grundsaetzlich korrekt (MSAL-Standard-Format pro Plattform), aber die Azure Portal App-Registration muss fuer JEDE Plattform die korrekte Redirect-URI konfiguriert haben.
- **Risiko**: Wenn die Azure Portal-Konfiguration vergessen wird, funktioniert der Login auf einer oder beiden nativen Plattformen nicht. Das ist ein haart-to-debug Fehler.
- **Aktion**: Explizite Azure Portal Konfigurationsschritte in das Auth-Konzept aufnehmen. Eine Checkliste mit den exakten Redirect-URIs die im Portal hinzugefuegt werden muessen, inklusive der bestehenden MAUI-URI die beibehalten werden muss.

### BEDENKEN: Vacation-Sync sendet ALLE lokalen Tage, Architektur-Code nur pending

- **Problem**: Der MAUI-SyncService (`SyncService.cs`, Zeilen 463-487) sendet beim Vacation-Sync ALLE lokalen Urlaubstage (synced + pending), nicht nur die pending. Die Architektur-Dokumentation (`shared-concepts.md`, Sync-Algorithmus) beschreibt aber, dass nur pending gesendet werden. Der Swift- und Kotlin-Code in der Architektur sendet ebenfalls nur pending.
- **Risiko**: Wenn die nativen Apps nur pending Vacation Days senden, aber das Backend erwartet die komplette Liste (um Loeschungen zu erkennen), dann werden lokal geloeschte Urlaubstage nie im Backend geloescht. Der MAUI-Ansatz (alle senden) ist korrekt -- das Backend vergleicht die gesendete Liste mit seiner eigenen und leitet daraus Loeschungen ab.
- **Aktion**: Die Sync-Strategie fuer VacationDays muss korrigiert werden: ALLE lokalen Tage senden, nicht nur pending. Dies ist ein subtiler aber kritischer Unterschied zum WorkSession-Sync, wo tatsaechlich nur pending gesendet werden. Den Sync-Algorithmus in `shared-concepts.md` differenzieren zwischen WorkSessions (nur pending senden) und VacationDays (alle senden).

### BEDENKEN: SwiftData-Risiko fuer Sync-Operationen wird unterschaetzt

- **Problem**: ADR-003 waehlt SwiftData und listet als Fallback GRDB. Die Sync-Engine macht aber Operationen, die mit SwiftData schwierig sind: Batch-Lookups nach ID (`fetch` mit Predicate fuer spezifische UUIDs in einer Schleife), Batch-Deletes, und transaktionale Updates ueber mehrere Entities. SwiftData's `#Predicate` Macro ist eingeschraenkt (keine `contains`-Operation auf Sets, keine IN-Queries).
- **Risiko**: Die Sync-Engine-Implementation koennte deutlich komplexer werden als die ~40 Zeilen in `shared-concepts.md` suggerieren. Performance bei >500 Sessions (Schleife mit einzelnen Fetches) koennte problematisch sein.
- **Aktion**: Im ADR-003 einen konkreten Proof-of-Concept-Test definieren: "In Woche 3 implementieren wir syncWorkSessions() mit SwiftData. Falls die Performance bei 500+ Sessions oder die API-Limitierungen des Predicate-Macros blockierend sind, switchen wir zu GRDB." Nicht erst nach Phase 1 evaluieren -- das ist zu spaet.

### BEDENKEN: PO-Plan und Architektur widersprechen sich bei Android DI

- **Problem**: Der PO-Plan (`android-plan.md`, Zeile 14) listet `Hilt (Dagger)` als DI-Technologie. Die Architektur (ADR-004) entscheidet sich explizit GEGEN Hilt. Die Ordnerstruktur im PO-Plan (`di/AppModule.kt`, `di/DatabaseModule.kt`, etc.) existiert in der Architektur nicht.
- **Risiko**: Kein technisches Risiko, aber ein Konsistenz-Problem. Wenn ein Entwickler den PO-Plan liest, erwartet er Hilt. Wenn er die Architektur liest, sieht er manuelle DI.
- **Aktion**: PO-Plan aktualisieren um die Architektur-Entscheidung zu reflektieren. `android-plan.md` ist das Hauptdokument das zuerst gelesen wird -- es sollte nicht veraltete Technologie-Entscheidungen enthalten.

### BEDENKEN: Ktor Client mit JsonNamingStrategy Problem

- **Problem**: Der Android-APIClient-Code (`android-architecture.md`, Zeile 232) nutzt `JsonNamingStrategy` fuer PascalCase-Konvertierung. Allerdings ist `JsonNamingStrategy` in kotlinx.serialization nicht stabil (experimentell) und funktioniert NICHT mit `@SerialName`-Annotationen zusammen. Die DTOs in `shared-concepts.md` und `data-layer.md` nutzen aber `@SerialName` fuer explizites Mapping.
- **Risiko**: Doppeltes Mapping: `@SerialName("StartTime")` + `namingStrategy` wuerden den Key zweimal transformieren. Der Code wird entweder die NamingStrategy ODER die SerialName-Annotationen nutzen, aber nicht beide.
- **Aktion**: Sich fuer einen Ansatz entscheiden: Entweder `@SerialName` auf allen DTOs (explizit, zuverlaessig) ODER `JsonNamingStrategy` (weniger Boilerplate, aber experimentell). Empfehlung: `@SerialName` beibehalten, NamingStrategy aus dem APIClient-Code entfernen.

### HINWEIS: 22 Wochen Zeitplan fuer zwei native Apps ist ambitioniert

- **Problem**: 22 Wochen fuer zwei vollstaendige native Apps (Swift + Kotlin), inklusive Auth, Sync, 4 Screens, Widgets, Apple Watch, und Store-Launch. Die Plaene gehen davon aus, dass iOS und Android parallel entwickelt werden.
- **Risiko**: Ein einzelner Entwickler kann das in 22 Wochen nicht schaffen. Zwei Entwickler (je einer pro Plattform) sind Minimum. Die Plaene nennen keine Teamgroesse. Bei AI-gestuetzter Entwicklung ist die Timeline theoretisch machbar, aber der Overhead fuer konsistente Sync-Logik auf zwei Plattformen wird unterschaetzt.
- **Aktion**: Teamgroesse explizit machen. Falls ein einzelner Entwickler: Phase 1 auf eine Plattform fokussieren (iOS empfohlen, da fakturus.poi-Referenz staerker), danach die zweite. Das wuerde die Timeline auf ~30 Wochen verlaengern, aber das Qualitaetsrisiko senken.

### HINWEIS: Overtime-Berechnung ohne Offline-Support ist eine bewusste Luecke

- **Problem**: Die Architektur entscheidet bewusst, Overtime-Summary nicht lokal zu cachen (`data-layer.md`, "Overtime-Summary (Ausnahme)"). Der Gesamt-Tab ist damit offline leer.
- **Risiko**: Fuer eine "Offline-first" App ist ein komplett leerer Tab eine schlechte UX. Der Nutzer sieht "Uebersicht konnte nicht geladen werden" ohne Internet.
- **Aktion**: Einen einfachen Disk-Cache fuer die letzte Overtime-Response einbauen (1 Datei, JSON). Zeige den letzten bekannten Stand mit einem Hinweis "Zuletzt aktualisiert: vor X Stunden". Das ist minimal aufwaendig und verbessert die Offline-UX erheblich.

### HINWEIS: Kein User-Agent Header geplant

- **Problem**: Die Migrationsstrategie (`migration.md`) plant "Backend User-Agent Analyse" um zu pruefen, ob alle Nutzer migriert haben. Aber weder die Backend-Integration noch die Architektur erwaehnen das Setzen eines `User-Agent` Headers in API-Calls.
- **Aktion**: In den APIClient-Code (beide Plattformen) einen `User-Agent` Header einbauen, z.B. `FakturusTrack-iOS/1.0.0` bzw. `FakturusTrack-Android/1.0.0`. Das Backend kann dann zwischen MAUI- und nativen Clients unterscheiden.

### HINWEIS: Settings-Sync-Strategie unklar

- **Problem**: Die MAUI-App nutzt einen `ConflictResolver` fuer UserSettings. Die Architektur-Dokumente beschreiben Settings-Sync als "Server-wins" (gleich wie WorkSessions). Aber der MAUI-Code zeigt, dass Settings-Konflikte gesondert behandelt werden (`ResolveUserSettingsConflictAsync`). Was passiert wenn ein Nutzer offline seine Wochenstunden aendert und das Backend einen anderen Wert hat?
- **Risiko**: Bei reinem Server-wins wuerden lokale Settings-Aenderungen verworfen. Das ist fuer WorkSessions akzeptabel (Backend hat die Wahrheit), aber fuer Settings kontraintuitiv -- der Nutzer hat aktiv etwas geaendert.
- **Aktion**: Settings-Sync-Strategie explizit definieren. Empfehlung: Settings bidirektional mit "Last-Write-Wins" (basierend auf UpdatedAt-Timestamp). Oder einfacher: Settings werden sofort synchronisiert (kein Offline-Edit, nur online aenderbar). Beides ist besser als undefiniertes Verhalten.

---

## Positives

**Architektur-Einfachheit**: Die Entscheidung gegen Clean Architecture, Hilt, UseCases und separate Repositories ist fuer diese App-Groesse goldrichtig. ADR-002 ist eines der besten "Nein zu Over-Engineering"-Argumente das ich gelesen habe. Die Begruendung "4 Screens, 8 Endpunkte, 3 Entities" ist praezise und nachvollziehbar.

**Sync-Engine als bewaehrtes Muster**: Den MAUI-Sync-Algorithmus als Referenz zu nutzen statt neu zu erfinden ist klug. Der 8-Schritt-Algorithmus funktioniert in Produktion. ADR-005 begruendet das sauber.

**ServiceContainer-Pattern**: Die Lektion aus fakturus.poi (480-Zeilen App-Datei vermeiden) zeigt, dass das Team aus Fehlern lernt. Der ServiceContainer ist eine saubere, einfache Loesung.

**Design-System**: Das Design-Dokument ist nicht generisch. Die Philosophie "Werkzeug, kein Social Network" und die konsequente Nutzung von System-Fonts und plattformspezifischen Icons zeigt Verstaendnis fuer die Zielgruppe.

**Plattform-Respekt**: iOS und Android werden nicht gleich behandelt. iOS nutzt SwiftUI-Patterns (Sheets, DisclosureGroups), Android nutzt Material 3 Patterns (BottomSheet, Snackbar mit Undo). Das ist richtig und zeigt, dass die nativen Plattformen ernst genommen werden.

**Minimale Dependencies**: Nur MSAL als externe Dependency auf iOS. Ktor + Room + MSAL auf Android. Keine Alamofire, kein Realm, kein Firebase. Das reduziert TCO und Sicherheitsrisiko erheblich.

**Migrationsstrategie**: "Es gibt keine Daten-Migration" ist die beste Migrationsstrategie. Backend als Single Source of Truth macht den Wechsel trivial.

**DSGVO-Dokumentation**: Die Sicherheits- und Datenschutz-Analyse ist gruendlich. Keine Tracking-SDKs, keine Analytics, keine Werbe-IDs. Fuer den deutschen Markt genau richtig.

---

## Empfehlung

### Sofort (vor Implementierungsstart)

1. **Overtime-Endpunkt korrigieren**: `/v1/overtime-summary` in ALLEN Dokumenten (backend-integration, ios-architecture, android-architecture, shared-concepts).
2. **CalendarEventId aus API-Response-Doku entfernen**: Ist ein mobil-lokales Feld, kommt nicht vom Server.
3. **VacationDay-Sync-Logik korrigieren**: Alle lokalen Tage senden, nicht nur pending. Den Unterschied zu WorkSession-Sync explizit dokumentieren.
4. **PO-Plan und Architektur synchronisieren**: Hilt aus `android-plan.md` entfernen, Feature-Liste `features.md` um Social Login als `[NEU]` korrigieren.
5. **Ktor NamingStrategy vs SerialName klaeren**: Einen Ansatz waehlen, den anderen entfernen.

### In Phase 1 Woche 1-2 (Validierung)

6. **SwiftData-Sync-PoC**: syncWorkSessions() mit SwiftData implementieren und mit 500+ Eintraegen testen. GRDB-Fallback-Entscheidung bis Ende Woche 3.
7. **Azure Portal Redirect-URIs konfigurieren**: Checkliste abarbeiten, Login auf beiden Plattformen testen.
8. **User-Agent Header einbauen**: Von Anfang an, nicht nachtraeglich.

### Spaeter (vor Phase 2)

9. **Overtime Disk-Cache**: Letzten Stand lokal speichern fuer Offline-Ansicht.
10. **Settings-Sync-Strategie definieren**: Last-Write-Wins oder Online-Only.
11. **Teamgroesse und Timeline validieren**: Ist das Ein-Personen- oder Zwei-Personen-Projekt?

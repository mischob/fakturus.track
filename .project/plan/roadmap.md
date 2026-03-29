# Roadmap -- Fakturus Track Native Apps

## Zeitleiste

> **Hinweis:** Diese Roadmap basiert auf **AI-gestuetzter Entwicklung mit 1 Entwickler** (Mensch + AI-Agenten). iOS und Android werden parallel entwickelt. Falls Engpaesse auftreten oder die Timeline unter Druck geraet: **iOS first, dann Android.** Begruendung: fakturus.poi iOS ist die staerkere Referenz-Codebasis, und der iOS App Store Review-Prozess dauert laenger (fruehzeitige Einreichung sinnvoll).

> **Marktanalyse-Update (Maerz 2026):** Phase 1 wurde um Pausenerfassung erweitert (gesetzliche Pflicht).
> Phase 2 wurde um PDF/CSV-Export und Krankheitstage erweitert (Markt-Must-Haves vor Launch).
> Die Zeiterfassungspflicht 2026 mit Uebergangsfristen (10-50 MA: 24 Monate) erzeugt ein
> Zeitfenster bis ca. Mitte 2028 -- der App Store Launch in Q3 2026 positioniert uns frueh.

```
2026
         Apr          Mai          Jun          Jul          Aug          Sep          Okt
    ┌────────────────────────────────┐
    │     Phase 1                    │
    │  Kern-Zeiterfassung            │
    │  Auth + Sync + Pausen          │
    │  10.5 Wochen                   │
    └────────────────────────────────┘
                                      ┌────────────────────────┐
                                      │    Phase 2              │
                                      │  Gesamt/Urlaub          │
                                      │  Settings/Export        │
                                      │  Krankheitstage         │
                                      │  8.5 Wochen             │
                                      └────────────────────────┘
                                                                ┌────────────┐
                                                                │  Phase 3   │
                                                                │  Polish    │
                                                                │  Widgets   │
                                                                │  4 Wochen  │
                                                                └────────────┘
                                                                              ┌─────────────┐
                                                                              │  Phase 4    │
                                                                              │  Launch     │
                                                                              │  Migration  │
                                                                              │  Feature-   │
                                                                              │  Gating     │
                                                                              │  5 Wochen   │
                                                                              └─────────────┘
```

**Gesamtdauer: ca. 25 Wochen -- +3 Wochen gegenueber urspruenglicher Planung (vorher 22 Wochen)**
Aufschluesselung: Pausenerfassung (+1.5 Wochen in Phase 1), Export/Krankheitstage (+1.5 Wochen in Phase 2),
Feature-Gating (+1 Woche in Phase 4). Nicht +4 Wochen wie DA vorschlaegt, da AI-gestuetzte Entwicklung
den Mehraufwand teilweise kompensiert; aber auch nicht nur +2, da Pausen und Krankheitstage substantielle
Backend-Aenderungen und UX-Durchspezifizierung erfordern.

---

## Phase 1: Kern-Zeiterfassung + Pausenerfassung (April - Mitte Juni 2026)

**Ziel:** Funktionierende App mit Login, Zeiterfassung, Pausenerfassung und Sync -- nutzbar fuer geschlossene Beta-Tester. Pausenerfassung ist gesetzliche Pflicht und MUSS vor Launch vorhanden sein (Marktanalyse-Erkenntnis).

### Meilenstein 1.1: Fundament (Woche 1-2)
**Ziel-Datum: 12. April 2026**

| Task | iOS | Android | Status |
|------|-----|---------|--------|
| Projekt-Setup | Xcode Project | Android Studio Project | |
| B2C Auth Integration | MSAL iOS | MSAL Android | |
| Login Screen | SwiftUI | Jetpack Compose | |
| Token-Management | Keychain | Keystore | |
| API-Client Grundlage | URLSession | Retrofit/Ktor | |

**Definition of Done:**
- Nutzer kann sich via Azure B2C einloggen
- Access Token wird sicher gespeichert
- API-Calls mit Bearer Token funktionieren

### Meilenstein 1.2: Lokale Datenschicht (Woche 3-4)
**Ziel-Datum: 26. April 2026**

| Task | iOS | Android | Status |
|------|-----|---------|--------|
| Datenbank-Setup | SwiftData | Room | |
| WorkSession Model/Entity | Swift Model | Kotlin Entity + DAO | |
| Repository-Pattern | Service Layer | Repository | |
| Network Monitor | NWPathMonitor | ConnectivityManager | |

**Definition of Done:**
- Sessions koennen lokal erstellt und gespeichert werden
- Netzwerk-Status wird korrekt erkannt
- Datenbankschema ist definiert

### Meilenstein 1.3: Zeiterfassungs-UI (Woche 5-6)
**Ziel-Datum: 10. Mai 2026**

| Task | iOS | Android | Status |
|------|-----|---------|--------|
| Tab-Navigation (4 Tabs) | TabView | NavigationBar | |
| Zeiterfassungs-Screen | SwiftUI View | Compose Screen | |
| Active Session Card + Timer | SwiftUI Animation | Compose Animation | |
| Start/Stop/Finish Workflow | ViewModel | ViewModel | |
| History mit Monatsgruppen | List + Section | LazyColumn | |
| Session bearbeiten | Sheet | BottomSheet | |
| Session loeschen | Swipe Action | SwipeToDismiss | |

**Definition of Done:**
- Nutzer kann Sessions erstellen, starten, stoppen, beenden
- History zeigt alle Sessions gruppiert nach Monat
- Sessions koennen bearbeitet und geloescht werden

### Meilenstein 1.4: Sync-System (Woche 7-8)
**Ziel-Datum: 24. Mai 2026**

| Task | iOS | Android | Status |
|------|-----|---------|--------|
| Sync Manager | Swift Actor | Kotlin Coroutines | |
| Upload pending Sessions | API Call | API Call | |
| Download + Merge | Sync Logic | Sync Logic | |
| Conflict Resolution | Server-wins | Server-wins | |
| Automatischer Sync | Background Fetch | WorkManager | |
| Pull-to-Refresh | Native | Native | |
| Offline-Banner | SwiftUI View | Compose | |
| Sync-Status Indikator | SwiftUI View | Compose | |

**Definition of Done:**
- Sessions werden zuverlaessig mit Backend synchronisiert
- Offline-Nutzung funktioniert (Daten lokal gespeichert)
- Bei Netzwerk-Wiederherstellung wird automatisch synchronisiert
- Pull-to-Refresh loest manuellen Sync aus

### Meilenstein 1.5: Pausenerfassung (Woche 9-10.5) -- NEU aus Marktanalyse
**Ziel-Datum: 14. Juni 2026**

> **Begruendung:** Die Marktanalyse identifiziert Pausenerfassung als gesetzliche Pflicht (ArbZG).
> 7 von 11 Wettbewerbern bieten diese Funktion. Ohne Pausenerfassung ist fakturus.track nicht
> gesetzeskonform und kann nicht als "compliant" vermarktet werden.

| Task | iOS | Android | Status |
|------|-----|---------|--------|
| Backend: WorkSession um PauseMinutes erweitern | API-Aenderung | API-Aenderung | |
| Pause-Button in ActiveSessionCard | SwiftUI | Compose | |
| Manuelle Pauseneingabe (Minuten-Feld) | SwiftUI | Compose | |
| Nettoarbeitszeit-Berechnung (Brutto - Pausen) | ViewModel | ViewModel | |
| ArbZG-Pausenhinweis (6h/9h Schwellen) | Local Notification | Notification | |
| History: Pausendauer in SessionRow anzeigen | SwiftUI | Compose | |
| Sync: PauseMinutes in Sync-Logik integrieren | SyncEngine | SyncEngine | |

**Definition of Done:**
- Nutzer kann Pausen waehrend einer laufenden Session erfassen
- Pausendauer wird bei nachtraeglicher Erfassung manuell eingebbar
- Nettoarbeitszeit wird korrekt berechnet (Brutto minus Pausen)
- ArbZG-Hinweis erscheint bei 6h (30min Pause) und 9h (45min Pause)
- Pausen werden im Backend synchronisiert

**Phase 1 Abschluss-Kriterien:**
- Interne Beta-Version (TestFlight / Firebase App Distribution)
- Alle Phase-1 Features funktionieren (inkl. Pausenerfassung)
- Keine kritischen Bugs
- Performance: App-Start < 2s, kein UI-Ruckeln
- **Gesetzeskonformitaet: Start, Ende, Pausen werden erfasst (ArbZG)**

---

## Phase 2: Vollstaendige Features + Export + Krankheitstage (Mitte Juni - Mitte August 2026)

**Ziel:** Feature-Paritaet mit dem Web-Frontend. Export-Funktionen fuer Compliance-Nachweis. Krankheitstage als Abwesenheitstyp.

### Meilenstein 2.1: Gesamt-Uebersicht (Woche 11-12.5)
**Ziel-Datum: 02. Juli 2026**

| Task | iOS | Android |
|------|-----|---------|
| Overtime Dashboard | SwiftUI Cards | Material 3 Cards |
| Monatliche Tabelle | List/Grid | LazyColumn |
| Jahresnavigation | Buttons | Buttons |
| Urlaubsanzeige (Summary) | Card | Card |
| Feiertagsanzeige | Card | Card |

### Meilenstein 2.2: Urlaubsverwaltung + Krankheitstage (Woche 13-15)
**Ziel-Datum: 19. Juli 2026**

| Task | iOS | Android |
|------|-----|---------|
| Kalender-Ansicht | Custom Calendar | Custom Composable |
| Tap-to-Toggle Urlaubstage | Gesture | Gesture |
| Feiertag-Markierung | Visual | Visual |
| Vacation Sync | Sync Service | Repository + Sync |
| Resturlaub-Anzeige | Header | Header |
| **Krankheitstage im Kalender** | **SwiftUI (eigene Farbe)** | **Compose (eigene Farbe)** |
| **Krankheitstage-Sync** | **Sync Service** | **Repository + Sync** |
| **Backend: SickDay Entity** | **API-Aenderung** | **API-Aenderung** |

> **Marktanalyse-Ergaenzung:** Krankheitstage als eigener Abwesenheitstyp neben Urlaub.
> 6/11 Wettbewerber bieten das. Krankheitstage reduzieren Soll-Stunden, aber nicht Urlaubskontingent.

### Meilenstein 2.3: Einstellungen + Export (Woche 16-19)
**Ziel-Datum: 09. August 2026**

| Task | iOS | Android |
|------|-----|---------|
| Settings Screen | Form | LazyColumn Items |
| Wochenstunden Eingabe | Stepper/TextField | TextField |
| Arbeitstage-Auswahl | Toggle pro Tag | FilterChip pro Tag |
| Bundesland-Picker | Picker | DropdownMenu |
| Urlaubstage/Jahr | Stepper/TextField | TextField |
| Kalender-URL | TextField | TextField |
| Schulferien-Verwaltung | List + Sheet | List + Dialog |
| Settings Sync | Service | Repository |
| Profil-Anzeige | Header | Header |
| **PDF-Monatsreport generieren** | **PDFKit / UIGraphics** | **Android PDF API** |
| **CSV/Excel-Export** | **CSV-String + Share** | **CSV-String + Share** |
| **Share-Sheet Integration** | **UIActivityVC** | **Intent.ACTION_SEND** |

> **Marktanalyse-Ergaenzung:** PDF/Excel-Export ist Basisanforderung. 10/11 Wettbewerber bieten
> mindestens PDF-Export. Nutzer benoetigen Nachweise gegenueber Behoerden und Arbeitgebern.

**Phase 2 Abschluss-Kriterien:**
- Feature-Paritaet mit Web-Frontend
- Urlaubsverwaltung funktioniert vollstaendig
- **Krankheitstage koennen erfasst und synchronisiert werden**
- **PDF- und CSV-Export funktionieren (Monatsreport)**
- Einstellungen werden korrekt synchronisiert
- Beta-Test mit erweiterter Nutzergruppe (10-20 Personen)

---

## Phase 3: Polish & Erweiterungen (Mitte August - Mitte September 2026)

**Ziel:** Professionelle, store-reife App mit Premium-Features.

### Meilenstein 3.1: UI/UX Polish (Woche 20-21)
**Ziel-Datum: 23. August 2026**

| Task | Plattform |
|------|-----------|
| Dark Mode | iOS + Android |
| Haptic Feedback | iOS + Android |
| Animationen & Transitionen | iOS + Android |
| Loading States & Skeletons | iOS + Android |
| Error Handling (User-facing) | iOS + Android |

### Meilenstein 3.2: Widgets & Erweiterungen (Woche 22-23)
**Ziel-Datum: 06. September 2026**

| Task | Plattform |
|------|-----------|
| Home Screen Widget | iOS (WidgetKit) + Android (Glance) |
| Live Activity (laufende Session) | iOS |
| Apple Watch Companion (minimal) | iOS |
| App Shortcut | Android |

### Meilenstein 3.3: Barrierefreiheit & Qualitaet (Woche 22-23)
| Task | Plattform |
|------|-----------|
| VoiceOver Audit | iOS |
| TalkBack Audit | Android |
| Dynamic Type | iOS |
| Kontrast-Pruefung | iOS + Android |
| Performance-Profiling | iOS (Instruments) + Android (Profiler) |

**Phase 3 Abschluss-Kriterien:**
- App erfuellt Apple/Google Store-Anforderungen
- Accessibility-Audit bestanden
- Performance: Cold Start < 1s
- Keine bekannten Crashes

---

## Phase 4: Store-Launch, Feature-Gating & Migration (Mitte September - Mitte Oktober 2026)

**Ziel:** Veroeffentlichung im App Store und Google Play. Feature-Gating fuer Freemium-Modell. MAUI-App Abloesung.

> **Marktanalyse-Kontext:** Die gesetzliche Zeiterfassungspflicht tritt 2026 in Kraft.
> Unternehmen mit 10-50 MA haben 24 Monate Uebergangsfrist (bis ca. Mitte 2028).
> Ein Launch in Q3/Q4 2026 positioniert uns frueh in diesem Zeitfenster.

### Meilenstein 4.0: Feature-Gating Implementation (Woche 24) -- NEU
**Ziel-Datum: 13. September 2026**

| Task | Plattform |
|------|-----------|
| Feature-Gating Infrastruktur (FREE/STARTER/PRO/TEAM) | iOS + Android |
| In-App-Purchase / Subscription-Management | iOS (StoreKit 2) + Android (Google Play Billing) |
| Tier-Pruefung in UI (gesperrte Features markieren) | iOS + Android |
| Restore Purchases | iOS + Android |

> **Begruendung:** Feature-Gating muss vor dem Store-Launch implementiert sein, damit das
> Freemium-Modell ab Tag 1 funktioniert. In-App-Purchase / Subscription-Management
> erfordert Integration mit Apple StoreKit 2 und Google Play Billing Library.

### Meilenstein 4.1: Store-Vorbereitung (Woche 25-26)
**Ziel-Datum: 27. September 2026**

| Task | Plattform |
|------|-----------|
| App Store Screenshots | iOS |
| Play Store Screenshots | Android |
| App-Beschreibung (DE/EN) | iOS + Android |
| Privacy Policy erstellen | Web |
| App Store Review Guidelines Check | iOS |
| Google Play Policy Check | Android |
| TestFlight Open Beta | iOS |
| Open Testing Track | Android |

### Meilenstein 4.2: Launch (Woche 27)
**Ziel-Datum: 04. Oktober 2026**

| Task | Plattform |
|------|-----------|
| App Store Submission | iOS |
| Play Store Submission | Android |
| Launch-Kommunikation an Nutzer | -- |

### Meilenstein 4.3: MAUI-Abloesung (Woche 28)
**Ziel-Datum: 11. Oktober 2026**

| Task | Details |
|------|---------|
| In-App-Nachricht in MAUI-App | "Neue native App verfuegbar" |
| MAUI-App aus Stores entfernen | Nach 4 Wochen Uebergangszeit |
| MAUI-Projekt archivieren | Code-Repository beibehalten |
| Backend: MAUI-spezifische Endpunkte pruefen | Normalerweise keine noetig |

---

## Risiko-Management

| Risiko | Massnahme | Verantwortung |
|--------|-----------|---------------|
| App Store Ablehnung | Fruehzeitige Review-Guidelines Pruefung | Product Owner |
| Sync-Konflikte bei Migration | Gleiche Backend-Endpunkte, keine Breaking Changes | Backend-Dev |
| B2C Token-Kompatibilitaet | Gleiche App-Registration in Phase 1 | DevOps |
| Performance-Probleme | Kontinuierliches Profiling ab Phase 1 | iOS/Android Dev |
| Scope Creep | Feature-Freeze nach Phase 2 Definition | Product Owner |

---

## Erfolgs-Metriken

| Metrik | Ziel | Messung |
|--------|------|---------|
| App Store Rating | >= 4.5 Sterne | App Store Connect / Play Console |
| Crash-Free Rate | >= 99.5% | Crashlytics |
| Cold Start Time | < 1 Sekunde | Performance Profiling |
| Sync-Erfolgsrate | >= 99% | Backend Logs |
| Beta-Tester Zufriedenheit | >= 4/5 | Feedback-Umfrage |
| MAUI-Migration | 100% Nutzer migriert | Backend User-Agent Analyse |

### Marktanalyse-basierte Metriken (Post-Launch)

| Metrik | Ziel (Monat 6) | Ziel (Monat 12) | Quelle |
|--------|---------------|-----------------|--------|
| Free User | 600 | 1.500 | Backend User-Count |
| Paid User (Conversion 5-8%) | 45 | 120 | Zahlungsanbieter |
| MRR | 158 EUR | 420 EUR | Zahlungsanbieter |
| Break-Even (15-30 Paid User) | Erreicht | Stabil | Kosten vs. MRR |

> Projektionen aus Preisanalyse (konservatives Szenario). Optimistisch: 3x dieser Werte.

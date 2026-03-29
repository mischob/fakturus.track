# Migrationsstrategie -- MAUI zu Nativ

## Ueberblick

Die Migration von der MAUI/Blazor-Hybrid-App zu den nativen Apps ist risikoarm, da:
1. Das **Backend bleibt unveraendert** -- gleiche API-Endpunkte
2. Der **gleiche Azure B2C Tenant** genutzt wird -- gleiche Benutzerkonten
3. **Kein Datenverlust** moeglich -- alle Daten liegen im Backend (PostgreSQL)

## Migrations-Phasen

### Phase A: Parallelbetrieb (waehrend Entwicklung)

```
                   ┌──────────────┐
                   │   Backend    │
                   │   (API v1)   │
                   └──────┬───────┘
                          │
            ┌─────────────┼─────────────┐
            │             │             │
    ┌───────┴──────┐ ┌────┴─────┐ ┌────┴─────┐
    │  MAUI App    │ │ iOS App  │ │ Android  │
    │  (bestehend) │ │  (neu)   │ │  (neu)   │
    └──────────────┘ └──────────┘ └──────────┘
```

- MAUI-App bleibt im Store und funktioniert weiter
- Neue native Apps werden parallel entwickelt und intern getestet
- Beide Plattformen nutzen die **gleichen API-Endpunkte**
- Kein Backend-Change erforderlich

### Phase B: Geschlossene Beta (Phase 1 + 2)

- Native Apps ueber TestFlight (iOS) und Firebase App Distribution (Android) verteilen
- Gleiche Beta-Tester wie MAUI-App (B2C BetaSupport Claim)
- Nutzer koennen beide Apps parallel nutzen (Daten synchronisieren sich ueber Backend)

### Phase C: Offene Beta (Phase 3)

- Native Apps ueber TestFlight Open Beta / Google Play Open Testing
- Hinweis in MAUI-App: "Neue native App verfuegbar -- jetzt testen!"
- Feedback-Kanal einrichten (E-Mail oder In-App)

### Phase D: Store-Launch (Phase 4)

1. Native Apps im App Store und Google Play veroeffentlichen
2. MAUI-App bekommt Update mit Migration-Hinweis:
   - Banner: "Fakturus Track gibt es jetzt als native App! Bitte wechseln Sie zur neuen Version."
   - Deep Link zum jeweiligen App Store
3. Uebergangszeit: **4 Wochen** Parallelbetrieb nach Launch

### Phase E: MAUI-Abschaltung

1. MAUI-App aus App Store und Google Play entfernen
2. Letztes MAUI-Update: Zeigt nur noch Hinweis auf native App
3. MAUI-Code wird archiviert (Branch `archive/maui-app`)
4. Backend-seitig: Keine Aenderungen (API bleibt gleich)

---

## Daten-Migration

### Es gibt keine Daten-Migration

Das ist der Kern der Strategie: **Alle Daten liegen im Backend.**

Wenn ein Nutzer die native App installiert und sich mit seinem bestehenden B2C-Account anmeldet:
1. Erster Sync holt alle Daten vom Backend
2. Lokale SQLite-Datenbank wird befuellt
3. Nutzer sieht sofort alle seine Daten

### Lokale Daten in der MAUI-App

Lokale, nicht-synchronisierte Daten in der MAUI-App (`fakturus_track.db`) koennen nicht direkt in die native App uebertragen werden. Szenarien:

| Szenario | Auswirkung | Massnahme |
|----------|------------|-----------|
| Alle Daten synchronisiert | Kein Datenverlust | Keine Aktion noetig |
| Pending Sessions vorhanden | Lokale Daten nur in MAUI-App | Nutzer auffordern, vor Wechsel zu synchronisieren |
| Offline-Daten nie synchronisiert | Lokale Daten gehen verloren | Edge Case -- unwahrscheinlich bei regelmaessiger Nutzung |

### Empfehlung
In der MAUI-App vor dem Migrations-Hinweis pruefen:
```csharp
var hasPending = await SyncService.HasPendingSyncsAsync();
if (hasPending)
{
    // Zeige: "Bitte synchronisieren Sie zuerst Ihre Daten,
    // bevor Sie zur neuen App wechseln."
    // + Sync-Button
}
else
{
    // Zeige: "Alle Daten sind synchronisiert.
    // Sie koennen zur neuen App wechseln."
    // + App Store Link
}
```

---

## Benutzer-Kommunikation

### E-Mail/Push an bestehende Nutzer

**Betreff:** Fakturus Track -- Neue native App verfuegbar

**Inhalt:**
> Wir haben Fakturus Track komplett neu entwickelt -- als native App fuer iOS und Android.
>
> Was ist neu:
> - Schnellere, fluidere Bedienung
> - Native iOS/Android Design
> - Offline-first mit zuverlaessigem Sync
> - Alle bisherigen Funktionen und mehr
>
> Ihre Daten:
> Alle Ihre erfassten Zeiten, Urlaubstage und Einstellungen sind bereits in der neuen App verfuegbar. Melden Sie sich einfach mit Ihrem bestehenden Konto an.
>
> [App herunterladen]

### In-App Migration-Banner (MAUI-App)

Mehrstufiger Ansatz:
1. **Woche 1-2 nach Launch:** Dezenter Banner oben: "Neue native App verfuegbar" (dismissable)
2. **Woche 3-4:** Prominenter Banner: "Bitte wechseln Sie zur neuen App. Diese Version wird bald eingestellt."
3. **Nach 4 Wochen:** Vollbild-Overlay: "Diese App wird nicht mehr unterstuetzt. Bitte installieren Sie die neue Fakturus Track App."

---

## Risiken und Mitigation

### Risiko: Nutzer installiert neue App nicht

**Mitigation:**
- Migration-Banner in MAUI-App wird zunehmend prominent
- Push-Notification an Nutzer (wenn erlaubt)
- E-Mail-Kommunikation

### Risiko: Nutzer verliert lokale Daten

**Mitigation:**
- MAUI-App erzwingt Sync vor Migrations-Hinweis
- Neue App zeigt "Erste Synchronisation..." beim ersten Start
- Backend hat alle Daten -- kein Datenverlust bei synchronisierten Nutzern

### Risiko: Unterschiedliches Verhalten Backend

**Mitigation:**
- Keine Backend-Aenderungen in Phase 1-2
- Gleiche API-Version (v1) fuer beide App-Generationen
- Umfassende API-Tests (Postman Collection)

### Risiko: B2C Account-Probleme

**Mitigation:**
- Gleicher B2C-Tenant und gleiche App-Registration (Phase 1)
- Token-Kompatibilitaet sichergestellt
- Nutzer muss sich ggf. neu anmelden (Refresh Token kann abgelaufen sein)

---

## Checkliste vor MAUI-Abschaltung

- [ ] Alle bestehenden Nutzer haben die native App installiert (User-Agent Analyse)
- [ ] Keine aktiven MAUI-App Sessions in den letzten 7 Tagen
- [ ] Native Apps haben mindestens 2 Wochen ohne kritische Bugs laufen
- [ ] App Store Ratings sind stabil (keine Abstuerze gemeldet)
- [ ] Backend-Logs zeigen keine MAUI-spezifischen Requests mehr
- [ ] MAUI-Code ist in separatem Branch archiviert
- [ ] Dokumentation ist aktualisiert (README.md)

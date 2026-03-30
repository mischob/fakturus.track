# EPIC 10: MAUI-Migration & Sunset

## Ziel

Bestehende MAUI-App-Nutzer werden sicher und vollstaendig zur nativen App migriert. Die MAUI-App wird kontrolliert abgeschaltet. Kein Datenverlust bei der Migration.

## Abhaengigkeiten

- **E09 (Launch)**: Native Apps muessen im Store verfuegbar sein
- **migration.md**: Migrationsstrategie als Grundlage (Parallelbetrieb, Banner-Stufen, Sync-Pruefung)

---

## Stories

### P4-E10-S01: MAUI-App Migration-Banner implementieren

**Als** bestehender MAUI-Nutzer
**moechte ich** ueber die neue native App informiert werden,
**damit** ich rechtzeitig wechseln kann bevor die MAUI-App eingestellt wird.

**Plattform**: MAUI-App (C# / Blazor)
**Abhaengigkeiten**: E09 (native Apps im Store)
**Parallelisierbar mit**: P4-E10-S02, P4-E10-S03
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Dreistufiges Banner-System (basierend auf migration.md):
  - **Woche 1-2**: Dezenter Banner oben: "Neue native App verfuegbar -- Jetzt wechseln!"
    - Dismissable (X-Button)
    - App Store / Play Store Link (plattformabhaengig)
  - **Woche 3-4**: Prominenter Banner: "Bitte wechseln Sie zur neuen App. Diese Version wird bald eingestellt."
    - Nicht dismissable
    - Anzeige bei jedem App-Start
  - **Nach 4 Wochen**: Vollbild-Overlay: "Diese App wird nicht mehr unterstuetzt."
    - Nur noch Link zur neuen App + "Trotzdem weiter" Link (klein)
- [ ] Banner-Stufe wird ueber Remote Config oder hardcodiertes Datum gesteuert
- [ ] Pending-Sync-Pruefung VOR dem Migrations-Link:
  - Wenn pending Syncs vorhanden: "Bitte synchronisieren Sie zuerst Ihre Daten" + Sync-Button
  - Wenn keine pending Syncs: "Alle Daten sind synchronisiert. Sie koennen zur neuen App wechseln." + Store-Link
- [ ] Given ein MAUI-Nutzer oeffnet die App in Woche 1
  Then sieht er einen dezenten Banner mit Link zur neuen App
  And kann den Banner schliessen

**Technische Hinweise**:
- C#: `SyncService.HasPendingSyncsAsync()` existiert bereits (siehe migration.md Codebeispiel)
- Plattform-Erkennung: `DeviceInfo.Current.Platform` fuer iOS/Android Store-Link
- iOS Store-Link: `https://apps.apple.com/app/idXXXXXXXXX`
- Play Store-Link: `https://play.google.com/store/apps/details?id=com.fakturus.track`
- Banner-Stufe: Berechnung basierend auf Launch-Datum (z.B. `DateTime.UtcNow - launchDate`)

---

### P4-E10-S02: Migrations-Kommunikation an bestehende Nutzer

**Als** Product Owner
**moechte ich** bestehende Nutzer proaktiv ueber die Migration informieren,
**damit** niemand ueberrascht wird und der Wechsel reibungslos verlaeuft.

**Plattform**: E-Mail / Push (nicht App-Code)
**Abhaengigkeiten**: E09 (Launch-Datum bekannt)
**Parallelisierbar mit**: P4-E10-S01, P4-E10-S03
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] **E-Mail an alle bestehenden Nutzer** (aus B2C-Nutzerliste):
  - Betreff: "Fakturus Track -- Neue native App verfuegbar"
  - Inhalt (basierend auf migration.md):
    - Was ist neu (native Performance, neue Features)
    - Daten-Sicherheit ("Alle Ihre Daten sind bereits in der neuen App verfuegbar")
    - Handlungsanweisung ("Installieren, anmelden, fertig")
    - App Store + Play Store Links
    - Zeitplan: "Die bisherige App wird in 4 Wochen eingestellt"
  - DE und EN Version
- [ ] **Release Notes** in App Store und Play Store:
  - Version 1.0 Release Notes (DE + EN)
  - Highlight: Native App, alle Features, Offline-first
- [ ] Given ein bestehender Nutzer erhaelt die E-Mail
  Then kann er ueber den Link die neue App installieren
  And nach Login sind alle seine Daten vorhanden

**Technische Hinweise**:
- E-Mail-Versand: Azure B2C Custom Email Provider oder manuell ueber SendGrid/Mailchimp
- Nutzerliste aus Azure B2C Graph API exportieren
- Timing: E-Mail am Launch-Tag oder 1 Tag nach Launch (Store-Propagation abwarten)

---

### P4-E10-S03: MAUI-App Sunset-Plan

**Als** Product Owner
**moechte ich** einen klaren Zeitplan fuer die MAUI-App-Abschaltung,
**damit** der Parallelbetrieb zeitlich begrenzt ist und keine Verwirrung entsteht.

**Plattform**: MAUI-App + Stores
**Abhaengigkeiten**: E09 (Launch abgeschlossen), E10-S01 (Banner aktiv)
**Parallelisierbar mit**: Keine (nach Launch)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Woche 1-2 nach Launch**: Parallelbetrieb, dezenter Banner
- [ ] **Woche 3-4**: Prominenter Banner, MAUI-App-Update mit nur noch Migrations-Hinweis einreichen
- [ ] **Nach 4 Wochen**: Pruefen ob Abschaltung moeglich:
  - [ ] Keine aktiven MAUI-App Sessions in den letzten 7 Tagen (Backend User-Agent Analyse)
  - [ ] Native Apps Crash-Free Rate >= 99.5%
  - [ ] App Store Rating stabil (>= 4.0)
- [ ] **Abschaltung**:
  - [ ] MAUI-App aus App Store und Play Store entfernen (nicht loeschen, nur verstecken)
  - [ ] Letztes MAUI-Update: Zeigt nur noch "Bitte installieren Sie Fakturus Track" mit Store-Links
  - [ ] MAUI-Repository archivieren (Branch `archive/maui-app`)
  - [ ] Backend: Pruefen ob MAUI-spezifische Logik entfernt werden kann
- [ ] Given 4 Wochen nach Launch sind alle Nutzer migriert
  Then wird die MAUI-App aus den Stores entfernt
  And das MAUI-Repository wird archiviert

**Technische Hinweise**:
- User-Agent Analyse: Backend-Logs nach MAUI-User-Agent filtern vs. native App User-Agent
- MAUI-App nicht sofort aus Store LOESCHEN (nur "nicht mehr verfuegbar") -- vorhandene Installationen funktionieren weiter
- Git: `git checkout -b archive/maui-app && git push origin archive/maui-app`

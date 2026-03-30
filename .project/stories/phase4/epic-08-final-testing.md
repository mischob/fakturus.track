# EPIC 08: Final Testing & QA

## Ziel

Umfassende Qualitaetssicherung aller Features aus Phase 1-4. Die App ist crash-frei, performant und bereit fuer den oeffentlichen Launch. Beta-Tester haben die App validiert. Crash-Monitoring ist eingerichtet.

## Abhaengigkeiten

- **E01-E04 (Feature-Gating)**: Alle Tier-Funktionalitaet muss testbar sein
- **Phase 1-3**: Alle Features muessen stehen

---

## Stories

### P4-E08-S01: Crash-Monitoring Setup

**Als** Entwickler
**moechte ich** Crash-Reports automatisch erfassen,
**damit** ich nach dem Launch Probleme schnell identifizieren und beheben kann.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: Keine (kann frueh starten)
**Parallelisierbar mit**: Alle
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] Crash-Reporting SDK integriert:
  - Option A: Firebase Crashlytics (kostenlos, Google-Produkt)
  - Option B: Sentry (Self-Hosted moeglich, DSGVO-freundlicher)
  - **Empfehlung: Sentry** (passt besser zum "keine Tracking-SDKs" Versprechen in der Privacy Policy)
- [ ] Crashes werden automatisch erfasst und an Dashboard gesendet
- [ ] Non-Fatal Errors koennen manuell geloggt werden (z.B. Sync-Fehler)
- [ ] Breadcrumbs fuer Debug-Kontext (letzte User-Actions vor Crash)
- [ ] User-ID wird NICHT mitgesendet (DSGVO) -- nur anonyme Crash-Daten
- [ ] Release-Builds haben symbolisierte Stack Traces (dSYM Upload / ProGuard Mapping)
- [ ] Given ein Crash tritt auf
  When die App beim naechsten Start Netzwerk hat
  Then wird der Crash-Report an Sentry gesendet
  And der Stack Trace ist symbolisiert und lesbar

**Technische Hinweise**:
- iOS: `Sentry.startWithOptions { options in options.dsn = "..." }`
- Android: `SentryAndroid.init(context) { options -> options.dsn = "..." }`
- dSYM Upload: In Xcode Build Phases oder via `sentry-cli`
- ProGuard Mapping: Sentry Gradle Plugin
- **Opt-In**: Crash-Reporting beim ersten App-Start fragen oder in Settings anbieten

---

### P4-E08-S02: Vollstaendiger Regressions-Test (Phase 1-3)

**Als** QA-Tester
**moechte ich** alle bestehenden Features systematisch testen,
**damit** keine Regression durch Feature-Gating oder Phase-4-Aenderungen eingefuehrt wurde.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: E01-E04 (Feature-Gating komplett)
**Parallelisierbar mit**: P4-E08-S03
**Geschaetzter Aufwand**: L

**Akzeptanzkriterien**:
- [ ] **Phase-1-Features (Kern)**:
  - [ ] Login via Azure B2C (Apple, Google, E-Mail)
  - [ ] Timer: Start -> Pause -> Weiter -> Stop -> Fertig (kompletter Flow)
  - [ ] Manuelle Session-Erstellung (Datum + Start + Ende + Pause)
  - [ ] Session bearbeiten (Start/Ende/Pause aendern)
  - [ ] Session loeschen (Swipe-to-Delete + Undo)
  - [ ] History: Monatsgruppierung, Scrollen, Aufklappen
  - [ ] Pull-to-Refresh
  - [ ] Offline-Modus: Session erstellen ohne Netzwerk -> Netzwerk wieder an -> Sync
  - [ ] ArbZG-Hinweis: 6h-Pause-Erinnerung, 10h-Limit-Warnung
- [ ] **Phase-2-Features (Tabs)**:
  - [ ] Einstellungen: Wochenstunden, Arbeitstage, Bundesland, Urlaubstage aendern -> Sync
  - [ ] Urlaub-Kalender: Urlaubstag setzen/entfernen, Feiertage angezeigt
  - [ ] Krankheitstag: Long-Press -> Krankheitstag setzen/entfernen
  - [ ] Gesamt-Tab: Ueberstunden korrekt berechnet, Jahresnavigation
  - [ ] PDF-Export: Monatsreport generieren, Share-Sheet oeffnen
  - [ ] CSV-Export: Monat/Quartal/Jahr exportieren
- [ ] **Phase-3-Features (Polish)**:
  - [ ] Dark Mode: System/Hell/Dunkel Toggle funktioniert
  - [ ] Widgets: Timer starten via Widget, Widget aktualisiert sich
  - [ ] VoiceOver/TalkBack: Alle Tabs navigierbar
  - [ ] Lokalisierung: DE komplett, EN komplett (Geraetesprache wechseln)
  - [ ] DATEV-Export: Korrekt formatierte Datei generieren
- [ ] **Phase-4-Features (Gating)**:
  - [ ] FREE-Tier: Nur Timer + History (30 Tage) + Pausen + Feiertage
  - [ ] STARTER-Tier: + Export, Urlaub, Krank, Widgets, Dashboard
  - [ ] PRO-Tier: + DATEV, Schulferien, Kalender-Integration
  - [ ] Tier-Wechsel: Upgrade und Downgrade korrekt
- [ ] Alle Tests auf BEIDEN Plattformen bestanden
- [ ] Keine kritischen oder hohen Bugs offen

**Technische Hinweise**:
- Testprotokoll als Checkliste fuehren (diese Story IST das Testprotokoll)
- Jeden Bug als Issue erfassen mit Reproduktionsschritten
- Testen auf mindestens 2 iOS-Versionen (17, 18) und 2 Android-Versionen (13, 14)

---

### P4-E08-S03: Feature-Gating Spezial-Tests

**Als** QA-Tester
**moechte ich** alle Tier-Uebergaenge und Edge Cases des Feature-Gating testen,
**damit** das Freemium-Modell zuverlaessig funktioniert.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: E01-E04
**Parallelisierbar mit**: P4-E08-S02
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Kauf-Flow testen** (Sandbox/Testumgebung):
  - [ ] Starter-Kauf: Paywall -> Kauf -> Features freigeschaltet
  - [ ] Pro-Kauf: Paywall -> Kauf -> Alle Features freigeschaltet
  - [ ] Upgrade Starter -> Pro: Sofortiger Wechsel, alle Features
  - [ ] Kauf abbrechen: Keine Aenderung, kein Fehler
  - [ ] Restore Purchases: Nach App-Neuinstallation Abo wiederherstellen
- [ ] **Downgrade-Szenarien**:
  - [ ] STARTER -> FREE: Urlaub/Krank Read-Only, History auf 30 Tage, Widgets zeigen Hinweis
  - [ ] PRO -> STARTER: DATEV/Schulferien gesperrt, Rest funktioniert
  - [ ] PRO -> FREE: Alles gesperrt bis auf Kern-Features
- [ ] **Offline-Szenarien**:
  - [ ] Abo gekauft, dann offline: Features bleiben verfuegbar (gecachter Tier)
  - [ ] App gestartet ohne Netzwerk: Letzter bekannter Tier wird genutzt
  - [ ] Abo laeuft ab waehrend offline: Wird bei naechster Online-Pruefung aktualisiert
- [ ] **Edge Cases**:
  - [ ] Zwei Geraete, gleicher Account: Abo auf beiden aktiv (Family Sharing nicht unterstuetzt)
  - [ ] App-Deininstall + Reinstall: Restore Purchases stellt Abo wieder her
  - [ ] Waehrend Kauf: App in Background -> Vordergrund -> Kauf abschliessen

**Technische Hinweise**:
- iOS: StoreKit Testing in Xcode (Sandbox) -- Abo laeuft in Sandbox alle 5 Minuten ab
- Android: License Testing in Play Console -- Testkaeufe ohne echte Zahlung
- Downgrade-Test: Abo in Sandbox kuendigen, warten bis es ablaeuft

---

### P4-E08-S04: Open Beta (TestFlight + Play Internal Testing)

**Als** Product Owner
**moechte ich** die App vor dem Launch einer breiteren Testgruppe zur Verfuegung stellen,
**damit** wir Feedback von echten Nutzern erhalten und kritische Bugs vor dem Launch finden.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P4-E08-S02 (Regressions-Test bestanden)
**Parallelisierbar mit**: Keine (nach Regression)
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **iOS TestFlight**:
  - Build auf TestFlight hochgeladen
  - External Testing Group eingerichtet (oeffentlicher Link oder E-Mail-Einladung)
  - Beta App Review bestanden
  - "Was ist neu" Text fuer Beta-Tester
  - Feedback-Kanal kommuniziert (E-Mail oder TestFlight-Feedback)
- [ ] **Android Internal Testing**:
  - Build auf Internal Testing Track hochgeladen
  - Tester-Liste konfiguriert (Google-Accounts)
  - Opt-In Link an Tester verteilt
  - Feedback-Kanal kommuniziert
- [ ] Mindestens 10 externe Tester haben die App genutzt
- [ ] Feedback gesammelt und kritische Issues behoben
- [ ] Crash-Free Rate >= 99.5% in Beta
- [ ] Given ein externer Tester installiert die Beta-Version
  When er die App normal nutzt (Timer, Export, Urlaub)
  Then funktioniert alles wie erwartet
  And er kann Feedback geben

**Technische Hinweise**:
- TestFlight: External Testing erfordert Beta App Review (1-2 Tage)
- Play Internal Testing: Kein Review erforderlich, sofort verfuegbar
- Beta-Dauer: Mindestens 1 Woche vor Launch-Submission
- Crash-Free Rate in Sentry/Crashlytics Dashboard pruefen

---

### P4-E08-S05: Performance-Finale & Store-Readiness

**Als** Entwickler
**moechte ich** die finalen Performance-Werte validieren,
**damit** die App die Store-Anforderungen und unsere eigenen Qualitaetsstandards erfuellt.

**Plattform**: Beide (iOS + Android)
**Abhaengigkeiten**: P4-E08-S02
**Parallelisierbar mit**: P4-E08-S04
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] **Performance-Ziele erreicht:**
  - Cold Start: < 1 Sekunde (iOS + Android)
  - History Scrolling: 60fps mit 200+ Eintraegen
  - Tab-Wechsel: < 200ms
  - Memory: < 50MB iOS, < 80MB Android
  - App-Groesse: < 30MB (Download-Groesse)
- [ ] **Kein bekannter Memory Leak** (Instruments / Android Profiler)
- [ ] **Crash-Free Rate**: >= 99.5% (aus Beta-Phase)
- [ ] **Netzwerk**: Sync funktioniert zuverlaessig (>= 99% Erfolgsrate)
- [ ] **Batterie**: Kein uebermaeassiger Batterieverbrauch im Background
- [ ] Release-Build (kein Debug) getestet
- [ ] Minimum OS-Version getestet: iOS 17, Android 13 (API 33)

**Technische Hinweise**:
- iOS: Instruments (Time Profiler, Allocations, Leaks, Energy Log)
- Android: Android Studio Profiler (CPU, Memory, Energy)
- App-Groesse: Xcode Archive -> App Thinning Size Report, Play Console -> App Size

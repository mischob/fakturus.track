# Phase 4: Store-Launch, Feature-Gating & Migration -- Detailplanung

## Scope-Zusammenfassung

Phase 4 ist die **letzte Phase vor dem oeffentlichen Launch**. Sie macht aus der store-reifen Beta-App ein veroeffentlichtes Produkt mit Freemium-Geschaeftsmodell:

- **Feature-Gating & In-App Purchase**: StoreKit 2 (iOS) + Google Play Billing (Android), FREE/STARTER/PRO Tier-System, Paywall-UI, Restore Purchases
- **App Store Vorbereitung (iOS)**: Screenshots, Beschreibung (DE/EN), Keywords, Privacy Policy, Review Guidelines Compliance
- **Google Play Store Vorbereitung (Android)**: Screenshots, Store Listing (DE/EN), Data Safety, Content Rating
- **Final Testing & QA**: Vollstaendiger Regressions-Test (Phase 1-3), Open Beta (TestFlight + Play Internal Testing), Crash-Monitoring Setup
- **Launch**: App Store Submission, Launch-Kommunikation

**Zeitraum**: ~4 Wochen + 1 Woche Puffer (E01-E09)
**Ergebnis**: Veroeffentlichte Apps in App Store und Google Play mit funktionierendem Freemium-Modell

---

## EPIC-Uebersicht

| EPIC | Titel | Geschaetzte Dauer | Abhaengigkeiten |
|------|-------|-------------------|-----------------|
| E01 | Feature-Gating Infrastruktur | 1 Woche | Phase 3 abgeschlossen |
| E02 | In-App Purchase: iOS (StoreKit 2) | 1 Woche | E01 (Tier-System) |
| E03 | In-App Purchase: Android (Play Billing) | 1 Woche | E01 (Tier-System) |
| E04 | Paywall-UI & Upgrade-Flow | 0.5 Wochen | E01, E02, E03 |
| E05 | App Store Vorbereitung (iOS) | 1 Woche | Phase 3 (finale UI fuer Screenshots) |
| E06 | Google Play Store Vorbereitung (Android) | 1 Woche | Phase 3 (finale UI fuer Screenshots) |
| E07 | Privacy Policy & Rechtliches | 0.5 Wochen | Keine |
| E08 | Final Testing & QA | 1.5 Wochen | E01-E04 (Feature-Gating komplett) |
| E09 | Launch-Submission & Go-Live | 0.5 Wochen | E05, E06, E07, E08 |

---

## Abhaengigkeitsdiagramm

```
                         +------------------+
                         |   Phase 3        |
                         |  abgeschlossen   |
                         +--------+---------+
                                  |
          +-----------+-----------+-----------+-----------+
          |           |           |           |           |
    +-----v-----+ +--v------+ +-v-------+ +-v------+ +--v------+
    |   E01     | |  E05    | |  E06    | |  E07   | |         |
    | Feature-  | |iOS Store| |Play     | |Privacy | |         |
    | Gating    | |Vorbereit| |Store    | |Policy  | |         |
    | Infra     | |         | |Vorber.  | |        | |         |
    +-----+-----+ +---------+ +---------+ +--------+ |         |
          |                                           |         |
    +-----+-----+                                     |         |
    |           |                                     |         |
+---v----+ +---v----+                                 |         |
|  E02   | |  E03   |                                 |         |
|iOS IAP | |Android |                                 |         |
|StoreKit| |Billing |                                 |         |
+---+----+ +---+----+                                 |         |
    |          |                                      |         |
    +-----+----+                                      |         |
          |                                           |         |
    +-----v-----+                                     |         |
    |   E04     |                                     |         |
    | Paywall-  |                                     |         |
    | UI        |                                     |         |
    +-----+-----+                                     |         |
          |                                           |         |
    +-----v------------------------------------------v---------v+
    |                        E08                                |
    |                 Final Testing & QA                         |
    +------------------------------+----------------------------+
                                   |
                            +------v------+
                            |    E09      |
                            | Launch &    |
                            | Go-Live     |
                            +------+------+
                                   |
```

**Erlaeuterung**:
- **E01 (Feature-Gating)** ist Voraussetzung fuer E02, E03 und E04 -- das Tier-System muss stehen bevor IAP integriert wird
- **E02 (iOS IAP) und E03 (Android IAP)** laufen parallel (verschiedene Plattformen)
- **E05 (iOS Store) und E06 (Play Store)** laufen parallel und unabhaengig von Feature-Gating
- **E07 (Privacy Policy)** hat keine Abhaengigkeit und kann jederzeit erstellt werden
- **E08 (Testing)** erfordert, dass Feature-Gating komplett ist, um alle Tiers testen zu koennen
- **E09 (Launch)** ist der finale Gate -- alle Store-Vorbereitung, Testing und Rechtliches muessen abgeschlossen sein
---

## Parallelitaets-Matrix

**Legende**: P = Parallel moeglich, S = Sequentiell (Abhaengigkeit)

|       | E01 | E02 | E03 | E04 | E05 | E06 | E07 | E08 | E09 |
|-------|-----|-----|-----|-----|-----|-----|-----|-----|-----|
| E01   | -   | S   | S   | S   | P   | P   | P   | S   | S   |
| E02   |     | -   | P   | S   | P   | P   | P   | S   | S   |
| E03   |     |     | -   | S   | P   | P   | P   | S   | S   |
| E04   |     |     |     | -   | P   | P   | P   | S   | S   |
| E05   |     |     |     |     | -   | P   | P   | P   | S   |
| E06   |     |     |     |     |     | -   | P   | P   | S   |
| E07   |     |     |     |     |     |     | -   | P   | S   |
| E08   |     |     |     |     |     |     |     | -   | S   |
| E09   |     |     |     |     |     |     |     |     | -   |

**Maximale Parallelitaet:**
- Bis zu **5 EPICs** koennen gleichzeitig in Arbeit sein (E01+E05+E06+E07 in Welle 1)
- iOS-Agent und Android-Agent arbeiten IMMER parallel am gleichen Feature
- Store-Vorbereitung (E05/E06) ist komplett unabhaengig von Feature-Gating (E01-E04)

---

## Kritischer Pfad

```
Pfad A (Feature-Gating -> Testing -> Launch):
E01 (1 Wo) -> E02+E03 parallel (1 Wo) -> E04 (0.5 Wo) -> E08 (1.5 Wo) -> E09 (0.5 Wo)
= 4.5 Wochen  <-- KRITISCHER PFAD

Pfad B (Store-Vorbereitung -> Launch):
E05+E06 parallel (1 Wo) + E07 (0.5 Wo) -> E09 (0.5 Wo)
= 1.5 Wochen (kein Engpass)

```

**Kritischer Pfad: ~4 Wochen** bei optimaler Parallelisierung + 1 Woche Puffer fuer:
- App Store Review Ablehnung (kann 1-5 Tage kosten)
- StoreKit 2 / Play Billing Sandbox-Probleme
- Last-Minute Bug-Fixes nach Beta-Feedback

**Risiko-Mitigation**: Store-Vorbereitung (E05+E06) und Privacy Policy (E07) werden parallel zur Feature-Gating-Entwicklung gestartet, um den kritischen Pfad nicht weiter zu belasten.

---

## Feature-Gating Tier-Zuordnung

Definitive Zuordnung der Features zu den Tiers (Basis: features.md + Preisanalyse):

| Feature | FREE | STARTER (2,99 EUR/Mo) | PRO (4,99 EUR/Mo) |
|---------|------|-----------------------|--------------------|
| Timer (Start/Stop/Finish) | x | x | x |
| Pausenerfassung | x | x | x |
| History-Ansicht | x | x | x |
| Offline-Sync | x | x | x |
| Feiertage (Bundesland) | x | x | x |
| Dark Mode | x | x | x |
| Widgets | x | x | x |
| Ueberstunden-Dashboard | x | x | x |
| PDF-Monatsreport | - | x | x |
| CSV/Excel-Export | - | x | x |
| Krankheitstage | - | x | x |
| Urlaubsverwaltung | - | x | x |
| DATEV-Export | - | - | x |
| Schulferien | - | - | x |
| Kalender-Integration | - | - | x |

**FREE-Einschraenkungen:**
- Historie begrenzt auf 30 Tage (aeltere Eintraege werden ausgeblendet, nicht geloescht)
- Kein Export
- Kein Urlaub/Krankheitstage

**Wichtig:** Gesetzlich vorgeschriebene Features (Timer, Pausen, Feiertage) MUESSEN im FREE-Tier bleiben. Dies ist aus ArbZG-Konformitaetsgruenden nicht verhandelbar.

---

## Dateien in diesem Ordner

| Datei | Inhalt |
|-------|--------|
| [epic-01-feature-gating.md](epic-01-feature-gating.md) | Feature-Gating Infrastruktur |
| [epic-02-ios-iap.md](epic-02-ios-iap.md) | In-App Purchase iOS (StoreKit 2) |
| [epic-03-android-billing.md](epic-03-android-billing.md) | In-App Purchase Android (Play Billing) |
| [epic-04-paywall-ui.md](epic-04-paywall-ui.md) | Paywall-UI & Upgrade-Flow |
| [epic-05-ios-store.md](epic-05-ios-store.md) | App Store Vorbereitung (iOS) |
| [epic-06-play-store.md](epic-06-play-store.md) | Google Play Store Vorbereitung (Android) |
| [epic-07-privacy-legal.md](epic-07-privacy-legal.md) | Privacy Policy & Rechtliches |
| [epic-08-final-testing.md](epic-08-final-testing.md) | Final Testing & QA |
| [epic-09-launch.md](epic-09-launch.md) | Launch-Submission & Go-Live |
| [execution-waves.md](execution-waves.md) | Ausfuehrungsplan in Wellen |
| [implementation-checklist.md](implementation-checklist.md) | Phase-4-spezifische Checklisten |

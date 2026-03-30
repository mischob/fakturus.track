# Ausfuehrungsplan -- Phase 4 in Wellen

## Uebersicht

Phase 4 wird in 4 Wellen ausgefuehrt (E01-E09). Feature-Gating ist der kritische Pfad. Store-Vorbereitung und Rechtliches laufen parallel dazu.

**Parallel-Kapazitaet**: iOS-Agent + Android-Agent + 1 Mensch (Store-Konfiguration, Rechtliches)

```
Phase 4 (~4 Wochen):
Woche  24        25        26        27
      +--------+--------+--------+--------+
      | W1              | W2     | W3     | W4
      | Gating-Infra +  | IAP +  | Test + | Launch
      | Store-Vorb. +   | Paywall| Beta   |
      | Legal            |        |        |
```

---

## Welle 1: Feature-Gating Infra + Store-Vorbereitung + Legal (Woche 24-25)

**Ziel**: Feature-Gate-System steht. Store-Materialien sind erstellt. Rechtliche Dokumente sind online.

**Voraussetzungen**: Phase 3 abgeschlossen.

**Dies ist die breiteste Welle -- sie nutzt maximale Parallelitaet, weil Feature-Gating, Store-Vorbereitung und Rechtliches voellig unabhaengig sind.**

### Parallel-Strang A: Feature-Gating Infrastruktur (iOS-Agent + Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E01-S01 | Feature-Flag-System | Beide | M |
| P4-E01-S02 | iOS Feature-Gate Integration | iOS | L |
| P4-E01-S03 | Android Feature-Gate Integration | Android | L |
| P4-E01-S04 | Tier-Downgrade Handling | Beide | M |

**Reihenfolge**: S01 zuerst (Shared), dann S02+S03 parallel (iOS/Android), dann S04.

### Parallel-Strang B: Store-Konfiguration (Mensch / Product Owner)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E02-S01 | App Store Connect Produkt-Setup | iOS | S |
| P4-E03-S01 | Google Play Console Produkt-Setup | Android | S |
| P4-E05-S02 | App Store Listing (Beschreibung, Keywords) | iOS | M |
| P4-E06-S02 | Google Play Store Listing | Android | M |

**Reihenfolge**: S01-Produkte zuerst (IAP-Voraussetzung), dann Listings parallel.

### Parallel-Strang C: Screenshots (nach UI-Finalisierung)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E05-S01 | App Store Screenshots | iOS | M |
| P4-E06-S01 | Google Play Screenshots | Android | M |

### Parallel-Strang D: Rechtliches (Mensch / Product Owner)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E07-S01 | Datenschutzerklaerung (Privacy Policy) | Web | M |
| P4-E07-S02 | Nutzungsbedingungen (Terms of Use) | Web | S |
| P4-E07-S03 | Impressum & App-Info | Beide | S |

### Parallel-Strang E: Crash-Monitoring (kann frueh starten)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E08-S01 | Crash-Monitoring Setup (Sentry) | Beide | M |

**Welle 1 DoD**: Feature-Flag-System implementiert und in alle bestehenden Views integriert. Subscription-Produkte in App Store Connect und Play Console angelegt. Store Listings (Beschreibung, Keywords) geschrieben. Screenshots erstellt. Privacy Policy und Terms of Use online. Sentry integriert.

---

## Welle 2: In-App Purchase + Paywall (Woche 25-26)

**Ziel**: Nutzer koennen Abos kaufen, verwalten und wiederherstellen. Paywall motiviert zum Upgrade.

**Voraussetzungen**: Welle 1 Strang A (Feature-Gating Infra) + Strang B (Produkt-Setup in Stores)

### Parallel-Strang A: iOS In-App Purchase (iOS-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E02-S02 | StoreKit 2 Integration | iOS | L |
| P4-E02-S03 | Restore Purchases (iOS) | iOS | S |

### Parallel-Strang B: Android In-App Purchase (Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E03-S02 | Google Play Billing Integration | Android | L |
| P4-E03-S03 | Restore Purchases (Android) | Android | S |

### Parallel-Strang C: Paywall-UI (nach IAP-Integration)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E04-S01 | iOS Paywall Screen | iOS | M |
| P4-E04-S02 | Android Paywall Screen | Android | M |
| P4-E04-S03 | Upgrade-Erfolgs-Flow | Beide | S |

**Reihenfolge**: Strang A+B parallel, dann Strang C (braucht funktionierenden Kauf-Flow).

### Parallel-Strang D: Store Review Compliance (kann parallel starten)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E05-S03 | App Store Review Compliance | iOS | M |
| P4-E06-S03 | Data Safety Section & Content Rating | Android | M |

**Welle 2 DoD**: Kauf-Flow funktioniert auf beiden Plattformen (Sandbox). Restore Purchases funktioniert. Paywall-UI zeigt dynamische Preise und Feature-Vergleich. Review Compliance ist geprueft.

---

## Welle 3: Final Testing & Beta (Woche 26-27)

**Ziel**: Alle Features sind getestet, keine kritischen Bugs. Beta-Tester haben die App validiert.

**Voraussetzungen**: Welle 2 abgeschlossen (gesamtes Feature-Gating + IAP funktioniert)

### Strang A: Regressions-Test (iOS-Agent + Android-Agent)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E08-S02 | Vollstaendiger Regressions-Test | Beide | L |
| P4-E08-S03 | Feature-Gating Spezial-Tests | Beide | M |
| P4-E08-S05 | Performance-Finale | Beide | S |

### Strang B: Open Beta (nach Regressions-Test)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E08-S04 | Open Beta (TestFlight + Play Internal) | Beide | M |

**Reihenfolge**: Strang A zuerst (komplett), dann Strang B (Beta-Build nur nach bestandenem Test).

**Welle 3 DoD**: Alle Phase 1-4 Features auf beiden Plattformen getestet. Keine kritischen Bugs. Feature-Gating und IAP funktionieren in Sandbox. Performance-Ziele erreicht. Mindestens 10 Beta-Tester haben die App genutzt. Crash-Free Rate >= 99.5%.

---

## Welle 4: Launch (Woche 27)

**Ziel**: Apps sind in beiden Stores veroeffentlicht und zum Download verfuegbar.

**Voraussetzungen**: Welle 3 (Testing bestanden, Beta-Feedback eingearbeitet), Welle 1 Strang D (Privacy Policy online)

| Story-ID | Titel | Plattform | Aufwand |
|----------|-------|-----------|---------|
| P4-E09-S01 | iOS App Store Submission | iOS | S |
| P4-E09-S02 | Google Play Store Submission | Android | S |
| P4-E09-S03 | Koordinierter Launch | Beide | S |

**Reihenfolge**: S01+S02 parallel (Submissions), dann S03 (nach Review-Approval beider Stores).

**Welle 4 DoD**: Beide Apps sind im jeweiligen Store verfuegbar. Downloads funktionieren. IAP funktioniert mit echtem Geld. Keine kritischen Bugs in den ersten 24h.

---

## Zusammenfassung: Story-Counts pro Welle

| Welle | Stories | Aufwand-Schwerpunkt | Wochen |
|-------|---------|---------------------|--------|
| W1 | 12 | Feature-Gating + Store-Setup + Legal | 1.5 |
| W2 | 9 | IAP Integration + Paywall + Compliance | 1 |
| W3 | 5 | Testing + Beta | 1 |
| W4 | 3 | Submission + Launch | 0.5 |
| **Gesamt** | **29 Stories** | | **~4 Wochen** |

---

## Diagramm: Parallelitaet ueber Zeit

```
Woche:  24          25          26          27          28
iOS:   [FeatureFlags─][GateViews────────]
                      [StoreKit2────────][Restore]
                                         [PaywallUI──][Success]
                                         [ReviewComp.]
       [Screenshots──]
                                                  [RegrTest────][BetaTest──]
                                                                     [Submission][LAUNCH]

Andr:  [FeatureFlags─][GateViews────────]
                      [PlayBilling──────][Restore]
                                         [PaywallUI──][Success]
                                         [DataSafety─]
       [Screenshots──]
                                                  [RegrTest────][BetaTest──]
                                                                     [Submission][LAUNCH]

PO:    [ASC-Products][PlayProducts]
       [iOS-Listing──][Play-Listing──]
       [Privacy─][Terms][Impressum]

Crash: [SentrySetup──]

```

**Lesehinweis**: Bloecke die vertikal uebereinander stehen laufen parallel. Ein Agent bearbeitet Bloecke in seiner Zeile von links nach rechts.

---

## Kritischer Pfad (Wellen-Perspektive)

```
W1 Feature-Gating (1.5 Wo) -> W2 IAP+Paywall (1 Wo) -> W3 Testing (1 Wo) -> W4 Launch (0.5 Wo)
= ~4 Wochen (inkl. 1 Woche Puffer)
```

**Groesstes Risiko**: App Store Review Ablehnung. Mitigation: Fruehzeitige Compliance-Pruefung (E05-S03), "Notes for Reviewers" sorgfaeltig ausfuellen, TestFlight Beta als Pre-Validation.

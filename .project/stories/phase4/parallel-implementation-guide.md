# Parallel-Implementation-Guide Phase 4

## Grundprinzip

Phase 4 hat drei unabhaengige Arbeitsstroeme die von Anfang an parallel laufen koennen. Erst ab Welle 3 (Testing) muessen alle zusammenlaufen.

```
Strom A: Feature-Gating + IAP + Paywall    (iOS-Agent + Android-Agent)
Strom B: Store-Vorbereitung                 (PO / Mensch)
Strom C: Rechtliches                        (PO / Mensch)
```

---

## Welle 1 (Woche 24-25): Maximale Parallelitaet

### 5 parallele Aktivitaeten

```
iOS-Agent:       [E01-S01 Tier+FeatureGate] -> [E01-S02 Gate-Views] -> [E01-S04 Downgrade]
Android-Agent:   [E01-S01 Tier+FeatureGate] -> [E01-S03 Gate-Views] -> [E01-S04 Downgrade]
PO (Stores):     [E02-S01 ASC Products] + [E03-S01 Play Products] -> [E05-S02 iOS Listing] + [E06-S02 Play Listing]
PO (Legal):      [E07-S01 Privacy] -> [E07-S02 Terms] -> [E07-S03 Impressum]
Crash-Monitor:   [E08-S01 Sentry iOS + Android]
```

### Synchronisationspunkt E01-S01

E01-S01 (Feature-Flag-System) ist die einzige Story die BEIDE Plattformen betrifft. Empfohlenes Vorgehen:

1. **Zuerst**: `Tier.swift` + `FeatureGate.swift` + `SubscriptionManager.swift` auf iOS implementieren
2. **Dann sofort parallel**: Android-Agent implementiert die identische Logik in Kotlin
3. **Zeitgleich**: iOS-Agent startet mit E01-S02 (Gate-Views iOS)

Die Datenmodelle (Tier, FeatureGate) muessen auf beiden Plattformen identisch sein. Die Implementation-Checklist definiert die Product-IDs und Feature-Zuordnungen verbindlich.

### Screenshots parallel zu Gating

E05-S01 (iOS Screenshots) und E06-S01 (Play Screenshots) koennen sofort starten, da sie die **Phase-3-UI** abfotografieren (ohne Feature-Gating). Feature-Gating aendert die UI nur fuer FREE-Nutzer -- Screenshots zeigen die PRO-Ansicht.

---

## Welle 2 (Woche 25-26): IAP parallel auf beiden Plattformen

### iOS und Android komplett unabhaengig

```
iOS-Agent:       [E02-S02 StoreKit 2] -> [E02-S03 Restore] -> [E04-S01 Paywall iOS]
Android-Agent:   [E03-S02 Play Billing] -> [E03-S03 Restore] -> [E04-S02 Paywall Android]
PO (Compliance): [E05-S03 Review Compliance] + [E06-S03 Data Safety]
```

**Keine Abhaengigkeit zwischen iOS und Android**: StoreKit 2 und Play Billing sind voellig unterschiedliche APIs. Die Agents koennen komplett unabhaengig arbeiten.

### Voraussetzung aus Welle 1

- E01-S01 muss fertig sein (SubscriptionManager, der von StoreKitManager/BillingManager gefuettert wird)
- E02-S01 / E03-S01 muessen fertig sein (Produkte in ASC / Play Console angelegt)

### Paywall nach IAP

E04-S01 (iOS Paywall) und E04-S02 (Android Paywall) koennen erst implementiert werden, wenn der jeweilige Kauf-Flow funktioniert. Aber: Die Paywall-UI (Layout, Feature-Tabelle) kann schon parallel zur IAP-Integration gebaut werden -- nur der `purchase()`-Aufruf wird erst spaeter verdrahtet.

**Empfehlung**: Paywall-UI als Stub anfangen (hardcodierte Preise), dann dynamische Preise einbauen sobald Products geladen werden.

### E04-S03 (Upgrade-Erfolgs-Flow)

Letzte Story in Welle 2. Benoetigt funktionierenden Kauf auf beiden Plattformen. Beide Agents implementieren das gleichzeitig, jeder fuer seine Plattform.

---

## Welle 3 (Woche 26-27): Testing -- Zusammenlauf

### Hier muessen alle Stroeme zusammenkommen

```
Voraussetzungen (alle muessen fertig sein):
  - Feature-Gating komplett (E01)
  - IAP funktioniert auf beiden Plattformen (E02, E03)
  - Paywall-UI fertig (E04)
  - Sentry integriert (E08-S01)
  - Privacy Policy online (E07)

Testing:
  iOS-Agent:       [E08-S02 Regressions-Test iOS] + [E08-S03 Gating-Tests iOS]
  Android-Agent:   [E08-S02 Regressions-Test Android] + [E08-S03 Gating-Tests Android]
  Beide:           [E08-S05 Performance-Check]
  PO:              [E08-S04 Beta-Verteilung + Feedback sammeln]
```

### Parallel innerhalb Testing

- E08-S02 (Regressions-Test) und E08-S03 (Gating-Tests) koennen auf jeder Plattform parallel laufen
- iOS und Android Testing sind komplett unabhaengig
- E08-S05 (Performance) kann parallel zu E08-S02/S03 laufen
- E08-S04 (Beta) erst NACH bestandenen Tests

---

## Welle 4 (Woche 27): Launch -- Parallel-Submission

```
iOS-Agent:       [E09-S01 App Store Submission]
Android-Agent:   [E09-S02 Play Store Submission]
Beide:           [--- Warten auf Review ---]
PO:              [E09-S03 Koordinierter Launch am selben Tag]
```

iOS und Android Submissions sind komplett parallel. Der einzige Synchronisationspunkt ist E09-S03: Beide muessen approved sein bevor einer gelauncht wird.

---

## Kritische Abhaengigkeitskette

```
E01-S01 (Tier + FeatureGate + SubscriptionManager)
    |
    +---> E01-S02/S03 (Gate-Views) -- parallel iOS/Android
    |         |
    |         +---> E01-S04 (Downgrade Handling)
    |
    +---> E02-S02 (StoreKit 2) ----+
    |                               |
    +---> E03-S02 (Play Billing) --+
                                    |
                                    +---> E04-S01/S02 (Paywall) -- parallel iOS/Android
                                              |
                                              +---> E04-S03 (Erfolgs-Flow)
                                                        |
                                                        +---> E08-S02/S03 (Testing)
                                                                   |
                                                                   +---> E08-S04 (Beta)
                                                                             |
                                                                             +---> E09 (Launch)
```

**Laenge des kritischen Pfads**: 8 Stories sequentiell = ~4.5 Wochen

---

## Agent-Zuweisung

### iOS-Agent arbeitet an:

| Welle | Stories | Aufwand |
|-------|---------|---------|
| W1 | E01-S01 (Shared), E01-S02 (iOS Views), E01-S04 (Shared) | L |
| W1 | E08-S01 (Sentry iOS) | S |
| W2 | E02-S02 (StoreKit 2), E02-S03 (Restore), E04-S01 (Paywall), E04-S03 (Erfolg) | L |
| W3 | E08-S02 (Regression iOS), E08-S03 (Gating iOS), E08-S05 (Perf iOS) | L |
| W4 | E09-S01 (Submission) | S |

### Android-Agent arbeitet an:

| Welle | Stories | Aufwand |
|-------|---------|---------|
| W1 | E01-S01 (Shared), E01-S03 (Android Views), E01-S04 (Shared) | L |
| W1 | E08-S01 (Sentry Android) | S |
| W2 | E03-S02 (Play Billing), E03-S03 (Restore), E04-S02 (Paywall), E04-S03 (Erfolg) | L |
| W3 | E08-S02 (Regression Android), E08-S03 (Gating Android), E08-S05 (Perf Android) | L |
| W4 | E09-S02 (Submission) | S |

### PO / Mensch arbeitet an:

| Welle | Stories | Aufwand |
|-------|---------|---------|
| W1 | E02-S01 (ASC Products), E03-S01 (Play Products) | S |
| W1 | E05-S01/S02 (Screenshots + Listing iOS) | M |
| W1 | E06-S01/S02 (Screenshots + Listing Android) | M |
| W1 | E07-S01/S02/S03 (Privacy, Terms, Impressum) | M |
| W2 | E05-S03 (Review Compliance), E06-S03 (Data Safety) | M |
| W3 | E08-S04 (Beta-Verteilung) | M |
| W4 | E09-S03 (Koordinierter Launch) | S |
---

## Checkliste: Wann kann die naechste Welle starten?

### Welle 1 -> Welle 2

- [ ] `Tier.swift/kt` + `FeatureGate.swift/kt` + `SubscriptionManager.swift/kt` existieren und kompilieren
- [ ] Alle bestehenden Views haben Gate-Checks (Lock-Icons sichtbar im FREE-Tier)
- [ ] Produkte in App Store Connect und Play Console angelegt (Product IDs matchen)
- [ ] Sentry integriert und sendet Test-Events

### Welle 2 -> Welle 3

- [ ] StoreKit 2 Kauf funktioniert in Sandbox (iOS)
- [ ] Play Billing Kauf funktioniert in License Testing (Android)
- [ ] Restore Purchases funktioniert auf beiden Plattformen
- [ ] Paywall-UI zeigt dynamische Preise
- [ ] Upgrade-Flow: Kauf -> Tier-Update -> UI-Aktualisierung -> Erfolgs-Meldung
- [ ] Privacy Policy + Terms of Use URLs erreichbar

### Welle 3 -> Welle 4

- [ ] Alle Regressions-Tests bestanden (Phase 1-4)
- [ ] Feature-Gating Spezial-Tests bestanden (alle Tier-Uebergaenge)
- [ ] Performance-Ziele erreicht
- [ ] Beta-Phase abgeschlossen, kritische Bugs behoben
- [ ] Crash-Free Rate >= 99.5%
- [ ] Screenshots und Store Listings final


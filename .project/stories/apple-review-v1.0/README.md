# Apple App Review Fixes -- Version 1.0

## Kontext

Ersteinreichung von FakturusTrack v1.0 wurde am 08.04.2026 von Apple abgelehnt.
Submission ID: `6b1e6a8c-07a8-4418-950a-d699cdce5d88`
Review Device: iPhone 17 Pro Max, iOS 26.4

## Ablehnungsgruende

| # | Guideline | Schwere | Zusammenfassung |
|---|-----------|---------|-----------------|
| 1 | 2.1(a) Performance - App Completeness | Blocker | Sign in with Apple liefert Fehler |
| 2 | 3.1.2(c) Business - Subscriptions | Blocker | Fehlender EULA/Terms-of-Use-Link in App Store Metadaten |
| 3 | 2.1(b) Performance - App Completeness | Blocker | IAP-Produkte (Starter Yearly, Starter Monthly, Pro Monthly) nicht zum Review eingereicht |

## Epics

- [Epic 01: Sign in with Apple Fix](epic-01-sign-in-with-apple.md)
- [Epic 02: App Store Metadaten EULA-Link](epic-02-eula-metadata.md)
- [Epic 03: IAP-Produkte einreichen](epic-03-iap-submission.md)

## Abhaengigkeiten

```
E01 (Apple Sign-In) ──┐
E02 (EULA-Link)    ──┤── Resubmission
E03 (IAP-Produkte) ──┘
```

Alle drei Issues muessen geloest sein bevor die App erneut eingereicht werden kann.
Keine Abhaengigkeiten untereinander -- alle drei sind parallelisierbar.

## Kritischer Pfad

E01 ist der einzige mit Code-Aenderungen und erfordert Debugging/Testing.
E02 und E03 sind reine App Store Connect Konfigurationsaenderungen.

## Ziel-Zeitrahmen

Schnellstmoeglich resubmitten -- idealerweise innerhalb von 2-3 Tagen.

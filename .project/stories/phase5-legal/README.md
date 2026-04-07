# Phase 5: Legal Compliance -- Rechtliche Grundlagen & Consent-Management

## Motivation

fakturus.track verlinkt in der PaywallView und den Einstellungen bereits auf `https://track.fakturus.com/privacy`, `/terms` und `/imprint` -- aber **keiner dieser Endpunkte existiert**. Das Backend liefert keine dieser Seiten aus. Darueber hinaus fehlt ein vollstaendiger Consent-Mechanismus: Nutzer koennen die App aktuell nutzen, ohne den AGB oder der Datenschutzerklaerung jemals aktiv zugestimmt zu haben.

Dies stellt ein **erhebliches rechtliches Risiko** dar:

1. **DSGVO Art. 6/7/13**: Ohne nachweisbare Einwilligung und ohne Datenschutzerklaerung vor Datenerhebung ist die Verarbeitung personenbezogener Daten rechtswidrig. Bussgelder bis 20 Mio. EUR oder 4% des Jahresumsatzes.
2. **BGB §305 ff. (AGB-Recht)**: AGB werden nur Vertragsbestandteil, wenn der Nutzer VOR Vertragsschluss auf sie hingewiesen wird UND ihnen aktiv zustimmt. Vorausgefuellte Haekchen sind unwirksam.
3. **DDG §5 (ehem. TMG)**: Impressumspflicht -- muss von jeder Seite/jedem Screen mit max. 2 Klicks erreichbar sein.
4. **App Store Review Guidelines 5.1.1**: Privacy Policy muss als oeffentlich erreichbare URL existieren.
5. **Google Play Policy**: Privacy Policy URL ist Pflichtfeld im Store Listing.

### Warum eine eigene Phase?

Die bestehende Phase 4 (Epic-07) deckt nur die **Texterstellung und Verlinkung** ab, nicht:
- Backend-Endpoints zum Ausliefern der Seiten
- Consent-Flow in der App (First-Launch, Versioning, Blocking)
- Consent-Tracking und -Speicherung (wann hat wer welcher Version zugestimmt?)
- Store-Compliance-Anforderungen

Diese Luecken sind **launch-blockierend** und benoetigen dedizierte Planung.

---

## EPIC-Uebersicht

| EPIC | Titel | Geschaetzter Aufwand | Abhaengigkeiten |
|------|-------|----------------------|-----------------|
| E01 | Legal Pages Backend | 1 Woche | Keine |
| E02 | Consent-Flow & Consent-Tracking | 1.5 Wochen | E01 (Backend-Endpoints fuer Consent-Storage) |
| E03 | Rechtliche Texte, AVV & VVT | 1 Woche + Anwalt | E01 (Hosting muss stehen) |
| E04 | Store Compliance | 0.5 Wochen | E01, E03 (URLs muessen live sein) |

---

## Abhaengigkeitsdiagramm

```
                    +------------------+
                    |   Phase 3/4      |
                    |  (App laeuft,    |
                    |  Settings exist) |
                    +--------+---------+
                             |
                    +--------v---------+
                    |      E01         |
                    | Legal Pages      |
                    | Backend          |
                    +--------+---------+
                             |
               +-------------+-------------+
               |                           |
      +--------v---------+       +--------v---------+
      |      E02         |       |      E03         |
      | Consent-Flow     |       | Rechtliche       |
      | & Tracking       |       | Texte            |
      +--------+---------+       +--------+---------+
               |                           |
               +-------------+-------------+
                             |
                    +--------v---------+
                    |      E04         |
                    | Store            |
                    | Compliance       |
                    +------------------+
```

---

## Kritischer Pfad

```
E01 (1 Wo) -> E02 + E03 parallel (1.5 Wo) -> E04 (0.5 Wo)
= 3 Wochen
```

E02 und E03 koennen **weitgehend parallel** laufen. E04 ist ein kurzer Abschluss-Check.

---

## Beziehung zu Phase 4

Phase 4 Epic-07 (Privacy Policy & Rechtliches) wird durch diese Phase **ersetzt und erweitert**. Die dort definierten Stories fuer Texterstellung und In-App-Verlinkung sind in E03 aufgegangen. Die dort fehlenden Themen (Backend, Consent, Store Compliance) sind in E01, E02 und E04 abgedeckt.

**Empfehlung**: Phase 4 Epic-07 als "Superseded by Phase 5" markieren.

---

## Rechtlicher Rahmen (Zusammenfassung)

| Gesetz/Regelung | Anforderung | Umsetzung in |
|-----------------|-------------|--------------|
| DSGVO Art. 13 | Information VOR Datenerhebung | E02 (First-Launch), E03 (Text) |
| DSGVO Art. 6/7 | Nachweisbare Einwilligung | E02 (Consent-Tracking) |
| BGB §305 ff. | Aktive Zustimmung zu AGB, kein Pre-Check | E02 (Consent-Flow) |
| DDG §5 | Impressum mit 2 Klicks erreichbar | E01 (Backend), E03 (Text) |
| App Store 5.1.1 | Oeffentliche Privacy Policy URL | E01 (Backend), E04 (Store) |
| Google Play Policy | Privacy Policy URL im Listing | E01 (Backend), E04 (Store) |

---

## Nicht-funktionale Anforderungen

- **Performance**: Legal Pages muessen in < 2s laden (statische Inhalte, Caching)
- **Verfuegbarkeit**: Legal Pages muessen OHNE Login erreichbar sein (oeffentlich)
- **Sprache**: Alle Texte in Deutsch (primaer) und Englisch
- **Barrierefreiheit**: Legal Pages muessen WCAG 2.1 AA erfuellen
- **Datenschutz**: Consent-Daten sind personenbezogen -- Loeschung bei Account-Loeschung beachten
- **Offline**: Consent-Status muss lokal gecached sein (App muss auch offline wissen, ob zugestimmt wurde)

---

## Review-Ergebnisse (Devils Advocate, 2026-04-01)

Die Stories wurden kritisch geprueft. Folgende Aenderungen wurden eingearbeitet:

1. **Rechtsgrundlagen-Trennung**: AGB = Vertragszustimmung (BGB §305), Datenschutz = Kenntnisnahme (Art. 6 Abs. 1 lit. b), KEIN Einwilligungs-Consent fuer Datenschutz. Finale Abgrenzung durch Anwalt.
2. **Consent-Widerruf entkoppelt von Konto-Loeschung**: Widerruf betrifft nur einwilligungsbasierte Verarbeitungen.
3. **Offline-Retry unbegrenzt**: Persistenter Sync mit User-Hinweis nach 7 Tagen.
4. **AVV mit Azure**: Neue Story E03-S06 (Auftragsverarbeitungsvertrag).
5. **VVT**: Neue Story E03-S07 (Verzeichnis der Verarbeitungstaetigkeiten).
6. **IP-Adresse in Datenschutzerklaerung**: Consent-Speicherung als eigener Abschnitt.
7. **Versionierung vereinfacht**: Einfache Nummern (v1, v2) + `requiresReConsent` Flag statt SemVer.
8. **Target Audience**: 16+ statt 18+ (konsistent mit AGB-Mindestalter).

---

## Dateien in diesem Ordner

| Datei | Inhalt |
|-------|--------|
| [epic-01-legal-pages-backend.md](epic-01-legal-pages-backend.md) | Backend-Endpoints fuer /privacy, /terms, /imprint |
| [epic-02-consent-flow.md](epic-02-consent-flow.md) | Consent-Mechanismus in der App |
| [epic-03-legal-texts.md](epic-03-legal-texts.md) | Struktur und Gliederung der rechtlichen Texte |
| [epic-04-store-compliance.md](epic-04-store-compliance.md) | App Store / Play Store Anforderungen |

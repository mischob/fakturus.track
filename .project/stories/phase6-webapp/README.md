# Phase 6: Web-App (Desktop/Tablet)

## Motivation

Die native Mobile-App deckt Smartphone-Nutzer ab. Viele Nutzer -- insbesondere Buero-Arbeiter, Freelancer am Schreibtisch und Arbeitgeber, die Reports erstellen -- benoetigen jedoch Zugang ueber den Desktop-Browser. Die Web-App unter `track.fakturus.com` bietet:

1. **Desktop-optimierte Zeiterfassung** mit breiteren Tabellen, Keyboard-Shortcuts und Side-Panels
2. **Einfachere Exports** (PDF/CSV/DATEV direkt im Browser, ohne App-Umweg)
3. **Stripe-basiertes Abo** (kein App-Store-Zwischenhandel, 30% weniger Gebuehren)
4. **Legal Pages** als echte Web-Seiten (SEO, Verlinkung, App-Store-Anforderung)
5. **Kein App-Download noetig** fuer gelegentliche Desktop-Nutzung

## Scope

Feature-Paritaet mit den nativen iOS/Android-Apps, PLUS:
- Desktop-optimierte Layouts (Sidebar-Navigation, Multi-Column, Side-Panels)
- Keyboard-Shortcuts
- DATEV-Export (nur Web)
- Stripe-Abo-Verwaltung (statt In-App-Purchase)
- Oeffentliche Legal Pages (/privacy, /terms, /imprint)

NICHT im Scope:
- Offline-Support (Web-App ist immer online)
- Push-Notifications (spaetere Phase)
- Mobile-Layout unter 768px (Verweis auf native Apps)

## Technologie

- **Blazor Server** (ASP.NET 10) -- das alte Blazor WASM Frontend wurde entfernt
- **Tailwind CSS** mit bestehender Design-System-Farbpalette
- **Azure AD B2C** (gleicher Tenant wie Mobile-Apps)
- **Stripe** fuer Subscriptions (Web-Abo muss kompatibel mit Mobile-Abos sein)
- **Heroicons** als Icon-Library

## Hosting

- Domain: `track.fakturus.com`
- Backend API: `api.track.fakturus.com` (bestehend)
- Server-to-Server API-Calls (kein CORS-Problem, bessere Security)

## EPIC-Uebersicht

| EPIC | Titel | Geschaetzter Aufwand | Abhaengigkeiten |
|------|-------|---------------------|-----------------|
| E01 | Projekt-Setup & Infrastruktur | 1 Woche | -- |
| E02 | Layout & Navigation | 1 Woche | E01 |
| E03 | Zeiterfassung (Timer + History) | 2 Wochen | E02 |
| E04 | Urlaub & Kalender | 1.5 Wochen | E02 |
| E05 | Reports & Export | 1.5 Wochen | E02 |
| E06 | Einstellungen & Account | 1 Woche | E02 |
| E07 | Legal Pages | 0.5 Wochen | E01 |
| E08 | Consent-Flow & Subscription | 1.5 Wochen | E02, E07 |

**Gesamt: ca. 10 Wochen** (mit Parallelisierung: ca. 7-8 Wochen)

## Abhaengigkeitsdiagramm

```
                    ┌─────────┐
                    │   E01   │
                    │ Setup   │
                    └────┬────┘
                         │
           ┌─────────────┼──────────────┐
           │             │              │
      ┌────▼────┐  ┌─────▼─────┐  ┌────▼────┐
      │   E02   │  │   E07     │  │ Backend │
      │ Layout  │  │  Legal    │  │ Stripe  │
      └────┬────┘  └─────┬─────┘  │ Endpts  │
           │             │        └────┬────┘
     ┌─────┼──────┬──────┤             │
     │     │      │      │             │
┌────▼┐ ┌──▼──┐ ┌─▼──┐ ┌▼─────┐  ┌───▼────┐
│ E03 │ │ E04 │ │E05 │ │ E06  │  │  E08   │
│Timer│ │Url. │ │Rep.│ │Einst.│  │Consent │
│Hist.│ │Kal. │ │Exp.│ │Acct. │  │Stripe  │
└─────┘ └─────┘ └────┘ └──────┘  └────────┘
```

## Abhaengigkeiten zum Backend

Das Backend (`api.track.fakturus.com`) stellt bereits alle benoetigten Endpoints bereit:
- WorkSessions CRUD + Sync
- VacationDays CRUD + Sync
- UserSettings CRUD
- OvertimeCalculation
- HolidayService
- SchoolHolidayPeriods CRUD
- Legal API (Versionen + Consent)

**Neu benoetigte Backend-Arbeit:**
- Stripe Webhook-Endpoints fuer Subscription-Management
- Stripe Customer Portal Integration
- Cross-Platform Abo-Synchronisation (Stripe <-> Apple/Google IAP)

## Priorisierung (MoSCoW)

**Must Have (MVP):**
- Projekt-Setup mit Auth (E01)
- Layout & Navigation (E02)
- Timer + History + Session-Edit (E03)
- Legal Pages (E07)
- Consent-Flow (E08 -- teilweise)

**Should Have:**
- Urlaub & Kalender (E04)
- Reports & Export (E05)
- Einstellungen (E06)

**Could Have:**
- DATEV-Export
- Keyboard-Shortcuts
- Shift+Klick Bereichsauswahl im Kalender
- Dark Mode Toggle

**Won't Have (this phase):**
- Offline-Support
- Push-Notifications
- Team/Multi-User Features
- Cross-Platform Abo-Sync (Stripe <-> Apple/Google) -- eigene Phase
- Stripe-Abo (Standalone-MVP zuerst, Abo in Phase 6b)

---

## Review-Ergebnisse (Devils Advocate, 2026-04-01)

Die Planung wurde kritisch geprueft. Folgende Aenderungen wurden eingearbeitet:

1. **🔴 Timer-Persistenz bei Circuit-Disconnect**: Aktive Session MUSS sofort ans Backend persistiert werden. Bei Disconnect/Browser-Neuladen wird die Session vom Backend wiederhergestellt. In E03-S01 ergaenzt.
2. **🔴 Scope reduziert auf MVP**: Phase 6a = E01-E03 + E07 + E08 (nur Consent). Stripe/Abo, Reports, Urlaub als Phase 6b. Realistisch: 5-6 Wochen.
3. **🔴 Consent-UI erst nach Anwalts-Klaerung**: Rechtsgrundlage (Einwilligung vs. Vertragsdurchfuehrung) muss geklaert sein bevor Consent-UI implementiert wird.
4. **🟡 Cross-Platform Abo als eigene Phase**: Web-App zeigt "Abo ueber Apple/Google aktiv" read-only. Kein Merge/Sync.
5. **🟡 MarkupString XSS**: Nur trusted Content, dokumentiert.
6. **🟡 Token Cache**: In-Memory fuer MVP akzeptabel, Re-Login nach App-Service-Restart dokumentiert.
- Mobile-Layout (< 768px)

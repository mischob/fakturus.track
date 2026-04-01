# Web-App Design -- Fakturus Track (Desktop/Tablet)

## Motivation

Die Web-App unter `track.fakturus.com` ersetzt das veraltete Blazor WASM Frontend. Sie ist optimiert fuer Desktop und Tablet (grosse Bildschirme). Fuer Mobile gibt es native iOS- und Android-Apps. Die Web-App bietet Feature-Paritaet mit den nativen Apps, nutzt aber den zusaetzlichen Platz fuer bessere Uebersicht und effizientere Workflows.

---

## Layout-Konzept

### Desktop (>= 1280px): Sidebar + Content

Die primaere Navigation erfolgt ueber eine fixierte Sidebar links. Der Content-Bereich nutzt die volle verbleibende Breite.

```
┌────────┬──────────────────────────────────────────────────────────┐
│        │  Header: Seitentitel + Aktionen                         │
│  LOGO  │─────────────────────────────────────────────────────────│
│        │                                                          │
│ ■ Dash │                                                          │
│ ■ Zeit │              Content Area                                │
│ ■ Url. │              (max-width: 1200px, zentriert)              │
│ ■ Rep. │                                                          │
│ ■ Einst│                                                          │
│        │                                                          │
│        │                                                          │
│        │                                                          │
│────────│                                                          │
│ User   │                                                          │
│ Logout │                                                          │
└────────┴──────────────────────────────────────────────────────────┘
```

**Sidebar (240px breit, fixiert):**
- Logo + App-Name oben
- 5 Navigationseintraege mit Icons + Labels
- User-Info + Logout unten
- Aktiver Eintrag: Primary-Hintergrund, weisser Text
- Hover: Leichter Hintergrund

**Content-Bereich:**
- Header mit Seitentitel und kontextuellen Aktionen
- Content zentriert mit max-width 1200px
- Padding: 32px (xl)

### Tablet (768px -- 1279px): Collapsible Sidebar

```
┌──────┬──────────────────────────────────────────┐
│      │  Header + Seitentitel                    │
│ Icon │──────────────────────────────────────────│
│ Icon │                                          │
│ Icon │           Content Area                   │
│ Icon │           (volle Breite)                 │
│ Icon │                                          │
│      │                                          │
│ User │                                          │
└──────┴──────────────────────────────────────────┘
```

- Sidebar auf 64px reduziert (nur Icons, Tooltip bei Hover)
- Hamburger-Button im Header zum Aufklappen
- Aufgeklappte Sidebar als Overlay (240px, mit Backdrop)
- Content nutzt volle Breite

### Legal Pages (oeffentlich, kein Login)

Legal Pages (`/privacy`, `/terms`, `/imprint`) haben ein eigenes, einfaches Layout:

```
┌──────────────────────────────────────────────────────────────────┐
│  [Fakturus Logo]              [Login]                            │
│──────────────────────────────────────────────────────────────────│
│                                                                   │
│                    Datenschutzerklaerung                          │
│                                                                   │
│     Content (max-width: 800px, zentriert)                        │
│     Typografie-optimiert fuer Lesbarkeit                         │
│     (groessere Schrift, mehr Zeilenabstand)                      │
│                                                                   │
│──────────────────────────────────────────────────────────────────│
│  Footer: Links zu /privacy, /terms, /imprint  |  (c) fakturus   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Responsive Breakpoints

| Breakpoint | Breite | Sidebar | Content max-width |
|------------|--------|---------|-------------------|
| Desktop XL | >= 1536px | 240px expanded | 1200px |
| Desktop | >= 1280px | 240px expanded | 1000px |
| Tablet | 768px -- 1279px | 64px collapsed | 100% (Padding 24px) |
| Nicht unterstuetzt | < 768px | -- | Redirect zu App Store / Play Store |

Hinweis: Unter 768px wird ein Hinweis angezeigt: "Fuer die beste Erfahrung auf dem Smartphone nutzen Sie unsere App" mit Links zu App Store / Play Store.

---

## Farbschema (aus Design System uebernommen)

Die Web-App nutzt das gleiche Farbschema wie die nativen Apps:

- **Primary:** #1A5CFF (Fakturus Blau)
- **Success:** #1DB954 (Positive Werte, Timer)
- **Danger:** #E5383B (Negative Werte, Fehler)
- **Warning:** #F59E0B (Warnungen)
- **Vacation:** #06B6D4 (Urlaub)
- **Holiday:** #8B5CF6 (Feiertage)
- **Sick:** #EF4444 (Krankheitstage)
- **Pause:** #8B5CF6 (Pause-Indikator)

Dark Mode wird unterstuetzt (System-Praeferenz + Toggle in Einstellungen).

### Web-spezifische Font

**Inter** als primaere Schrift (bereits im bestehenden Tailwind-Config). Monospaced Zahlen via `font-variant-numeric: tabular-nums`.

---

## Seiten-Wireframes

### 1. Dashboard / Home

Das Dashboard ist die Startseite nach dem Login. Es kombiniert den Timer (haeufigste Aktion) mit einer Tagesuebersicht.

```
┌────────┬──────────────────────────────────────────────────────────┐
│        │  Dashboard                                  31.03.2026  │
│  LOGO  │─────────────────────────────────────────────────────────│
│        │                                                          │
│ ■ Dash │  ┌──────────────────────┐  ┌──────────────────────────┐ │
│   Zeit │  │  Timer               │  │  Heute                   │ │
│   Url. │  │                      │  │                          │ │
│   Rep. │  │     03:42:18         │  │  Start:    08:30         │ │
│   Einst│  │  ● Laufende Sitzung  │  │  Pause:    30 min       │ │
│        │  │                      │  │  Netto:    3:12h         │ │
│        │  │  Start: 08:30        │  │  Soll:     8:00h        │ │
│        │  │  Pause: 30 min       │  │  Fehlend:  4:48h        │ │
│        │  │                      │  │                          │ │
│        │  │ [Pause] [Stop] [OK]  │  │  ArbZG: Pause nach 6h   │ │
│        │  └──────────────────────┘  └──────────────────────────┘ │
│        │                                                          │
│        │  ┌──────────────────────┐  ┌──────────────────────────┐ │
│        │  │  Ueberstunden        │  │  Urlaub                  │ │
│        │  │  +12:30h             │  │  25 von 30 Tagen         │ │
│        │  │  Maerz: +2:15h      │  │  ████████████████░░░░    │ │
│        │  └──────────────────────┘  └──────────────────────────┘ │
│        │                                                          │
│────────│  Letzte Eintraege                     [Alle anzeigen >] │
│ Max M. │  ┌──────────────────────────────────────────────────────┐│
│ Logout │  │ Mo 30.03.  08:30 - 17:00  P30   8:00h    [Edit]    ││
│        │  │ Fr 28.03.  09:00 - 17:30  P30   8:00h    [Edit]    ││
│        │  │ Do 27.03.  08:00 - 16:15  P30   7:45h    [Edit]    ││
│        │  └──────────────────────────────────────────────────────┘│
└────────┴──────────────────────────────────────────────────────────┘
```

**Desktop-Vorteile:**
- Timer und Tages-Stats nebeneinander (2-Column Grid)
- Ueberstunden + Urlaub als kompakte Summary Cards
- Letzte Eintraege direkt sichtbar (kein Tab-Wechsel noetig)
- ArbZG-Warnbanner integriert in Tages-Stats

### 2. Zeiten (History)

Breitere Tabelle mit mehr Informationen pro Zeile. Monatsnavigation ueber Sidebar oder Tabs.

```
┌────────┬──────────────────────────────────────────────────────────┐
│        │  Zeiten                              [+ Nacherfassung]  │
│  LOGO  │─────────────────────────────────────────────────────────│
│        │                                                          │
│   Dash │  ┌──────────────────────────────────────────────────────┐│
│ ■ Zeit │  │  Maerz 2026              12 Eintraege    98:45h     ││
│   Url. │  ├──────────────────────────────────────────────────────┤│
│   Rep. │  │  Datum       Start  Ende   Pause  Netto   Status    ││
│   Einst│  │──────────────────────────────────────────────────────││
│        │  │  Mo 30.03.   08:30  17:00  30min  8:00h   [Sync]   ││
│        │  │  Fr 28.03.   09:00  17:30  30min  8:00h   [Sync]   ││
│        │  │  Do 27.03.   08:00  16:15  30min  7:45h   [Sync]   ││
│        │  │  Mi 26.03.   08:30  17:00  30min  8:00h   [Sync]   ││
│        │  │  Di 25.03.   09:00  18:00  45min  8:15h   [Sync]   ││
│        │  │  Mo 24.03.   08:00  16:30  30min  8:00h   [Sync]   ││
│        │  │  ...                                                 ││
│        │  ├──────────────────────────────────────────────────────┤│
│        │  │  Monats-Summe            12x      98:45h            ││
│        │  │  Soll: 176:00h  |  Differenz: -77:15h (laufend)    ││
│        │  └──────────────────────────────────────────────────────┘│
│        │                                                          │
│────────│  ┌──────────────────────────────────────────────────────┐│
│ Max M. │  │  Februar 2026            20 Eintraege    162:30h    ││
│ Logout │  │  [Aufklappen]                                        ││
│        │  └──────────────────────────────────────────────────────┘│
│        │                                                          │
│        │  ┌──────────────────────────────────────────────────────┐│
│        │  │  Januar 2026             22 Eintraege    178:15h    ││
│        │  │  [Aufklappen]                                        ││
│        │  └──────────────────────────────────────────────────────┘│
└────────┴──────────────────────────────────────────────────────────┘
```

**Desktop-Vorteile:**
- Volle Tabelle mit allen Spalten (kein Scrollen)
- Inline-Edit: Klick auf Zeile oeffnet Inline-Formular oder Side-Panel
- Nacherfassung direkt via Button im Header
- Monats-Zusammenfassung am Tabellenende

**Session-Edit (Side-Panel statt Modal):**

```
┌────────┬──────────────────────────────────┬──────────────────────┐
│        │  Zeiten            [+ Nacherf.]  │  Session bearbeiten  │
│  LOGO  │──────────────────────────────────│                      │
│        │                                  │  Datum               │
│   Dash │  [Tabelle wie oben,             │  [30.03.2026     v]  │
│ ■ Zeit │   mit markierter Zeile]         │                      │
│   Url. │                                  │  Startzeit           │
│   Rep. │                                  │  [08:30          v]  │
│   Einst│                                  │                      │
│        │                                  │  Endzeit             │
│        │                                  │  [17:00          v]  │
│        │                                  │                      │
│        │                                  │  Pause (Minuten)     │
│        │                                  │  [30              ]  │
│        │                                  │                      │
│        │                                  │  Brutto: 8:30h       │
│        │                                  │  Netto:  8:00h       │
│        │                                  │                      │
│────────│                                  │ [Speichern][Loeschen]│
│ Max M. │                                  │ [Abbrechen]          │
└────────┴──────────────────────────────────┴──────────────────────┘
```

### 3. Urlaub

Groesserer Kalender mit Side-Panel fuer Details und Aktionen.

```
┌────────┬──────────────────────────────────────────────────────────┐
│        │  Urlaub                                                  │
│  LOGO  │─────────────────────────────────────────────────────────│
│        │                                                          │
│   Dash │  ┌────────────────┐  ┌──────────────────────────────────┐│
│   Zeit │  │  Resturlaub    │  │  Krankheitstage                  ││
│ ■ Url. │  │  25 / 30 Tage  │  │  3 Tage in 2026                 ││
│   Rep. │  │  ████████░░    │  │                                  ││
│   Einst│  └────────────────┘  └──────────────────────────────────┘│
│        │                                                          │
│        │  ┌──────────────────────────────────────────────────────┐│
│        │  │               << Maerz 2026 >>                       ││
│        │  │                                                      ││
│        │  │  Mo    Di    Mi    Do    Fr    Sa    So              ││
│        │  │                                1     2     3        ││
│        │  │   4     5     6     7     8     9    10              ││
│        │  │  11    12    13    14    15    16    17              ││
│        │  │  18    19    20    21    22    23    24              ││
│        │  │  25    26    27   [28]   29    30    31              ││
│        │  │                                                      ││
│        │  │  Modus: [Urlaub] [Krankheit]                        ││
│        │  │                                                      ││
│        │  │  Legende:                                            ││
│        │  │  ● Urlaub (cyan)  ● Feiertag (lila)                ││
│        │  │  ● Krank (rot)    ● Schulferien (orange)            ││
│────────│  └──────────────────────────────────────────────────────┘│
│ Max M. │                                                          │
│ Logout │  Kommende Feiertage              Schulferien 2026       │
│        │  ┌────────────────────────┐  ┌──────────────────────────┐│
│        │  │ 01.05. Tag der Arbeit  │  │ 06.04.-18.04. Ostern    ││
│        │  │ 29.05. Chr. Himmelfahrt│  │ 06.07.-18.08. Sommer    ││
│        │  │ 09.06. Pfingstmontag   │  │ 12.10.-24.10. Herbst    ││
│        │  └────────────────────────┘  └──────────────────────────┘│
└────────┴──────────────────────────────────────────────────────────┘
```

**Desktop-Vorteile:**
- Groesserer Kalender mit mehr Platz pro Tag (Feiertagsname sichtbar)
- Modus-Wechsel via Toggle-Buttons (statt Long-Press auf Mobile)
- Feiertage und Schulferien nebeneinander als Karten
- Resturlaub + Krankheitstage als Summary Cards oben

**Interaktionslogik (Web-spezifisch):**
- Klick auf Arbeitstag: Setzt je nach aktivem Modus (Urlaub/Krankheit)
- Erneuter Klick: Entfernt Markierung
- Modus-Toggle: Explizite Buttons statt Long-Press (Desktop hat keine Long-Press Konvention)
- Shift+Klick: Bereich markieren (erster Klick = Start, Shift+Klick = Ende, alle Arbeitstage dazwischen markieren)

### 4. Reports / Gesamt

Charts und Export-Optionen, Desktop-optimiert mit mehr Datendichte.

```
┌────────┬──────────────────────────────────────────────────────────┐
│        │  Reports                                     2026   v   │
│  LOGO  │─────────────────────────────────────────────────────────│
│        │                                                          │
│   Dash │  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌──────┐ │
│   Zeit │  │Ueberstund.│  │  Urlaub   │  │ Feiertage │  │Krank │ │
│   Url. │  │  +12:30h  │  │  5/30     │  │    11     │  │  3   │ │
│ ■ Rep. │  │  (gruen)  │  │  25 uebr. │  │  in 2026  │  │ Tage │ │
│   Einst│  └───────────┘  └───────────┘  └───────────┘  └──────┘ │
│        │                                                          │
│        │  Monatliche Uebersicht                                  │
│        │  ┌──────────────────────────────────────────────────────┐│
│        │  │ Monat   Arbeitstage  Gearbeitet  Erwartet    +/-    ││
│        │  │──────────────────────────────────────────────────────││
│        │  │ Jan     22           171:15h     176:00h    -4:45h  ││
│        │  │ Feb     20           158:30h     160:00h    -1:30h  ││
│        │  │ Maer    11 (lfd.)    98:45h      176:00h    ----    ││
│        │  │ Apr     --           --          --         --      ││
│        │  │ ...     ...          ...         ...        ...     ││
│        │  │ Dez     --           --          --         --      ││
│        │  │──────────────────────────────────────────────────────││
│        │  │ Gesamt  53           428:30h     512:00h   +12:30h  ││
│        │  └──────────────────────────────────────────────────────┘│
│        │                                                          │
│────────│  Export                                                  │
│ Max M. │  ┌──────────────────────────────────────────────────────┐│
│ Logout │  │                                                      ││
│        │  │  Zeitraum: [Maerz 2026  v]                          ││
│        │  │                                                      ││
│        │  │  [PDF Monatsreport]  [CSV Monat]  [CSV Quartal]     ││
│        │  │  [CSV Jahr]          [DATEV Export]                  ││
│        │  │                                                      ││
│        │  └──────────────────────────────────────────────────────┘│
└────────┴──────────────────────────────────────────────────────────┘
```

**Desktop-Vorteile:**
- 4 Summary Cards nebeneinander (statt horizontal scrollbar)
- Volle 12-Monats-Tabelle sichtbar ohne Scrollen
- Export-Bereich mit allen Optionen auf einen Blick
- DATEV-Export nur in Web (nicht in Mobile-App)

### 5. Einstellungen

Multi-Column Layout fuer Desktop, gruppiert in logische Bereiche.

```
┌────────┬──────────────────────────────────────────────────────────┐
│        │  Einstellungen                                           │
│  LOGO  │─────────────────────────────────────────────────────────│
│        │                                                          │
│   Dash │  ┌──────────────────────────────────────────────────────┐│
│   Zeit │  │  Profil                                              ││
│   Url. │  │  [Avatar] Max Mustermann                             ││
│   Rep. │  │           max@beispiel.de                            ││
│ ■ Einst│  │  Personalnummer: [12345           ]                  ││
│        │  │  [Konto loeschen]                    [Abmelden]      ││
│        │  └──────────────────────────────────────────────────────┘│
│        │                                                          │
│        │  ┌───────────────────────┐  ┌───────────────────────────┐│
│        │  │  Arbeitszeit          │  │  Standort & Urlaub        ││
│        │  │                       │  │                           ││
│        │  │  Stunden/Woche        │  │  Bundesland               ││
│        │  │  [40.0          ]     │  │  [Nordrhein-Westfalen v]  ││
│        │  │                       │  │  11 Feiertage in 2026     ││
│        │  │  Arbeitstage          │  │                           ││
│        │  │  [Mo][Di][Mi][Do][Fr] │  │  Urlaubstage/Jahr         ││
│        │  │   Sa  So              │  │  [30                  ]   ││
│        │  │                       │  │                           ││
│        │  └───────────────────────┘  └───────────────────────────┘│
│        │                                                          │
│        │  ┌───────────────────────┐  ┌───────────────────────────┐│
│        │  │  Kalender             │  │  Abonnement               ││
│        │  │                       │  │                           ││
│        │  │  Schulferien:         │  │  Aktuell: PRO             ││
│────────│  │  [Verwalten >]        │  │  4.99 EUR/Monat           ││
│ Max M. │  │                       │  │                           ││
│ Logout │  │  iCal-URL:            │  │  [Abo verwalten]          ││
│        │  │  [URL eingeben    ]   │  │  (Stripe Customer Portal) ││
│        │  └───────────────────────┘  └───────────────────────────┘│
│        │                                                          │
│        │  ┌──────────────────────────────────────────────────────┐│
│        │  │  Erscheinungsbild: [System] [Hell] [Dunkel]          ││
│        │  │  Datenschutz | AGB | Impressum          v1.0.0       ││
│        │  └──────────────────────────────────────────────────────┘│
└────────┴──────────────────────────────────────────────────────────┘
```

**Desktop-Vorteile:**
- 2-Column Grid fuer Einstellungs-Karten
- Alle Einstellungen auf einen Blick (kein Scrollen noetig)
- Stripe-Abo-Verwaltung (statt App Store)
- Links zu Legal Pages im Footer

### 6. Legal Pages (/privacy, /terms, /imprint)

Oeffentlich erreichbare Seiten ohne Login. Einfaches, lesbares Layout.

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                   │
│  [Fakturus Logo]  fakturus.track           [Anmelden] [Starten] │
│                                                                   │
│──────────────────────────────────────────────────────────────────│
│                                                                   │
│                    Datenschutzerklaerung                          │
│                    Stand: 01.03.2026                              │
│                                                                   │
│     ─────────────────────────────────────────                    │
│                                                                   │
│     1. Verantwortlicher                                          │
│                                                                   │
│     Verantwortlich fuer die Datenverarbeitung ist...             │
│                                                                   │
│     2. Erhobene Daten                                            │
│                                                                   │
│     Bei der Nutzung von fakturus.track werden                    │
│     folgende personenbezogene Daten erhoben:                     │
│     ...                                                          │
│                                                                   │
│     [max-width: 800px, zentriert]                                │
│     [font-size: 18px, line-height: 1.8]                          │
│                                                                   │
│──────────────────────────────────────────────────────────────────│
│  Datenschutz  |  AGB  |  Impressum      (c) 2026 fakturus GmbH  │
└──────────────────────────────────────────────────────────────────┘
```

**Design-Entscheidungen:**
- Eigenes Header-Layout (kein Sidebar-Navigation, da oeffentlich)
- Maximale Lesefreundlichkeit: grosse Schrift, viel Zeilenabstand
- "Anmelden" und "Starten"-Buttons im Header fuer Conversion
- Footer mit Querlinks zu den anderen Legal Pages
- HTML-Inhalt wird aus bestehenden Dateien geladen (Backend: `/legal/*.html`)

### 7. Consent-Flow

Wird nach dem Login angezeigt, wenn der User noch keine Zustimmung gegeben hat.

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                   │
│              [Fakturus Logo]                                     │
│                                                                   │
│              Willkommen bei fakturus.track                       │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │                                                          │    │
│  │  Bevor Sie starten, benoetigen wir Ihre Zustimmung:     │    │
│  │                                                          │    │
│  │  [ ] Ich stimme den AGB zu.                              │    │
│  │      [AGB lesen >]                                       │    │
│  │                                                          │    │
│  │  [ ] Ich habe die Datenschutzerklaerung                  │    │
│  │      zur Kenntnis genommen.                              │    │
│  │      [Datenschutzerklaerung lesen >]                     │    │
│  │                                                          │    │
│  │  [          Weiter          ]  (disabled bis beide OK)   │    │
│  │                                                          │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 8. Login-Seite

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                   │
│                                                                   │
│                   [Fakturus Logo]                                │
│                                                                   │
│                   fakturus.track                                 │
│              Arbeitszeit erfassen.                                │
│              Einfach. Ueberall.                                  │
│                                                                   │
│              ┌──────────────────────────┐                        │
│              │  Mit Google anmelden     │                        │
│              └──────────────────────────┘                        │
│              ┌──────────────────────────┐                        │
│              │  Mit E-Mail anmelden     │                        │
│              └──────────────────────────┘                        │
│                                                                   │
│              Datenschutz | AGB | Impressum                       │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

Hinweis: Kein "Apple Sign-In" auf der Web-App (nur in den nativen Apps). Stattdessen Google + E-Mail ueber Azure B2C.

---

## Interaktionspatterns (Web-spezifisch)

### Keyboard Shortcuts

| Shortcut | Aktion |
|----------|--------|
| `Space` | Timer starten/stoppen (wenn Dashboard aktiv) |
| `Ctrl+N` | Neue Nacherfassung |
| `Escape` | Panel/Modal schliessen |
| `Ctrl+E` | Markierte Session bearbeiten |

### Inline-Edit vs. Panel

- **Zeiten:** Klick auf Zeile oeffnet Side-Panel (rechts, 400px)
- **Urlaub:** Klick auf Tag setzt/entfernt direkt (kein Panel)
- **Einstellungen:** Direkt editierbar (Auto-Save mit Debounce 500ms)

### Notifications / Toasts

Toasts erscheinen oben rechts:
- Erfolg: "Session gespeichert" (gruen, 3s)
- Fehler: "Speichern fehlgeschlagen" (rot, persistent bis dismiss)
- ArbZG-Warnung: "Pause nach 6h empfohlen" (gelb, dismiss-Button)

---

## Dark Mode

Die Web-App respektiert `prefers-color-scheme` und bietet zusaetzlich einen manuellen Toggle in den Einstellungen (System/Hell/Dunkel).

Dark Mode Farben (aus Design System):
- Background: #0F1117
- Cards: #1A1D27
- Borders: #2D3040
- Text: #F0F1F3
- Primary: #4D8AFF (heller fuer Lesbarkeit)

---

## Technische Rahmenbedingungen

- **Framework:** Blazor Server (ASP.NET 8)
- **Hosting:** `track.fakturus.com` (Azure App Service)
- **Auth:** Azure AD B2C (MSAL.js / Blazor Auth Integration)
- **Styling:** Tailwind CSS (Build-Pipeline via PostCSS)
- **Icons:** Heroicons (SVG, passt zu Tailwind-Oekosystem)
- **State Management:** Blazor Server-seitig (kein LocalStorage noetig, Server haelt State)
- **API-Kommunikation:** Direkt via HttpClient zum Backend (Server-to-Server, kein CORS-Problem)
- **Kein Offline-Support:** Web-App ist immer online (Offline-Support nur in nativen Apps)

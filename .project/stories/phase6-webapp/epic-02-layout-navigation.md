# EPIC 02: Layout & Navigation

## Ziel

Die grundlegende App-Shell mit Sidebar-Navigation, responsivem Layout, Theme-Support und allen Routing-Strukturen. Am Ende dieser Epic hat die App ein vollstaendiges, navigierbares Geruest.

---

## Stories

### S01: Sidebar-Navigation (Desktop)
**Als** Benutzer **moechte ich** eine Sidebar-Navigation am linken Bildschirmrand, **damit** ich schnell zwischen den Bereichen der App wechseln kann.

**Akzeptanzkriterien:**
- [ ] Fixierte Sidebar (240px breit) am linken Rand
- [ ] Logo + "fakturus.track" oben
- [ ] 5 Navigationseintraege mit Heroicons:
  - Dashboard (Home)
  - Zeiten (Clock)
  - Urlaub (Sun)
  - Reports (ChartBar)
  - Einstellungen (Cog)
- [ ] Aktiver Eintrag visuell hervorgehoben (Primary-Hintergrund)
- [ ] Hover-Effekt auf inaktiven Eintraegen
- [ ] User-Info unten (Name, E-Mail, Abmelden-Link)
- [ ] Sidebar hat eigenen Scroll bei zu vielen Eintraegen

**Aufwand:** M

---

### S02: Responsive Sidebar (Tablet)
**Als** Tablet-Benutzer **moechte ich** eine kompakte Sidebar, **damit** der Content mehr Platz hat.

**Akzeptanzkriterien:**
- [ ] Unter 1280px: Sidebar kollabiert auf 64px (nur Icons)
- [ ] Tooltip bei Hover ueber kollabierte Icons (Seitenname)
- [ ] Hamburger-Button im Header zum Aufklappen
- [ ] Aufgeklappte Sidebar als Overlay (240px, mit dunklem Backdrop)
- [ ] Klick auf Backdrop oder Navigation schliesst Overlay
- [ ] Transition/Animation beim Auf-/Zuklappen (300ms)

**Aufwand:** M

---

### S03: Content-Layout & Header
**Als** Benutzer **moechte ich** einen strukturierten Content-Bereich mit Seitentitel und Aktionen, **damit** ich immer weiss wo ich bin und was ich tun kann.

**Akzeptanzkriterien:**
- [ ] Content-Bereich rechts neben der Sidebar
- [ ] Header-Zeile mit:
  - Seitentitel (links)
  - Kontextuelle Aktions-Buttons (rechts, z.B. "+ Nacherfassung")
  - Datum (optional, rechts)
- [ ] Content max-width: 1200px (Desktop XL) / 1000px (Desktop) / 100% (Tablet)
- [ ] Content zentriert mit symmetrischem Padding
- [ ] Scrollbar nur im Content-Bereich (Sidebar bleibt fixiert)

**Aufwand:** S

---

### S04: Routing-Struktur
**Als** Entwickler **moechte ich** alle Routen definiert haben, **damit** die Navigation funktioniert.

**Akzeptanzkriterien:**
- [ ] Authentifizierte Routen:
  - `/` oder `/dashboard` -> Dashboard
  - `/time` -> Zeiten
  - `/vacation` -> Urlaub
  - `/reports` -> Reports
  - `/settings` -> Einstellungen
- [ ] Oeffentliche Routen (ohne Auth):
  - `/privacy` -> Datenschutzerklaerung
  - `/terms` -> AGB
  - `/imprint` -> Impressum
  - `/login` -> Login-Seite
- [ ] 404-Seite fuer unbekannte Routen
- [ ] Redirect: nicht-authentifiziert -> `/login`
- [ ] Redirect: authentifiziert + `/login` -> `/dashboard`
- [ ] Deep-Links funktionieren (z.B. `/reports` direkt aufrufbar)

**Aufwand:** S

---

### S05: Dark Mode / Theme-System
**Als** Benutzer **moechte ich** zwischen hellem und dunklem Design wechseln, **damit** ich die App meinen Praeferenzen anpassen kann.

**Akzeptanzkriterien:**
- [ ] Drei Modi: System (Standard), Hell, Dunkel
- [ ] System-Modus respektiert `prefers-color-scheme`
- [ ] Manuelle Auswahl wird im User-Setting gespeichert
- [ ] Tailwind `dark:` Prefix funktioniert korrekt
- [ ] Alle Farben aus dem Design-System in Dark Mode angepasst:
  - Background: #0F1117
  - Cards: #1A1D27
  - Borders: #2D3040
  - Text: #F0F1F3
  - Primary: #4D8AFF
- [ ] Theme-Wechsel ohne Page-Reload
- [ ] Sidebar, Content und alle Komponenten reagieren auf Theme

**Aufwand:** M

---

### S06: Toast/Notification System
**Als** Benutzer **moechte ich** Feedback zu meinen Aktionen erhalten, **damit** ich weiss ob etwas geklappt hat oder fehlgeschlagen ist.

**Akzeptanzkriterien:**
- [ ] Toast-Komponente oben rechts im Bildschirm
- [ ] Typen: Erfolg (gruen), Fehler (rot), Warnung (gelb), Info (blau)
- [ ] Erfolg-Toasts verschwinden nach 3 Sekunden
- [ ] Fehler-Toasts bleiben bis zum manuellen Schliessen
- [ ] Mehrere Toasts stacken sich vertikal
- [ ] Slide-In Animation von rechts
- [ ] Blazor Service: `IToastService.Show(message, type)`

**Aufwand:** S

---

### S07: Unter-768px-Hinweis
**Als** Benutzer auf einem kleinen Bildschirm **moechte ich** auf die native App hingewiesen werden, **damit** ich die beste Erfahrung erhalte.

**Akzeptanzkriterien:**
- [ ] Unter 768px Viewport-Breite: Fullscreen-Hinweis
- [ ] Text: "Fuer die beste Erfahrung auf dem Smartphone nutzen Sie unsere App"
- [ ] Links zu App Store und Play Store
- [ ] Hinweis kann dismisst werden (Cookie: 7 Tage)
- [ ] Nach Dismiss: Web-App wird angezeigt (kein Zwang)

**Aufwand:** S

---

## Zusammenfassung

| Story | Aufwand | Abhaengigkeit |
|-------|---------|---------------|
| S01 Sidebar Desktop | M | E01 |
| S02 Sidebar Tablet | M | S01 |
| S03 Content-Layout | S | S01 |
| S04 Routing | S | E01 |
| S05 Dark Mode | M | S01 |
| S06 Toast System | S | S01 |
| S07 Mobile-Hinweis | S | S01 |

**Gesamt: ca. 1 Woche** (S01+S04 parallel, dann S02+S03+S05+S06+S07 parallel)

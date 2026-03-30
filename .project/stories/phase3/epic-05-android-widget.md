# EPIC 05: Android Widget (Glance)

## Ziel

Home Screen Widget fuer Android, das den aktuellen Timer-Status und die heutige Arbeitszeit anzeigt. Quick Actions ermoeglichen Timer-Start/Stop direkt vom Widget. Implementierung mit Jetpack Glance (Compose-basierte Widgets).

## Abhaengigkeiten

- **Phase 2 abgeschlossen**: WorkSession-Daten und Timer-Logik muessen stehen
- **Keine Backend-Aenderungen**: Widget liest nur lokale Daten

---

## Stories

### P3-E05-S01: Android Widget Setup (Glance)

**Als** Entwickler
**moechte ich** ein Android Home Screen Widget mit Jetpack Glance einrichten,
**damit** ich Compose-basierte Widgets entwickeln kann.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 Android-Projekt
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E03-*, P3-E04-*, P3-E07-*
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Jetpack Glance Dependency hinzugefuegt (`androidx.glance:glance-appwidget`)
- [ ] `TimerWidgetReceiver` in `widget/` Package
- [ ] `timer_widget_info.xml` in `res/xml/` mit Widget-Metadata (min. 2x2 / 4x2)
- [ ] Widget in `AndroidManifest.xml` registriert
- [ ] Placeholder-Widget erscheint im Widget-Picker
- [ ] DataStore oder SharedPreferences fuer Widget-State definiert

**Technische Hinweise**:
- Glance 1.1+ (Compose-basierte Widget-API)
- `GlanceAppWidgetReceiver` als Basis
- `glance-appwidget` fuer Home Screen Widgets
- `updateAll()` aufrufen wenn Timer-State sich aendert

---

### P3-E05-S02: Android Timer-Status Widget

**Als** Nutzer
**moechte ich** meinen aktuellen Timer-Status auf dem Home Screen sehen,
**damit** ich mit einem Blick weiss ob mein Timer laeuft.

**Plattform**: Android
**Abhaengigkeiten**: P3-E05-S01 (Widget Setup)
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E03-*, P3-E04-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Small Widget** (2x2):
  - Idle: "Bereit" + Icon
  - Running: Timer "03:42" + gruener Indikator
  - Paused: "Pausiert" + Pausendauer
  - Tap oeffnet die App
- [ ] **Medium Widget** (4x2):
  - Alles vom Small Widget PLUS:
  - Heutige Gesamtarbeitszeit
  - Startzeit der aktuellen Session
- [ ] Widget aktualisiert sich bei Timer-State-Aenderungen
- [ ] Material 3 Design: DynamicColors, Corner Radius
- [ ] Dark Mode: Widget passt sich automatisch an
- [ ] Given der Timer laeuft seit 08:30
  When der Nutzer auf den Home Screen schaut
  Then zeigt das Widget den laufenden Timer

**Technische Hinweise**:
- Glance Composables: `GlanceModifier`, `Column`, `Row`, `Text`, `Image`
- Timer-Update: WorkManager Periodic Task (min. 15 Min) + BroadcastReceiver fuer sofortige Updates
- `GlanceAppWidget.update(context, glanceId)` fuer einzelnes Widget-Update
- Material 3: `GlanceTheme` mit dynamischen Farben

---

### P3-E05-S03: Android Widget Quick Actions

**Als** Nutzer
**moechte ich** meinen Timer direkt vom Widget aus starten oder stoppen koennen,
**damit** ich die App nicht erst oeffnen muss.

**Plattform**: Android
**Abhaengigkeiten**: P3-E05-S02 (Widget UI)
**Parallelisierbar mit**: P3-E01-*, P3-E02-*, P3-E03-*, P3-E04-*
**Geschaetzter Aufwand**: M

**Akzeptanzkriterien**:
- [ ] **Start-Button**: Erstellt neue Session und startet Timer (Medium Widget)
- [ ] **Stop-Button**: Stoppt den laufenden Timer
- [ ] **Pause/Weiter-Button**: Pausiert oder setzt Timer fort
- [ ] Aktionen werden via `ActionCallback` an die App weitergegeben
- [ ] Widget aktualisiert sich sofort nach Aktion
- [ ] Given kein Timer laeuft
  When der Nutzer im Medium Widget "Start" tippt
  Then startet der Timer
  And das Widget aktualisiert sich

**Technische Hinweise**:
- Glance `ActionCallback` fuer Button-Aktionen im Widget
- `actionRunCallback<StartTimerAction>()` als onClick
- Action schreibt in SharedPreferences/DataStore und triggert Widget-Update
- `BroadcastReceiver` in der Haupt-App empfaengt Widget-Aktionen
- Oder: `WorkManager.enqueue(OneTimeWorkRequest)` fuer zuverlaessige Ausfuehrung

---

### P3-E05-S04: Android App Shortcut

**Als** Nutzer
**moechte ich** per Long-Press auf das App-Icon schnell einen Timer starten koennen,
**damit** ich noch schneller Zeiten erfassen kann.

**Plattform**: Android
**Abhaengigkeiten**: Phase 2 Android-Projekt
**Parallelisierbar mit**: Alle anderen Stories
**Geschaetzter Aufwand**: S

**Akzeptanzkriterien**:
- [ ] Long-Press auf App-Icon zeigt Shortcuts:
  - "Timer starten" (startet direkt den Timer)
  - "Letzten Eintrag sehen" (oeffnet History)
- [ ] `shortcuts.xml` in `res/xml/` definiert
- [ ] Shortcuts in `AndroidManifest.xml` registriert
- [ ] Given der Nutzer drückt lange auf das App-Icon
  When die Shortcuts erscheinen
  Then kann er "Timer starten" waehlen und der Timer startet

**Technische Hinweise**:
- Static Shortcuts via `<shortcuts>` in `res/xml/shortcuts.xml`
- `<shortcut android:shortcutId="start_timer" ...>`
- Deep Link zur Timer-Aktion: `Intent` mit Extra-Parameter
- `ShortcutManagerCompat` fuer dynamische Shortcuts (optional)

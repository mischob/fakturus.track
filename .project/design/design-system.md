# Design System -- Fakturus Track

## Markenpositionierung (aus Marktanalyse)

> **Tagline:** "Arbeitszeit erfassen. Einfach. Ueberall."
>
> **Value Proposition:** fakturus.track ist die Zeiterfassung, die funktioniert -- auch ohne
> Internet. Starten, stoppen, fertig. Mit automatischer Ueberstundenberechnung und allen
> deutschen Feiertagen.
>
> **Kern-Differenzierung:**
> 1. Offline-First (funktioniert ohne Internet -- Baustelle, unterwegs)
> 2. Sofort startklar (keine Konfiguration, kein Onboarding-Call)
> 3. Deutschland-optimiert (Bundesland-Feiertage, ArbZG-konform)
> 4. Native Mobile (echte App, schnell, zuverlaessig)
>
> **Primaere Zielgruppe:** Freelancer, Selbststaendige, Kleinstunternehmen (1-10 MA),
> Handwerker und mobile Arbeitnehmer, Eltern in Teilzeit
>
> Diese Positionierung muss sich im Design widerspiegeln: **einfach, schnell, vertrauenswuerdig, deutsch.**

---

## Design-Philosophie

Fakturus Track ist ein **Werkzeug**, kein Social Network. Das Design muss:

1. **Effizient** sein: Minimale Taps zum Ziel (Start/Stop in 1 Tap)
2. **Klar** sein: Sofort erkennbar, was gerade passiert (laufender Timer, Sync-Status)
3. **Ruhig** sein: Keine Ablenkungen, kein visuelles Rauschen
4. **Vertrauenswuerdig** wirken: Professionell, zuverlaessig, deutsch

Das Design ist **nicht** generisch oder "AI-erzeugt". Es hat eine eigene Handschrift:
- Funktionale Schaerfe statt dekorativem Flair
- Praezise Typografie statt verspielter Fonts
- Gezielte Farbakzente statt buntem Interface
- Weissraum als Strukturelement

---

## Farbpalette

### Primaerfarbe: "Fakturus Blau"

Die Marke Fakturus nutzt ein kraeftiges Blau. Fuer Track setzen wir auf eine etwas waermere, ruhigere Variante -- passend zum Werkzeug-Charakter.

| Token | Hex | Verwendung |
|-------|-----|------------|
| `primary` | `#1A5CFF` | Primaere Aktionen, Links, aktiver Tab |
| `primary-light` | `#EBF0FF` | Hintergrundflaehen mit Akzent |
| `primary-dark` | `#0D3DBF` | Hover/Pressed States |

### Semantische Farben

| Token | Hex | Verwendung |
|-------|-----|------------|
| `success` | `#1DB954` | Positive Ueberstunden, laufender Timer, Erfolg |
| `success-light` | `#E8F8EE` | Hintergrund fuer positive Werte |
| `danger` | `#E5383B` | Negative Ueberstunden, Fehler, Loeschen |
| `danger-light` | `#FDECEC` | Hintergrund fuer negative Werte |
| `warning` | `#F59E0B` | Warnungen, Offline-Status, Beta-Badge |
| `warning-light` | `#FEF3C7` | Hintergrund fuer Warnungen |
| `neutral` | `#6B7280` | Sekundaertext, Icons, deaktiviert |

### Graustufen

| Token | Hex | Verwendung |
|-------|-----|------------|
| `gray-50` | `#F9FAFB` | Seitenhintergrund (Light Mode) |
| `gray-100` | `#F3F4F6` | Karten-Hintergrund alternierend |
| `gray-200` | `#E5E7EB` | Trennlinien, Borders |
| `gray-300` | `#D1D5DB` | Deaktivierte Elemente |
| `gray-500` | `#6B7280` | Sekundaerer Text |
| `gray-700` | `#374151` | Primaerer Text (Body) |
| `gray-900` | `#111827` | Headlines, wichtige Zahlen |

### Dark Mode

| Light Mode Token | Dark Mode Hex | Anmerkung |
|------------------|---------------|-----------|
| `gray-50` (BG) | `#0F1117` | Tiefschwarz mit leichtem Blauton |
| `gray-100` (Cards) | `#1A1D27` | Karten-Hintergrund |
| `gray-200` (Borders) | `#2D3040` | Subtile Trennung |
| `gray-900` (Text) | `#F0F1F3` | Heller Text |
| `primary` | `#4D8AFF` | Etwas heller fuer Lesbarkeit |
| `success` | `#34D96E` | Etwas heller |
| `danger` | `#FF6B6B` | Etwas heller |
| `pause` | `#A78BFA` | Helleres Violet fuer Dark Mode (Lesbarkeit auf dunklem Hintergrund) |

### Spezialfarben

| Token | Hex | Verwendung |
|-------|-----|------------|
| `timer-active` | `#1DB954` | Pulsierender Timer (laufende Session) |
| `timer-bg` | `#111827` | Timer-Hintergrund (dunkle Variante) |
| `vacation` | `#06B6D4` | Urlaubstage im Kalender |
| `holiday` | `#8B5CF6` | Feiertage im Kalender |
| `school-holiday` | `#F97316` | Schulferien im Kalender |
| `sick-day` | `#EF4444` | Krankheitstage im Kalender (Marktanalyse: neuer Abwesenheitstyp) |
| `pause` | `#8B5CF6` | Pausenindikator in Timer/History -- Violet/Lila, semantisch "pausiert/wartend", klar unterscheidbar von Warning-Gelb |

---

## Typografie

### Schriftart

**iOS:** SF Pro (System-Font) -- keine Custom Font noetig
**Android:** Roboto (System-Font) -- keine Custom Font noetig

System-Fonts sind die richtige Wahl, weil:
- Optimale Lesbarkeit auf jeder Plattform
- Automatische Unterstuetzung fuer Dynamic Type / Schriftgroesse
- Kein Font-Loading, kein Asset-Management
- Konsistent mit dem restlichen Betriebssystem

### Typografie-Skala

| Rolle | iOS (SF Pro) | Android (Roboto) | Verwendung |
|-------|-------------|-------------------|------------|
| Display Large | 34pt Bold | 57sp | Seitentitel (selten) |
| Headline | 28pt Bold | 32sp | Sektions-Ueberschriften |
| Title | 22pt Semibold | 22sp | Karten-Titel, Monatsnamen |
| Body Large | 17pt Regular | 16sp | Primaerer Fliesstext |
| Body | 15pt Regular | 14sp | Sekundaerer Text |
| Caption | 13pt Regular | 12sp | Zeitstempel, Labels |
| Timer | 48pt Monospaced Bold | 48sp Mono | Laufender Timer |

### Zahlen-Darstellung

Fuer Zeiten und Stunden nutzen wir **Tabular Figures** (gleichmaessige Ziffernbreite):

```swift
// iOS
Text("08:32h")
    .font(.system(.title, design: .monospaced))
    .monospacedDigit()
```

```kotlin
// Android
Text("08:32h",
    fontFamily = FontFamily.Monospace,
    fontFeatureSettings = "tnum"
)
```

---

## Spacing & Layout

### Spacing-Skala (8px Grundraster)

| Token | Wert | Verwendung |
|-------|------|------------|
| `xs` | 4px | Minimaler Abstand (z.B. Icon zu Label) |
| `sm` | 8px | Abstand zwischen eng zusammengehoerenden Elementen |
| `md` | 16px | Standard-Innenabstand (Padding) |
| `lg` | 24px | Abstand zwischen Sektionen |
| `xl` | 32px | Grosser Abstand (z.B. Seitenraender) |
| `2xl` | 48px | Abstand zwischen Hauptbereichen |

### Karten (Cards)

| Eigenschaft | Wert |
|-------------|------|
| Border Radius | 12px (iOS: 10px, Android: 12px) |
| Padding | 16px |
| Shadow | `0 1px 3px rgba(0,0,0,0.08)` (Light) / keine (Dark) |
| Background | Weiss (Light) / `gray-100` Dark |
| Spacing zwischen Cards | 12px |

### Safe Areas

- iOS: Automatisch via SafeAreaInsets
- Android: Edge-to-Edge + WindowInsets
- Tab Bar Hoehe: 49px (iOS) / 80dp (Android Material 3)
- Status Bar: Transparent, Content dahinter

---

## Icons

### Icon-System

**iOS:** SF Symbols (Apple's Icon-Library)
**Android:** Material Symbols (Google's Icon-Library)

Kein Custom Icon-Set noetig. Beide Plattformen bieten erstklassige Icons.

### Icon-Zuordnung

| Funktion | iOS (SF Symbol) | Android (Material) |
|----------|-----------------|-------------------|
| Zeiten Tab | `clock.fill` | `schedule` |
| Urlaub Tab | `sun.max.fill` | `beach_access` |
| Gesamt Tab | `chart.bar.fill` | `bar_chart` |
| Settings Tab | `gearshape.fill` | `settings` |
| Start | `play.fill` | `play_arrow` |
| Stop | `stop.fill` | `stop` |
| Finish | `checkmark.circle.fill` | `check_circle` |
| Delete | `trash.fill` | `delete` |
| Sync | `arrow.triangle.2.circlepath` | `sync` |
| Offline | `wifi.slash` | `cloud_off` |
| Timer | `timer` | `timer` |
| Edit | `pencil` | `edit` |
| Calendar | `calendar` | `calendar_today` |
| Person | `person.fill` | `person` |
| Add | `plus` | `add` |

### Icon-Groessen

| Kontext | iOS | Android |
|---------|-----|---------|
| Tab Bar | 24pt | 24dp |
| Navigation Bar | 20pt | 24dp |
| Inline (Body) | 17pt | 20dp |
| Large (Card Header) | 28pt | 28dp |
| Hero (Empty State) | 64pt | 64dp |

---

## Elevation & Schatten

### iOS-Ansatz (flach)
iOS nutzt minimale Schatten. Tiefe wird durch Hintergrundfarben und Trennlinien kommuniziert:
- Karten: Leichter Schatten (`0 1px 2px rgba(0,0,0,0.06)`)
- Modals/Sheets: System-Standard
- Tab Bar: Haarline-Border oben (`0.5px`)

### Android-Ansatz (Material Elevation)
Android nutzt das Material 3 Elevation-System:
- Cards: Level 1 (1dp)
- Bottom Sheet: Level 3 (6dp)
- Navigation Bar: Level 2 (3dp)
- FAB: Level 3 (6dp) -- falls verwendet

---

## Animationen & Micro-Interactions

### Timer-Animation (laufende Session)
- Pulsierender gruener Punkt (Heartbeat-Animation, 2s Zyklus)
- Sekundengenaue Aktualisierung der Anzeige
- Zahlen-Wechsel mit dezenter Transition

### Sync-Animation
- Rotierende Pfeile waehrend Sync
- Kurzer Checkmark nach erfolgreichem Sync
- Dauer: 300ms Rotation, 200ms Checkmark

### Swipe-to-Delete
- iOS: System-Standard (roter Hintergrund mit Papierkorb)
- Android: Material SwipeToDismiss mit rotem Hintergrund

### Tab-Wechsel
- iOS: Kein Animation (System-Standard)
- Android: Cross-Fade (200ms, Material 3 Standard)

---

## Grundsaetze fuer die Implementierung

1. **System-nah bleiben**: Nutze native Patterns, erfinde das Rad nicht neu
2. **Konsistenz vor Kreativitaet**: Lieber langweilig-konsistent als kreativ-verwirrend
3. **Zahlen muessen lesbar sein**: Grosse, mono-space Zahlen fuer Zeiten
4. **Farbe sparsam einsetzen**: Nur fuer Bedeutung (gruen=positiv, rot=negativ, blau=Aktion)
5. **Weissraum nutzen**: Mehr Platz = bessere Lesbarkeit
6. **Mobile-first**: Optimiert fuer Einhand-Bedienung, wichtige Aktionen im unteren Bildschirmbereich

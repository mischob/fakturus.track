# Tech-Spec: EPIC 10 -- MAUI-Migration & Sunset

## Uebersicht

Aenderungen nur in der MAUI-Codebasis (Fakturus.Track.Mobile). Die nativen Apps (iOS/Android) sind nicht betroffen. Ziel: Bestehende MAUI-Nutzer informieren, migrieren, MAUI-App kontrolliert abschalten.

---

## Migrations-Banner in MAUI-App

### Datei: MigrationBanner.razor (NEU)

Banner-Komponente mit 3 Stufen basierend auf Launch-Datum:

```csharp
@inject IPreferences Preferences

@if (BannerLevel > 0)
{
    <div class="migration-banner @BannerCssClass">
        @if (BannerLevel == 1)
        {
            <p>Neue native App verfuegbar!</p>
            <a href="@StoreLink">Jetzt wechseln</a>
            <button @onclick="Dismiss">X</button>
        }
        else if (BannerLevel == 2)
        {
            <p><strong>Bitte wechseln Sie zur neuen App.</strong> Diese Version wird bald eingestellt.</p>
            <a href="@StoreLink" class="btn-primary">Zur neuen App</a>
        }
        else
        {
            <!-- Vollbild-Overlay -->
            <div class="fullscreen-overlay">
                <h2>Diese App wird nicht mehr unterstuetzt.</h2>
                <a href="@StoreLink" class="btn-primary">Neue App installieren</a>
                <a @onclick="ContinueAnyway" class="link-small">Trotzdem weiter</a>
            </div>
        }
    </div>
}

@code {
    private int BannerLevel => CalculateBannerLevel();
    private bool isDismissed;

    // Launch-Datum wird hardcodiert oder via Remote Config gesetzt
    private static readonly DateTime LaunchDate = new DateTime(2026, 10, 15);

    private int CalculateBannerLevel()
    {
        var daysSinceLaunch = (DateTime.UtcNow - LaunchDate).Days;
        if (daysSinceLaunch < 0) return 0;  // Vor Launch
        if (daysSinceLaunch <= 14) return isDismissed ? 0 : 1;  // Woche 1-2
        if (daysSinceLaunch <= 28) return 2;  // Woche 3-4
        return 3;  // Nach 4 Wochen
    }

    private string StoreLink =>
        DeviceInfo.Current.Platform == DevicePlatform.iOS
            ? "https://apps.apple.com/app/idXXXXXXXXX"
            : "https://play.google.com/store/apps/details?id=com.fakturus.track";

    private string BannerCssClass => BannerLevel switch
    {
        1 => "banner-info",
        2 => "banner-warning",
        3 => "banner-critical",
        _ => ""
    };

    private void Dismiss() => isDismissed = true;
    private void ContinueAnyway() => isDismissed = true; // Temporaer schliessen
}
```

---

## Pending-Sync-Pruefung vor Migration

### Datei: SyncService.cs (MODIFIZIERT)

Die Methode `HasPendingSyncsAsync()` existiert bereits (laut migration.md). Sie wird im Banner verwendet:

```csharp
// Vor dem Store-Link:
@if (HasPendingSyncs)
{
    <p>Bitte synchronisieren Sie zuerst Ihre Daten.</p>
    <button @onclick="SyncNow">Jetzt synchronisieren</button>
}
else
{
    <p>Alle Daten sind synchronisiert. Sie koennen sicher wechseln.</p>
    <a href="@StoreLink">Zur neuen App</a>
}
```

Pruefung:
```csharp
private async Task<bool> CheckPendingSyncs()
{
    return await SyncService.HasPendingSyncsAsync();
}

private async Task SyncNow()
{
    await SyncService.SyncAllAsync();
    HasPendingSyncs = await SyncService.HasPendingSyncsAsync();
}
```

---

## Keine Aenderungen an nativen Apps

Die nativen iOS/Android Apps muessen fuer die MAUI-Migration NICHTS aendern:
- Login funktioniert ueber denselben Azure B2C Tenant
- Backend-API ist identisch
- Daten sind nach Login sofort verfuegbar (Server-Sync)
- Kein spezieller "Migrations-Modus" noetig

---

## Migrations-Kommunikation

### E-Mail-Template (DE)

```
Betreff: Fakturus Track -- Neue native App verfuegbar

Hallo [Name],

Fakturus Track gibt es jetzt als native App fuer iOS und Android!

Was ist neu:
- Deutlich schnellere Performance
- Widgets fuer den Homescreen
- Dark Mode
- Neue Features: Schulferien, Kalender-Integration

Ihre Daten sind sicher:
Alle Ihre erfassten Zeiten, Urlaubs- und Krankheitstage sind
bereits in der neuen App verfuegbar. Einfach installieren, anmelden, fertig.

[App Store Button]  [Google Play Button]

Wichtig: Die bisherige App wird in 4 Wochen eingestellt.
Bitte wechseln Sie zeitnah zur neuen Version.

Bei Fragen: support@fakturus.com
```

### Versand

- Nutzerliste: Azure B2C Graph API Export
- Versand: SendGrid, Mailchimp oder Azure Communication Services
- Timing: Am Launch-Tag oder 1 Tag danach (Store-Propagation abwarten)

---

## Sunset-Timeline

```
Tag X:       Launch der nativen Apps
             E-Mail an alle Nutzer
             MAUI-App: Banner Stufe 1 (dezent, dismissable)

Tag X+14:    MAUI-App: Banner Stufe 2 (prominent, nicht dismissable)

Tag X+28:    Pruefung:
             - Aktive MAUI-Sessions in letzten 7 Tagen? (Backend User-Agent)
             - Native Apps Crash-Free >= 99.5%?
             - App Store Rating >= 4.0?

             Wenn alles OK:
             - MAUI-App: Banner Stufe 3 (Vollbild)
             - MAUI-App aus Stores entfernen (verstecken, nicht loeschen)

Tag X+42:    MAUI-Repository archivieren (git branch archive/maui-app)
             Backend: MAUI-spezifische Logik pruefen und ggf. entfernen
```

---

## User-Agent Analyse

Backend-Logs pruefen um MAUI-Nutzung zu tracken:

```
MAUI User-Agent:   "FakturusTrack-MAUI/x.y.z"
iOS User-Agent:    "FakturusTrack-iOS/1.0.0"
Android User-Agent: "FakturusTrack-Android/1.0.0"
```

Query fuer aktive MAUI-Nutzer:
```sql
SELECT DISTINCT UserId
FROM RequestLogs
WHERE UserAgent LIKE 'FakturusTrack-MAUI%'
  AND Timestamp > DATEADD(DAY, -7, GETUTCDATE())
```

Wenn 0 Ergebnisse: MAUI kann sicher abgeschaltet werden.

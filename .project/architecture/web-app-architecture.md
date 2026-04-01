# Web-App Architektur -- Fakturus Track

## Leitprinzip

Gleiche Philosophie wie die nativen Apps: **Einfachheit und Direktheit ueber Abstraktion.** Blazor Server macht vieles einfacher als WASM -- kein Offline-Sync, kein LocalStorage, kein Token-Handling im Browser. Wir nutzen das konsequent aus.

---

## 1. Projekt-Struktur

### Entscheidung: Neues Projekt (nicht Umbau)

**Neues Projekt `Fakturus.Track.WebApp/`**, nicht Umbau des bestehenden WASM-Frontends.

Begruendung:
- Blazor Server und Blazor WASM sind fundamental unterschiedlich: Server haelt den State serverseitig, WASM laeuft komplett im Browser. Das aendert alles -- Auth, State Management, API-Calls, Offline-Logik.
- Das alte Frontend hat WASM-spezifischen Code: `LocalStorageService`, `SyncService`, `VacationSyncService`, `TrackAuthMessageHandler` (MSAL.js Token-Injection). All das ist fuer Server irrelevant.
- Sauberer Start ohne die technischen Schulden (BetaAccess-Service, alte Sync-Logik).
- Die wiederverwendbaren Teile (Models, API-Interfaces, Tailwind-Config) sind ueberschaubar und werden einfach kopiert und angepasst.

### Entscheidung: Blazor Server (nicht WASM, nicht United)

**Blazor Server**, nicht Blazor WASM, nicht Blazor United (Interactive Server + Static SSR).

| Option | Vorteil | Nachteil | Passt fuer uns? |
|--------|---------|----------|-----------------|
| Blazor WASM | Laeuft im Browser, Offline moeglich | Grosses Download, Token im Browser, CORS | Nein -- wir haben native Apps fuer Offline |
| Blazor Server | Server-to-Server API-Calls, kein CORS, Token sicher, schneller Start | SignalR-Verbindung noetig, keine Offline | Ja -- Web ist immer online |
| Blazor United (.NET 8) | SSR fuer Legal Pages + Interactive fuer App | Komplexer Render-Mode-Mix, zwei mentale Modelle | Overkill -- Legal Pages koennen auch in Blazor Server SSR-aehnlich sein |

Blazor Server Vorteile konkret:
- **API-Calls**: Server-to-Server via HttpClient, kein CORS, kein Token im Browser
- **Auth**: `Microsoft.Identity.Web` serverseitig, Token in der Session, kein MSAL.js noetig
- **Kein Download**: App startet sofort (kein 5MB WASM-Download)
- **State**: Server haelt den State, kein LocalStorage, kein IndexedDB
- **Legal Pages**: Werden als normale Razor Pages gerendert (kein JavaScript noetig = SEO-freundlich)

### Verzeichnisstruktur

```
Fakturus.Track.WebApp/
├── Program.cs                      # DI, Auth, Routing -- alles an einem Ort
├── Fakturus.Track.WebApp.csproj
├── appsettings.json
├── appsettings.Development.json
│
├── Components/                     # Blazor Components (App-Shell)
│   ├── App.razor                   # Root Component
│   ├── Routes.razor                # Router
│   ├── _Imports.razor
│   │
│   ├── Layout/                     # Layouts
│   │   ├── MainLayout.razor        # Sidebar + Content (authentifiziert)
│   │   ├── LegalLayout.razor       # Header + Footer (oeffentlich)
│   │   └── ConsentLayout.razor     # Zentriert, minimal (Consent-Flow)
│   │
│   ├── Pages/                      # Seiten (eine Datei pro Route)
│   │   ├── Dashboard.razor         # /
│   │   ├── TimeEntries.razor       # /zeiten
│   │   ├── Vacation.razor          # /urlaub
│   │   ├── Reports.razor           # /reports
│   │   ├── Settings.razor          # /einstellungen
│   │   ├── Consent.razor           # /consent
│   │   ├── Privacy.razor           # /privacy (oeffentlich)
│   │   ├── Terms.razor             # /terms (oeffentlich)
│   │   └── Imprint.razor           # /imprint (oeffentlich)
│   │
│   └── Shared/                     # Wiederverwendbare UI-Bausteine
│       ├── Timer.razor             # Timer-Widget (Dashboard)
│       ├── SessionEditPanel.razor  # Side-Panel fuer Session-Edit
│       ├── CalendarGrid.razor      # Monatskalender
│       ├── SummaryCard.razor       # Statistik-Karte
│       ├── Toast.razor             # Toast-Notification
│       └── MobileRedirect.razor    # Unter 768px: "Nutze die App"
│
├── Services/                       # API-Clients + Business Logic
│   ├── ApiClient.cs                # Typisierter HttpClient (alle Endpoints)
│   ├── TimerService.cs             # Timer-State (laeuft serverseitig)
│   └── ExportService.cs            # PDF/CSV/DATEV-Generierung
│
├── Models/                         # DTOs fuer API-Kommunikation
│   ├── WorkSession.cs
│   ├── VacationDay.cs
│   ├── UserSettings.cs
│   ├── OvertimeSummary.cs
│   ├── CalendarEvent.cs
│   └── SchoolHolidayPeriod.cs
│
├── wwwroot/                        # Statische Assets
│   ├── css/
│   │   └── app.css                 # Tailwind-Output
│   └── favicon.ico
│
├── tailwind.config.js              # Tailwind-Konfiguration
├── package.json                    # npm: Tailwind, PostCSS
└── Dockerfile
```

**Keine Unterordner in Services oder Models.** Bei 3 Service-Dateien und 6 Model-Dateien waere das Over-Engineering. Falls spaeter mehr dazukommt, refactoren wir dann.

### Namespace-Konvention

```
Fakturus.Track.WebApp                    # Root
Fakturus.Track.WebApp.Components.Pages   # Pages
Fakturus.Track.WebApp.Components.Shared  # Shared Components
Fakturus.Track.WebApp.Services           # Services
Fakturus.Track.WebApp.Models             # DTOs
```

---

## 2. Technologie-Stack

| Kategorie | Entscheidung | Begruendung |
|-----------|-------------|-------------|
| Runtime | .NET 10 (aktuell im Projekt) | Konsistenz mit Backend |
| Blazor-Modus | Blazor Server (InteractiveServer) | Siehe Abschnitt 1 |
| CSS | Tailwind CSS 3 via PostCSS | Bewaehrt im alten Frontend, Design-System-Farben bereits definiert |
| Icons | Heroicons (SVG inline) | Passt zum Tailwind-Oekosystem, Konsistenz mit Design |
| Font | Inter (self-hosted) | Bereits im alten Frontend, Design-Vorgabe |
| HTTP-Client | Typisierter HttpClient (kein Refit) | Refit macht fuer 5 Endpoints keinen Sinn wenn Server-Side -- ein einfacher Service reicht |
| Auth | Microsoft.Identity.Web | Standard-Bibliothek fuer Azure AD B2C in ASP.NET Server-Apps |
| Toasts | Eigene Component (10 Zeilen) | Blazored.Toast ist Overkill fuer ein paar Notifications |
| Validation | DataAnnotations | FluentValidation ist Overhead bei den wenigen Formularen |
| Realtime | Kein SignalR noetig | Timer laeuft serverseitig im Blazor-Circuit, Updates via `StateHasChanged()` |

### Kein Refit -- Begruendung

Das alte Frontend nutzt Refit mit 5 separaten Interface-Registrierungen (je ~5 Zeilen DI-Config). In Blazor Server brauchen wir kein Refit:

- Server-to-Server Calls sind simpler (kein Token-MessageHandler wie in WASM)
- Ein einzelner `ApiClient`-Service mit Methoden ist lesbarer als 5 Interfaces
- Token-Injection passiert einmal im HttpClient-Setup, nicht pro Interface

```csharp
// So sieht der ApiClient aus -- direkt, keine Interfaces:
public class ApiClient(HttpClient http)
{
    public Task<List<WorkSession>> GetWorkSessionsAsync()
        => http.GetFromJsonAsync<List<WorkSession>>("/v1/work-sessions");

    public Task<WorkSession> CreateWorkSessionAsync(CreateWorkSessionRequest req)
        => http.PostAsJsonAsync("/v1/work-sessions", req)
               .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<WorkSession>()).Unwrap();
    // ... alle Endpoints in einer Datei
}
```

### State Management: Kein Framework

Kein Fluxor, kein eigener Store. Blazor Server haelt den State im Circuit (serverseitig pro Verbindung). Das reicht:

- **Page-State**: Lokale Variablen im `@code`-Block der jeweiligen Page
- **Cross-Page-State** (Timer laeuft): `TimerService` als Scoped-Service (lebt fuer die Dauer der Verbindung)
- **User-Settings**: Einmal laden beim App-Start, als `CascadingValue` im `MainLayout` bereitstellen

Kein CascadingParameter-Overuse -- nur fuer echte App-weite Daten (UserSettings, Theme).

---

## 3. Architektur-Diagramm

```
┌─────────────────────────────────────────────────────────────────┐
│  Browser                                                         │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │  Blazor Server (SignalR-Verbindung)                       │   │
│  │  UI wird Server-Side gerendert, DOM-Diffs via WebSocket   │   │
│  └───────────────────────────────────────────────────────────┘   │
│  (Kein Client-Side JavaScript fuer App-Logik)                    │
└─────────────────────────────────────────────────────────────────┘
                              │ SignalR (WebSocket)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Fakturus.Track.WebApp (ASP.NET Server)                          │
│                                                                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────┐  │
│  │ Pages      │  │ Shared     │  │ Layouts    │  │ Legal     │  │
│  │ Dashboard  │  │ Timer      │  │ MainLayout │  │ Pages     │  │
│  │ TimeEntries│  │ EditPanel  │  │ LegalLayout│  │ (SSR)     │  │
│  │ Vacation   │  │ Calendar   │  │            │  │           │  │
│  │ Reports    │  │ SummaryCard│  │            │  │           │  │
│  │ Settings   │  │ Toast      │  │            │  │           │  │
│  └─────┬──────┘  └────────────┘  └────────────┘  └───────────┘  │
│        │                                                          │
│        │ Direkte Methoden-Aufrufe (kein Event-Bus)               │
│        ▼                                                          │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Services                                                   │  │
│  │ ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │  │
│  │ │ ApiClient    │  │ TimerService │  │ ExportService    │  │  │
│  │ │ (HttpClient) │  │ (Scoped)     │  │ (PDF/CSV/DATEV)  │  │  │
│  │ └──────┬───────┘  └──────────────┘  └──────────────────┘  │  │
│  └────────┼───────────────────────────────────────────────────┘  │
│           │ HTTP (Server-to-Server, kein CORS)                   │
│           │ Bearer Token aus Session                              │
└───────────┼──────────────────────────────────────────────────────┘
            ▼
┌─────────────────────────────────────────────────────────────────┐
│  Fakturus.Track.Backend (api.track.fakturus.com)                 │
│  ASP.NET Core 10 + FastEndpoints + PostgreSQL                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐    │
│  │WorkSess. │  │Vacation  │  │Settings  │  │Legal/Consent │    │
│  │Endpoints │  │Endpoints │  │Endpoints │  │Endpoints     │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Injection Setup (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Auth: Microsoft.Identity.Web fuer Azure AD B2C
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
    // HINWEIS: In-Memory Token Cache geht bei App-Service-Restart verloren.
    // Nutzer muessen sich dann erneut anmelden. Fuer MVP akzeptabel.
    // Bei Bedarf: .AddDistributedTokenCaches() mit Redis/SQL Server.

// API-Client: Ein typisierter HttpClient mit automatischem Token
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!);
});

// Services
builder.Services.AddScoped<TimerService>();
builder.Services.AddScoped<ExportService>();

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Was wird NICHT aus dem alten Frontend uebernommen

| Alte Datei/Konzept | Begruendung fuer Wegfall |
|-------------------|--------------------------|
| `LocalStorageService` | Kein Client-Side Storage noetig |
| `SyncService`, `VacationSyncService` | Kein Offline-Sync, direkte API-Calls |
| `TrackAuthMessageHandler` | Token-Handling uebernimmt Microsoft.Identity.Web serverseitig |
| `BetaAccessService` | Feature wird nicht mehr gebraucht |
| `CalendarModalService` | Desktop nutzt Side-Panel statt Modal |
| `VersionCheckService` | Server weiss seine eigene Version |
| Alle Refit-Interfaces | Ersetzt durch einen direkten `ApiClient` |

### Was wird uebernommen (kopiert + angepasst)

| Quelle | Ziel | Aenderung |
|--------|------|-----------|
| `Models/*.cs` | `Models/*.cs` | Sync-spezifische Properties entfernen (`IsSynced`, `IsPendingSync`, `SyncedAt`) |
| `tailwind.config.js` | `tailwind.config.js` | Farben auf Design-System updaten (Primary #1A5CFF statt #2563eb), Safe-Area-Plugin entfernen |
| API-Endpoint-Pfade aus Refit-Interfaces | `ApiClient.cs` Methoden | Direkte HttpClient-Calls statt Refit-Annotationen |

---

## 4. Auth-Konzept

### Azure AD B2C in Blazor Server

```
Browser                    WebApp Server                  B2C Tenant
  │                            │                              │
  │  1. Zugriff auf /          │                              │
  │ ──────────────────────►    │                              │
  │                            │  2. Nicht auth? Redirect     │
  │  ◄──────────────────────── │                              │
  │                            │                              │
  │  3. Login bei B2C          │                              │
  │ ─────────────────────────────────────────────────────►    │
  │                            │                              │
  │  4. Auth-Code Callback     │                              │
  │ ──────────────────────►    │                              │
  │                            │  5. Code -> Token (Server)   │
  │                            │ ────────────────────────►    │
  │                            │  ◄────────────────────────   │
  │                            │  6. Token in Session/Cache   │
  │                            │                              │
  │  7. Blazor-App geladen     │                              │
  │  ◄──────────────────────── │                              │
  │                            │                              │
  │  8. API-Call (Server-Side) │                              │
  │  (via SignalR)             │  9. HttpClient + Bearer      │
  │ ──────────────────────►    │ ──────► API Backend          │
```

**Entscheidende Vorteile gegenueber WASM:**
- Token **nie** im Browser (kein MSAL.js, kein Token in JavaScript)
- Server tauscht Auth-Code gegen Token und speichert ihn im In-Memory Token-Cache
- Bei API-Calls haengt der Server den Bearer Token an -- der Browser sieht ihn nie
- Token-Refresh passiert serverseitig und transparent

### Konfiguration (appsettings.json)

```json
{
  "AzureAdB2C": {
    "Instance": "https://fakturusb2c.b2clogin.com",
    "Domain": "fakturusb2c.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<webapp-client-id>",
    "ClientSecret": "<in-key-vault>",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "ApiSettings": {
    "BaseUrl": "https://api.track.fakturus.com",
    "Scopes": [ "https://fakturusb2c.onmicrosoft.com/track-api/access" ]
  }
}
```

**Hinweis:** Die WebApp braucht eine eigene App-Registration in Azure AD B2C (Typ: "Web", nicht "SPA"), weil Server-seitige Apps den Confidential Client Flow nutzen (mit ClientSecret), waehrend WASM den Public Client Flow nutzt (ohne Secret).

### Consent-Gate

**Als Blazor Component**, nicht als Middleware.

Begruendung: Consent ist UI-Logik (Checkboxen, Links zu Legal Pages), keine HTTP-Pipeline-Logik. Als Component kann sie Blazor-Routing und State nutzen.

```razor
<!-- In MainLayout.razor -->
@if (!userSettings.HasConsented)
{
    <ConsentLayout>
        <Consent OnConsented="HandleConsent" />
    </ConsentLayout>
}
else
{
    <Sidebar />
    <main>@Body</main>
}
```

Der Consent-Status wird beim Laden der UserSettings vom Backend abgefragt. Kein zusaetzlicher Endpoint noetig -- `GetConsentStatus` existiert bereits.

---

## 5. Legal Pages Routing

### Problem

Legal Pages (/privacy, /terms, /imprint) muessen:
1. Ohne Login erreichbar sein
2. SEO-freundlich sein (Server-Side gerendert, kein JavaScript noetig)
3. Eigenes Layout haben (kein Sidebar, stattdessen Header + Footer)

### Loesung: Blazor Pages mit `[AllowAnonymous]` und eigenem Layout

```razor
<!-- Privacy.razor -->
@page "/privacy"
@layout LegalLayout
@attribute [AllowAnonymous]

<PageTitle>Datenschutzerklaerung -- fakturus.track</PageTitle>

<article class="prose max-w-3xl mx-auto">
    @* SECURITY: MarkupString rendert Raw-HTML. Nur trusted Content verwenden. *@
    @* Der /legal/*.html Endpoint liefert ausschliesslich statische, vom Team gepflegte Inhalte. *@
    @((MarkupString)legalContent)
</article>

@code {
    private string legalContent = "";

    protected override async Task OnInitializedAsync()
    {
        // Inhalt vom Backend laden (bereits existierender Endpoint)
        legalContent = await ApiClient.GetLegalContentAsync("privacy");
    }
}
```

### Warum keine statischen HTML-Dateien im wwwroot?

- Die Legal-Inhalte haben Versionierung im Backend (`legal-versions.json`, Consent-Tracking)
- Statisches HTML muesste bei jeder Aenderung neu deployed werden
- Das Backend hat bereits `/legal/*.html` Endpoints -- wir nutzen die
- Blazor Server rendert die Pages serverseitig -- der Browser bekommt fertiges HTML (SEO-freundlich)

### SEO-Anforderungen

Blazor Server rendert den initialen HTML-Response komplett serverseitig. Legal Pages sind damit automatisch SEO-freundlich:
- Suchmaschinen sehen den vollen HTML-Inhalt
- Kein JavaScript noetig fuer den ersten Render
- `<meta>`-Tags und `<title>` via `<PageTitle>` und `<HeadContent>`

Falls Google den SignalR-Reconnect-Overhead nicht mag: Legal Pages koennten spaeter als Static SSR (`rendermode="None"`) gerendert werden -- ein Einzeiler-Aenderung in .NET 8.

---

## 6. Stripe Integration

### Uebersicht

Stripe wird fuer Web-Abos genutzt (statt App Store / Google Play). Die Integration hat drei Teile:

```
Browser                    WebApp Server              Stripe              Backend
  │                            │                        │                    │
  │  1. "Abo starten"         │                        │                    │
  │ ──────────────────────►    │                        │                    │
  │                            │  2. Create Checkout    │                    │
  │                            │     Session (Server)   │                    │
  │                            │ ──────────────────►    │                    │
  │                            │  ◄──────────────────   │                    │
  │  3. Redirect zu Stripe     │                        │                    │
  │  ◄──────────────────────── │                        │                    │
  │                            │                        │                    │
  │  4. Zahlung bei Stripe     │                        │                    │
  │ ─────────────────────────────────────────────────►  │                    │
  │                            │                        │                    │
  │  5. Redirect zurueck       │                        │                    │
  │ ──────────────────────►    │                        │                    │
  │                            │                        │  6. Webhook        │
  │                            │                        │ ──────────────►    │
  │                            │                        │                    │
  │                            │  7. Abo-Status via API │                    │
  │                            │ ───────────────────────────────────────►    │
  │  8. "PRO aktiv"           │                        │                    │
  │  ◄──────────────────────── │                        │                    │
```

### Entscheidung: Server-Side Stripe (kein Stripe.js im Browser)

**Stripe Checkout Sessions** -- komplett Server-seitig via Stripe.NET SDK.

Begruendung:
- Kein Stripe.js noetig (der Browser wird zu Stripes gehosteter Checkout-Seite redirected)
- Keine PCI-Compliance-Anforderungen fuer uns (Stripe hostet das Zahlungsformular)
- Einfacher: Ein API-Call erstellt die Checkout-Session, Browser wird redirected
- Stripe Customer Portal fuer Abo-Verwaltung (auch gehostet bei Stripe)

```csharp
// In ApiClient.cs oder als eigener StripeService im Backend
public async Task<string> CreateCheckoutSessionAsync()
{
    var response = await http.PostAsJsonAsync("/v1/subscriptions/create-checkout", new { });
    var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
    return result.CheckoutUrl; // Browser wird hierhin redirected
}
```

**Stripe.js wird NICHT benoetigt.** Der `wwwroot/js/stripe.js` aus der Verzeichnisstruktur kann entfallen. Checkout und Customer Portal sind gehostete Stripe-Seiten.

### Webhook-Handling

Webhooks gehen direkt ans Backend (nicht an die WebApp):

```
Stripe Webhook ──► api.track.fakturus.com/v1/stripe/webhook
```

Das Backend verarbeitet:
- `checkout.session.completed` -- Abo aktivieren
- `customer.subscription.updated` -- Plan-Aenderungen
- `customer.subscription.deleted` -- Kuendigung
- `invoice.payment_failed` -- Zahlungsproblem

### Abo-Sync mit Mobile-Abos

Die Abo-Synchronisation zwischen Stripe (Web), Apple (iOS) und Google (Android) wird im Backend geloest:

```
┌─────────┐     ┌─────────┐     ┌─────────┐
│  Stripe │     │  Apple  │     │  Google │
│ Webhook │     │ S2S Not.│     │ RTDN    │
└────┬────┘     └────┬────┘     └────┬────┘
     │               │               │
     ▼               ▼               ▼
┌──────────────────────────────────────────┐
│  Backend: SubscriptionService            │
│  - Alle Quellen schreiben in eine        │
│    Subscription-Tabelle                  │
│  - GET /v1/subscription/status           │
│    gibt immer den aktuellen Stand        │
│  - Plattform-uebergreifend:             │
│    Ein PRO-Abo gilt ueberall             │
└──────────────────────────────────────────┘
```

**Wichtig:** Die WebApp fragt nur den Status ab (`GET /v1/subscription/status`). Die Logik, welches Abo von welcher Plattform gilt, liegt komplett im Backend.

---

## 7. Deployment

### Azure App Service Konfiguration

```
track.fakturus.com     ──►  Azure App Service (WebApp)     Port 8080
api.track.fakturus.com ──►  Azure App Service (Backend)    Port 8080
```

Beide Services laufen als separate App Services. Kein Shared-Hosting, kein Azure Container Apps (Overkill).

### Docker

**Ja, Docker.** Konsistenz mit dem Backend (hat bereits ein Dockerfile).

```dockerfile
# Fakturus.Track.WebApp/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Node fuer Tailwind-Build
RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y nodejs

WORKDIR /src
COPY ["Fakturus.Track.WebApp/Fakturus.Track.WebApp.csproj", "Fakturus.Track.WebApp/"]
RUN dotnet restore "Fakturus.Track.WebApp/Fakturus.Track.WebApp.csproj"

COPY Fakturus.Track.WebApp/ Fakturus.Track.WebApp/
WORKDIR /src/Fakturus.Track.WebApp

# Tailwind Build
RUN npm ci && npm run buildcss

# .NET Build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Fakturus.Track.WebApp.dll"]
```

### CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/webapp-deploy.yml
name: Deploy WebApp

on:
  push:
    branches: [main]
    paths: ['Fakturus.Track.WebApp/**']
  workflow_dispatch:

jobs:
  build-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Build Docker image
        run: docker build -f Fakturus.Track.WebApp/Dockerfile -t fakturus-webapp .

      - name: Push to Azure Container Registry
        run: |
          docker tag fakturus-webapp fakturusacr.azurecr.io/track-webapp:${{ github.sha }}
          docker push fakturusacr.azurecr.io/track-webapp:${{ github.sha }}

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: fakturus-track-webapp
          images: fakturusacr.azurecr.io/track-webapp:${{ github.sha }}
```

Staging-Environment: `staging-track.fakturus.com` als separater App Service Slot oder separater App Service.

### Domain-Setup

| Domain | Ziel | SSL |
|--------|------|-----|
| `track.fakturus.com` | Azure App Service (WebApp) | Azure Managed Certificate |
| `api.track.fakturus.com` | Azure App Service (Backend) | Azure Managed Certificate |

DNS: CNAME-Records auf die `.azurewebsites.net` Adressen.

---

## 8. Migration

### Strategie

**Hard-Cut, kein paralleler Betrieb.**

Das alte Blazor WASM Frontend unter `track.fakturus.com` wird durch die neue Blazor Server App ersetzt. Kein Feature-Flag, kein A/B-Test -- es ist ein Ein-Personen-Projekt.

Schritte:
1. Neue WebApp entwickeln und auf `staging-track.fakturus.com` testen
2. Wenn Feature-komplett: DNS-Switch von `track.fakturus.com` auf neuen App Service
3. Altes Frontend-Projekt im Repo behalten (fuer Referenz), aber nicht mehr deployen

### Was ist wiederverwendbar?

| Aus dem alten Frontend | Aufwand | Strategie |
|----------------------|---------|-----------|
| Models (6 Dateien) | Kopieren, Sync-Properties entfernen | 30 min |
| Tailwind-Config (Farben, Font) | Kopieren, anpassen | 15 min |
| API-Endpoint-Pfade | In `ApiClient.cs` uebernehmen | Im Zuge der Implementierung |
| Blazor-Page-Logik (Berechnungen, Formatierung) | Selektiv uebernehmen | Im Zuge der Implementierung |
| CSS-Klassen / Tailwind-Patterns | Visuell referenzieren, neu schreiben | Design ist sowieso neu |

**Nicht wiederverwendbar:** Alles was mit WASM, Offline, Sync, MSAL.js zu tun hat (~50% des alten Codes).

### Datenmigration

**Keine noetig.** Das Backend bleibt identisch. Die WebApp nutzt die gleiche API wie die Mobile-Apps. Gleicher User, gleiche Daten, gleicher B2C-Tenant. Der User meldet sich einfach im Web an und sieht seine Daten.

---

## Zusammenfassung der Entscheidungen

| Entscheidung | Wahl | Verworfene Alternativen |
|-------------|------|------------------------|
| Projekt | Neues `Fakturus.Track.WebApp/` | Umbau des WASM-Frontends |
| Blazor-Modus | Blazor Server | WASM, United/Hybrid |
| State Management | Scoped Services + CascadingValue | Fluxor, Redux-Pattern |
| HTTP-Client | Typisierter HttpClient | Refit, RestSharp |
| Auth | Microsoft.Identity.Web (Confidential Client) | MSAL.js (Public Client) |
| Consent | Blazor Component | HTTP Middleware |
| Legal Pages | Blazor Pages mit `[AllowAnonymous]` | Statische HTML in wwwroot |
| Stripe | Checkout Sessions + Customer Portal (Server-Side) | Stripe.js Elements (Client-Side) |
| Deployment | Docker auf Azure App Service | Direkt-Deployment ohne Docker |
| Migration | Hard-Cut mit Staging-Test | Paralleler Betrieb |
| Toasts | Eigene Component | Blazored.Toast |
| Validation | DataAnnotations | FluentValidation |
| Realtime (Timer) | `StateHasChanged()` im Blazor Circuit | SignalR Hub |

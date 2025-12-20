using Blazored.Toast;
using Fakturus.Track.Frontend;
using Fakturus.Track.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure Azure AD B2C authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);

    // Set the redirect URIs explicitly
    var baseUri = builder.HostEnvironment.BaseAddress;
    options.ProviderOptions.Authentication.RedirectUri = $"{baseUri}authentication/login-callback";
    options.ProviderOptions.Authentication.PostLogoutRedirectUri = $"{baseUri}authentication/logout-callback";

    // Add the required scope for your API from configuration
    var apiScope = builder.Configuration["AzureAdB2C:ApiScope"];
    if (!string.IsNullOrEmpty(apiScope)) options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);

    options.ProviderOptions.LoginMode = "redirect";
});

// Configure HTTP client with authentication
builder.Services.AddHttpClient("ServerAPI",
        client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7067");
        })
    .AddHttpMessageHandler<TrackAuthMessageHandler>();

// Configure Refit API clients
builder.Services.AddRefitClient<IWorkSessionsApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7067");
    })
    .AddHttpMessageHandler<TrackAuthMessageHandler>();

builder.Services.AddRefitClient<ICalendarApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7067");
    })
    .AddHttpMessageHandler<TrackAuthMessageHandler>();

builder.Services.AddRefitClient<IVacationApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7067");
    })
    .AddHttpMessageHandler<TrackAuthMessageHandler>();

builder.Services.AddRefitClient<ISettingsApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7067");
    })
    .AddHttpMessageHandler<TrackAuthMessageHandler>();

// Register the authorization message handler
builder.Services.AddScoped<TrackAuthMessageHandler>();

// Configure the BaseAddressAuthorizationMessageHandler
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("ServerAPI"));

// NEW: Unauthenticated client for version check
builder.Services.AddHttpClient("VersionCheck",
    client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7067");
    });

builder.Services.AddScoped<IVersionCheckService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("VersionCheck");
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    return new VersionCheckService(httpClient, jsRuntime);
});

// Register services
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IVacationSyncService, VacationSyncService>();
builder.Services.AddSingleton<ICalendarModalService, CalendarModalService>();

// Add Toast notifications
builder.Services.AddBlazoredToast();

var app = builder.Build();

// Start periodic sync
var syncService = app.Services.GetRequiredService<ISyncService>();
await syncService.StartPeriodicSyncAsync();

await app.RunAsync();
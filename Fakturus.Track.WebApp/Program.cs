using Fakturus.Track.WebApp.Components;
using Fakturus.Track.WebApp.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Auth: Azure AD B2C
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Microsoft Identity UI controllers (provides /MicrosoftIdentity/Account/SignIn etc.)
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// API Client
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
                                 ?? "https://api.track.fakturus.com");
});

// Services
builder.Services.AddScoped<TimerService>();
builder.Services.AddScoped<ConsentService>();

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Map Microsoft Identity UI controllers (/MicrosoftIdentity/Account/SignIn, SignOut)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization(); // Forces OIDC redirect for unauthenticated users at HTTP level

app.Run();

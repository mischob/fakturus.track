using Fakturus.Track.WebApp.Components;
using Fakturus.Track.WebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Serilog;
using Serilog.Formatting.Compact;

// Configure Serilog (Fakturus Logging-Standard: JSON on stdout)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("service",
        Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "fakturus-track-webapp")
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Configure forwarded headers for reverse proxy (Traefik)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

app.UseForwardedHeaders();

// Trace-ID middleware (Fakturus Logging-Standard)
app.Use(async (context, next) =>
{
    var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault()
                  ?? Guid.NewGuid().ToString();
    using (Serilog.Context.LogContext.PushProperty("trace_id", traceId))
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
        await next();
    }
});

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

// GET-accessible sign-out endpoint (MicrosoftIdentity/Account/SignOut only accepts POST)
app.MapGet("/account/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    await httpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

// Export proxy endpoint - forwards export requests to the Backend API with authentication
app.MapGet("/api/reports/export", async (
    HttpContext httpContext,
    ITokenAcquisition tokenAcquisition,
    IConfiguration config,
    IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var scopes = new[] { "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access" };
        var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);

        var backendUrl = config["ApiSettings:BaseUrl"] ?? "http://fakturus-track-api:80";
        var query = httpContext.Request.QueryString;
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"{backendUrl}/v1/reports/export{query}");

        if (!response.IsSuccessStatusCode)
            return Results.StatusCode((int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "export";
        var bytes = await response.Content.ReadAsByteArrayAsync();

        return Results.File(bytes, contentType, fileName);
    }
    catch (Microsoft.Identity.Client.MsalUiRequiredException)
    {
        return Results.Redirect("/account/logout");
    }
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization(); // Forces OIDC redirect for unauthenticated users at HTTP level

try
{
    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

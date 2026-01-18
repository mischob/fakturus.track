using System.Reflection;
using Blazored.Toast;
using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Services.Api;
using Fakturus.Track.Mobile.Services.Auth;
using Fakturus.Track.Mobile.Services.Network;
using Fakturus.Track.Mobile.Services.Offline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Refit;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Fakturus.Track.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        // Configure Serilog
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Fakturus.Track.Mobile")
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug(outputTemplate: "[{Level:u3}] {Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        Log.Logger = loggerConfig.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        // Configuration
        var configurationBuilder = new ConfigurationBuilder();
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Fakturus.Track.Mobile.appsettings.json");

        if (stream != null)
        {
            configurationBuilder.AddJsonStream(stream);
        }
        else
        {
            // Fallback: Try to load from file system if embedded resource not found
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath)) configurationBuilder.AddJsonFile(appSettingsPath, false);
        }

        var configuration = configurationBuilder.Build();
        builder.Services.AddSingleton<IConfiguration>(configuration);

        // Database
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "fakturus_track.db");
        builder.Services.AddDbContext<MobileDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databasePath}");
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(message => Log.Logger.Debug("[Database] {Message}", message), LogLevel.Debug);
#endif
        });

        // Offline Data Services
        builder.Services.AddScoped<IWorkSessionService, WorkSessionService>();
        builder.Services.AddScoped<IVacationDayService, VacationDayService>();
        builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
        builder.Services.AddScoped<ISyncQueueService, SyncQueueService>();

        // Auth Services
        builder.Services.AddSingleton<IDeviceIdService, DeviceIdService>();
        builder.Services.AddScoped<IOfflineAuthService, OfflineAuthService>();

        // Network Services
        var networkMonitor = new NetworkMonitor();
        networkMonitor.StartMonitoring();
        builder.Services.AddSingleton<INetworkMonitor>(networkMonitor);

        // Conflict Resolution
        builder.Services.AddScoped<IConflictResolver, ConflictResolver>();

        // Sync Service
        builder.Services.AddScoped<ISyncService, SyncService>();

        // API Clients
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://api.track.fakturus.com";

        builder.Services.AddScoped<TrackAuthMessageHandler>();

        builder.Services.AddRefitClient<IWorkSessionsApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<TrackAuthMessageHandler>();

        builder.Services.AddRefitClient<IVacationApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<TrackAuthMessageHandler>();

        builder.Services.AddRefitClient<ISettingsApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<TrackAuthMessageHandler>();

        builder.Services.AddRefitClient<ICalendarApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<TrackAuthMessageHandler>();

        builder.Services.AddRefitClient<ISchoolHolidayApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<TrackAuthMessageHandler>();

        // Toast Notifications
        builder.Services.AddBlazoredToast();

        var app = builder.Build();

        // Initialize database
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MobileDbContext>();
            dbContext.Database.EnsureCreated();
        }

        return app;
    }
}
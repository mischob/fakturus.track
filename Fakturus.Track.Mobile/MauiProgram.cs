using Microsoft.Extensions.Logging;
using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Services.Offline;
using Fakturus.Track.Mobile.Services.Auth;
using Fakturus.Track.Mobile.Services.Network;
using Fakturus.Track.Mobile.Services.Api;
using Microsoft.EntityFrameworkCore;
using Refit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Blazored.Toast;
using Microsoft.Maui.Storage;
using System.Reflection;

namespace Fakturus.Track.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
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
			if (File.Exists(appSettingsPath))
			{
				configurationBuilder.AddJsonFile(appSettingsPath, optional: false);
			}
		}
		
		var configuration = configurationBuilder.Build();
		builder.Services.AddSingleton<IConfiguration>(configuration);

		// Database
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "fakturus_track.db");
		builder.Services.AddDbContext<MobileDbContext>(options =>
			options.UseSqlite($"Data Source={databasePath}"));

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

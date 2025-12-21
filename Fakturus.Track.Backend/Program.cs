using Asp.Versioning;
using Azure.Identity;
using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault for Production environment
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"] ?? "https://fakturus.vault.azure.net/";
    var clientId = builder.Configuration["KeyVault:ClientId"] ?? Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
    var clientSecret = builder.Configuration["KeyVault:ClientSecret"] ??
                       Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
    var tenantId = builder.Configuration["KeyVault:TenantId"] ?? Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

    if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId))
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
    }
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string? connectionString;

    // In production, get connection string from Key Vault
    if (!builder.Environment.IsDevelopment())
    {
        connectionString = builder.Configuration["TrackPostgresConnectionString"]
                           ?? builder.Configuration.GetConnectionString("DefaultConnection");

        // Validate connection string
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Database connection string is missing. " +
                "Ensure 'TrackPostgresConnectionString' secret exists in Azure Key Vault or 'DefaultConnection' is configured.");

        // Clean the connection string: remove any newlines, carriage returns, and extra whitespace
        connectionString = connectionString
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Trim();

        // Validate the connection string format by attempting to parse it
        try
        {
            var connStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            // If parsing succeeds, the connection string is valid
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Invalid PostgreSQL connection string format from Key Vault. " +
                $"Error: {ex.Message}. " +
                $"Ensure the 'TrackPostgresConnectionString' secret contains a valid PostgreSQL connection string " +
                $"(format: Host=...;Database=...;Username=...;Password=...).",
                ex);
        }
    }
    else
    {
        // In development, use the default connection string from configuration
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Database connection string is missing. " +
                "Ensure 'ConnectionStrings:DefaultConnection' is configured in appsettings.Development.json");

        connectionString = connectionString.Trim();
    }

    options.UseNpgsql(connectionString);
});

// Configure Authentication (Azure AD B2C)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options);
            options.TokenValidationParameters.NameClaimType = "name";

            // Ensure Audience is set - it's required for token validation
            var azureAdB2CConfig = builder.Configuration.GetSection("AzureAdB2C");
            var audience = azureAdB2CConfig["Audience"] ?? azureAdB2CConfig["ClientId"];

            if (!string.IsNullOrEmpty(audience))
            {
                options.TokenValidationParameters.ValidAudiences = new[] { audience };
                options.TokenValidationParameters.ValidateAudience = true;
            }
            else
            {
                throw new InvalidOperationException(
                    "AzureAdB2C Audience is not configured. " +
                    "Ensure 'AzureAdB2C:Audience' or 'AzureAdB2C:ClientId' is set in configuration or Key Vault.");
            }
        },
        options => { builder.Configuration.Bind("AzureAdB2C", options); });

// Post-configure JWT Bearer options to ensure B2C issuer validation works correctly
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var azureAdB2CConfig = builder.Configuration.GetSection("AzureAdB2C");
    var instance = azureAdB2CConfig["Instance"]?.TrimEnd('/');
    var tenantId = azureAdB2CConfig["TenantId"];
    var audience = azureAdB2CConfig["Audience"] ?? azureAdB2CConfig["ClientId"];

    // Ensure Audience is set if it wasn't set during initial configuration
    if (string.IsNullOrEmpty(audience))
        throw new InvalidOperationException(
            "AzureAdB2C Audience is not configured. " +
            "Ensure 'AzureAdB2C:Audience' or 'AzureAdB2C:ClientId' is set in configuration or Key Vault.");

    // Set audience if not already set
    if (options.TokenValidationParameters.ValidAudiences == null ||
        !options.TokenValidationParameters.ValidAudiences.Any())
    {
        options.TokenValidationParameters.ValidAudiences = new[] { audience };
        options.TokenValidationParameters.ValidateAudience = true;
    }

    if (!string.IsNullOrEmpty(instance) && !string.IsNullOrEmpty(tenantId) && instance.Contains("b2clogin.com"))
    {
        // Build the expected B2C issuer
        var b2cIssuer = $"{instance}/{tenantId}/v2.0/";

        // Ensure B2C issuer is in valid issuers list
        var validIssuers = new List<string> { b2cIssuer };
        if (options.TokenValidationParameters.ValidIssuers != null)
        {
            validIssuers.AddRange(options.TokenValidationParameters.ValidIssuers);
            validIssuers = validIssuers.Distinct().ToList();
        }

        options.TokenValidationParameters.ValidIssuers = validIssuers;
        options.TokenValidationParameters.ValidateIssuer = true;
    }
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthentication", policy =>
        policy.RequireAuthenticatedUser());
});

// API Versioning Setup
VersionSets.CreateApi("FakturusTrack", v => v
    .HasApiVersion(new ApiVersion(1, 0)));

VersionSets.CreateApi("health", v => v
    .HasApiVersion(new ApiVersion(1, 0)));

// FastEndpoints with Versioning and Swagger
builder.Services.AddFastEndpoints();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Fakturus Track API";
        s.Version = "v1";
        s.Description = "API for Fakturus Track - Time Tracker";
    };
    o.EnableJWTBearerAuth = true;
    o.ExcludeNonFastEndpoints = false;
    o.AutoTagPathSegmentIndex = 0;
    o.ShortSchemaNames = true;
});

// Add custom services
builder.Services.AddScoped<IWorkSessionService, WorkSessionService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IVacationDayService, VacationDayService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IOvertimeCalculationService, OvertimeCalculationService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<ISchoolHolidayService, SchoolHolidayService>();
builder.Services.AddHttpClient(); // For fetching calendar feed

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
            policy.WithOrigins("https://localhost:7086", "http://localhost:5138", "https://localhost:7003",
                    "http://localhost:7003")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        else
            policy.WithOrigins("https://track.fakturus.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
    });
});

var app = builder.Build();

// Log configuration status for debugging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
if (!app.Environment.IsDevelopment())
{
    var hasKeyVaultConnectionString = !string.IsNullOrEmpty(app.Configuration["TrackPostgresConnectionString"]);
    logger.LogInformation("Production environment detected. Key Vault connection string available: {HasKeyVault}",
        hasKeyVaultConnectionString);

    var azureAdB2CConfig = app.Configuration.GetSection("AzureAdB2C");
    var audience = azureAdB2CConfig["Audience"] ?? azureAdB2CConfig["ClientId"];
    logger.LogInformation("AzureAdB2C Audience configured: {HasAudience}", !string.IsNullOrEmpty(audience));
}

// Apply pending database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            dbLogger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));
            context.Database.Migrate();
            dbLogger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            dbLogger.LogInformation("Database is up to date, no pending migrations");
        }
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "An error occurred while applying database migrations");
        throw; // Re-throw to prevent app from starting with incorrect database schema
    }
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app
    .UseAuthentication()
    .UseAuthorization()
    .UseDefaultExceptionHandler()
    .UseFastEndpoints(c => { c.Versioning.Prefix = "v"; })
    .UseSwaggerGen()
    .UseSwaggerUI();

try
{
    app.Run();
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}
using Fakturus.Track.Mobile.Data.Entities;
using Fakturus.Track.Mobile.Services.Api;
using Fakturus.Track.Mobile.Services.Auth;
using Fakturus.Track.Mobile.Services.Network;
using Fakturus.Track.Mobile.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using VacationDayDto = Fakturus.Track.Mobile.Services.Api.VacationDayDto;
using CreateWorkSessionRequest = Fakturus.Track.Mobile.Services.Api.CreateWorkSessionRequest;
using SyncWorkSessionsRequest = Fakturus.Track.Mobile.Services.Api.SyncWorkSessionsRequest;
using SyncVacationDaysRequest = Fakturus.Track.Mobile.Services.Api.SyncVacationDaysRequest;

namespace Fakturus.Track.Mobile.Services.Offline;

public class SyncService : ISyncService, IDisposable
{
    private readonly IOfflineAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IConflictResolver _conflictResolver;
    private readonly ILogger<SyncService> _logger;
    private readonly INetworkMonitor _networkMonitor;
    private readonly ISettingsApiClient _settingsApiClient;
    private readonly int _syncIntervalSeconds;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IVacationApiClient _vacationApiClient;
    private readonly IVacationDayService _vacationDayService;
    private readonly IWorkSessionsApiClient _workSessionsApiClient;
    private readonly IWorkSessionService _workSessionService;

    private Timer? _syncTimer;

    public SyncService(
        IWorkSessionService workSessionService,
        IVacationDayService vacationDayService,
        IUserSettingsService userSettingsService,
        IWorkSessionsApiClient workSessionsApiClient,
        IVacationApiClient vacationApiClient,
        ISettingsApiClient settingsApiClient,
        IOfflineAuthService authService,
        INetworkMonitor networkMonitor,
        IConflictResolver conflictResolver,
        IConfiguration configuration,
        ILogger<SyncService> logger)
    {
        _workSessionService = workSessionService;
        _vacationDayService = vacationDayService;
        _userSettingsService = userSettingsService;
        _workSessionsApiClient = workSessionsApiClient;
        _vacationApiClient = vacationApiClient;
        _settingsApiClient = settingsApiClient;
        _authService = authService;
        _networkMonitor = networkMonitor;
        _conflictResolver = conflictResolver;
        _configuration = configuration;
        _logger = logger;
        _syncIntervalSeconds = _configuration.GetValue("SyncSettings:IntervalSeconds", 30);

        _networkMonitor.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsSyncing { get; private set; }

    public event EventHandler? SyncCompleted;
    public event EventHandler<string>? SyncError;

    public async Task SyncAsync()
    {
        if (IsSyncing)
        {
            _logger.LogInformation("Sync already in progress, skipping");
            return;
        }

        // Check if authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            _logger.LogInformation("User not authenticated, skipping sync");
            return;
        }

        // Check network connectivity
        var isConnected = await _networkMonitor.CheckConnectivityAsync();
        if (!isConnected)
        {
            _logger.LogInformation("No network connectivity, skipping sync");
            return;
        }

        IsSyncing = true;

        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UserId is null, cannot sync");
                return;
            }

            _logger.LogInformation("Starting sync for user {UserId}", userId);

            // Sync WorkSessions
            await SyncWorkSessionsAsync(userId);

            // Sync VacationDays
            await SyncVacationDaysAsync(userId);

            // Sync UserSettings
            await SyncUserSettingsAsync(userId);

            _logger.LogInformation("Sync completed successfully");
            SyncCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync");
            SyncError?.Invoke(this, ex.Message);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    public async Task<bool> HasPendingSyncsAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdOrAnonymousAsync();
            var pendingSessions = await _workSessionService.GetPendingSyncAsync(userId);
            var pendingDays = await _vacationDayService.GetPendingSyncAsync(userId);

            return pendingSessions.Any() || pendingDays.Any();
        }
        catch
        {
            return false;
        }
    }

    public async Task StartPeriodicSyncAsync()
    {
        var hasPending = await HasPendingSyncsAsync();
        if (!hasPending)
        {
            _logger.LogInformation("No pending syncs, not starting background sync");
            return;
        }

        if (_syncTimer != null)
        {
            _logger.LogInformation("Background sync already running");
            return;
        }

        _logger.LogInformation("Starting background sync with {Interval}s interval", _syncIntervalSeconds);
        _syncTimer = new Timer(TimeSpan.FromSeconds(_syncIntervalSeconds).TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = true
        };

        _syncTimer.Elapsed += async (_, _) =>
        {
            try
            {
                var stillHasPending = await HasPendingSyncsAsync();
                if (!stillHasPending)
                {
                    _logger.LogInformation("No more pending syncs, stopping background sync");
                    StopPeriodicSync();
                    return;
                }

                await SyncAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background sync error");
            }
        };
    }

    public void StopPeriodicSync()
    {
        if (_syncTimer != null)
        {
            _logger.LogInformation("Stopping background sync");
            _syncTimer.Stop();
            _syncTimer.Dispose();
            _syncTimer = null;
        }
    }

    public void Dispose()
    {
        StopPeriodicSync();
        _networkMonitor.ConnectivityChanged -= OnConnectivityChanged;
    }

    private async Task SyncWorkSessionsAsync(string userId)
    {
        try
        {
            // Get local pending sessions
            var pendingSessions = await _workSessionService.GetPendingSyncAsync(userId);
            _logger.LogInformation("Found {Count} pending work sessions", pendingSessions.Count);

            List<WorkSessionModel> backendSessions;

            if (pendingSessions.Any())
            {
                var syncRequest = new SyncWorkSessionsRequest
                {
                    WorkSessions = pendingSessions.Select(s => new CreateWorkSessionRequest
                    {
                        Id = s.Id,
                        Date = s.Date,
                        StartTime = s.StartTime,
                        StopTime = s.StopTime
                    }).ToList()
                };

                backendSessions = await _workSessionsApiClient.SyncWorkSessionsAsync(syncRequest);
                _logger.LogInformation("Synced {Count} pending sessions, received {BackendCount} from backend",
                    pendingSessions.Count, backendSessions.Count);
            }
            else
            {
                backendSessions = await _workSessionsApiClient.GetWorkSessionsAsync();
                _logger.LogInformation("No pending sessions, fetched {Count} from backend", backendSessions.Count);
            }

            // Merge backend sessions into local database
            var localSessions = await _workSessionService.GetByUserIdAsync(userId);
            var localDict = localSessions.ToDictionary(s => s.Id);

            foreach (var backendModel in backendSessions)
                if (localDict.TryGetValue(backendModel.Id, out var localEntity))
                {
                    // Resolve conflict
                    var resolved = await _conflictResolver.ResolveWorkSessionConflictAsync(localEntity, backendModel);
                    await _workSessionService.UpdateAsync(resolved);
                }
                else
                {
                    // New session from backend
                    var entity = new WorkSessionEntity
                    {
                        Id = backendModel.Id,
                        UserId = userId,
                        Date = backendModel.Date,
                        StartTime = backendModel.StartTime,
                        StopTime = backendModel.StopTime,
                        CreatedAt = backendModel.CreatedAt,
                        UpdatedAt = backendModel.UpdatedAt,
                        SyncedAt = backendModel.SyncedAt ?? DateTime.UtcNow,
                        IsSynced = true,
                        IsPendingSync = false,
                        IsFinished = backendModel.StopTime.HasValue // Backend sessions are finished if StopTime is set
                    };
                    await _workSessionService.AddAsync(entity);
                }

            // Mark pending sessions as synced
            foreach (var pending in pendingSessions)
                if (backendSessions.Any(b => b.Id == pending.Id))
                {
                    pending.IsSynced = true;
                    pending.IsPendingSync = false;
                    pending.SyncedAt = DateTime.UtcNow;
                    await _workSessionService.UpdateAsync(pending);
                }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing work sessions");
            throw;
        }
    }

    private async Task SyncVacationDaysAsync(string userId)
    {
        try
        {
            // Get local pending vacation days
            var pendingDays = await _vacationDayService.GetPendingSyncAsync(userId);
            _logger.LogInformation("Found {Count} pending vacation days", pendingDays.Count);

            // Always fetch backend first
            var backendDays = await _vacationApiClient.GetVacationDaysAsync();
            _logger.LogInformation("Fetched {Count} vacation days from backend", backendDays.Count);

            if (pendingDays.Any())
            {
                var allDaysForSync = new List<VacationDayDto>();

                // Add backend days
                foreach (var backendDay in backendDays)
                    allDaysForSync.Add(new VacationDayDto
                    {
                        Id = backendDay.Id,
                        Date = backendDay.Date,
                        CreatedAt = backendDay.CreatedAt,
                        UpdatedAt = backendDay.UpdatedAt,
                        SyncedAt = backendDay.SyncedAt
                    });

                // Add local pending days
                foreach (var pendingDay in pendingDays)
                    if (!backendDays.Any(b => b.Id == pendingDay.Id))
                        allDaysForSync.Add(new VacationDayDto
                        {
                            Id = pendingDay.Id,
                            Date = pendingDay.Date,
                            CreatedAt = pendingDay.CreatedAt,
                            UpdatedAt = pendingDay.UpdatedAt,
                            SyncedAt = pendingDay.SyncedAt
                        });

                var syncRequest = new SyncVacationDaysRequest
                {
                    VacationDays = allDaysForSync
                };

                var response = await _vacationApiClient.SyncVacationDaysAsync(syncRequest);
                backendDays = response.ServerVacationDays;
                _logger.LogInformation("Synced {Count} pending days, received {BackendCount} from backend",
                    pendingDays.Count, backendDays.Count);
            }

            // Merge backend days into local database
            var localDays = await _vacationDayService.GetByUserIdAsync(userId);
            var localDict = localDays.ToDictionary(d => d.Id);

            foreach (var backendModel in backendDays)
                if (localDict.TryGetValue(backendModel.Id, out var localEntity))
                {
                    var resolved = await _conflictResolver.ResolveVacationDayConflictAsync(localEntity, backendModel);
                    await _vacationDayService.UpdateAsync(resolved);
                }
                else
                {
                    var entity = new VacationDayEntity
                    {
                        Id = backendModel.Id,
                        UserId = userId,
                        Date = backendModel.Date,
                        CreatedAt = backendModel.CreatedAt,
                        UpdatedAt = backendModel.UpdatedAt,
                        SyncedAt = backendModel.SyncedAt ?? DateTime.UtcNow,
                        IsSynced = true,
                        IsPendingSync = false
                    };
                    await _vacationDayService.AddAsync(entity);
                }

            // Mark pending days as synced
            foreach (var pending in pendingDays)
                if (backendDays.Any(b => b.Id == pending.Id))
                {
                    pending.IsSynced = true;
                    pending.IsPendingSync = false;
                    pending.SyncedAt = DateTime.UtcNow;
                    await _vacationDayService.UpdateAsync(pending);
                }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing vacation days");
            throw;
        }
    }

    private async Task SyncUserSettingsAsync(string userId)
    {
        try
        {
            var backendSettings = await _settingsApiClient.GetUserSettingsAsync();
            var localSettings = await _userSettingsService.GetOrCreateAsync(userId);

            var resolved = await _conflictResolver.ResolveUserSettingsConflictAsync(localSettings, backendSettings);
            await _userSettingsService.UpdateAsync(resolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user settings");
            throw;
        }
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            _logger.LogInformation("Network connectivity restored, checking for pending syncs");
            _ = Task.Run(async () =>
            {
                var hasPending = await HasPendingSyncsAsync();
                if (hasPending) await StartPeriodicSyncAsync();
            });
        }
        else
        {
            _logger.LogInformation("Network connectivity lost");
        }
    }
}
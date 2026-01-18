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
        var syncStartTime = DateTime.UtcNow;
        
        if (IsSyncing)
        {
            _logger.LogWarning("[Sync] Sync already in progress, skipping duplicate request");
            return;
        }

        _logger.LogDebug("[Sync] SyncAsync called - checking prerequisites");

        // Check if authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            _logger.LogInformation("[Sync] User not authenticated, skipping sync");
            return;
        }
        _logger.LogDebug("[Sync] Authentication check passed");

        // Check network connectivity
        var isConnected = await _networkMonitor.CheckConnectivityAsync();
        if (!isConnected)
        {
            _logger.LogInformation("[Sync] No network connectivity, skipping sync");
            return;
        }
        _logger.LogDebug("[Sync] Network connectivity check passed");

        IsSyncing = true;
        _logger.LogInformation("[Sync] ===== Starting sync operation =====");

        try
        {
            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("[Sync] UserId is null or empty, cannot sync");
                return;
            }

            _logger.LogInformation("[Sync] Starting sync for user {UserId}", userId);

            // Sync WorkSessions
            var workSessionStartTime = DateTime.UtcNow;
            _logger.LogDebug("[Sync] Starting WorkSessions sync");
            await SyncWorkSessionsAsync(userId);
            var workSessionDuration = (DateTime.UtcNow - workSessionStartTime).TotalMilliseconds;
            _logger.LogInformation("[Sync] WorkSessions sync completed in {Duration}ms", workSessionDuration);

            // Sync VacationDays
            var vacationStartTime = DateTime.UtcNow;
            _logger.LogDebug("[Sync] Starting VacationDays sync");
            await SyncVacationDaysAsync(userId);
            var vacationDuration = (DateTime.UtcNow - vacationStartTime).TotalMilliseconds;
            _logger.LogInformation("[Sync] VacationDays sync completed in {Duration}ms", vacationDuration);

            // Sync UserSettings
            var settingsStartTime = DateTime.UtcNow;
            _logger.LogDebug("[Sync] Starting UserSettings sync");
            await SyncUserSettingsAsync(userId);
            var settingsDuration = (DateTime.UtcNow - settingsStartTime).TotalMilliseconds;
            _logger.LogInformation("[Sync] UserSettings sync completed in {Duration}ms", settingsDuration);

            var totalDuration = (DateTime.UtcNow - syncStartTime).TotalMilliseconds;
            _logger.LogInformation("[Sync] ===== Sync completed successfully in {TotalDuration}ms =====", totalDuration);
            _logger.LogDebug("[Sync] Sync breakdown - WorkSessions: {WorkDuration}ms, VacationDays: {VacationDuration}ms, Settings: {SettingsDuration}ms",
                workSessionDuration, vacationDuration, settingsDuration);
            
            SyncCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            var totalDuration = (DateTime.UtcNow - syncStartTime).TotalMilliseconds;
            _logger.LogError(ex, "[Sync] ===== Error during sync after {Duration}ms =====", totalDuration);
            _logger.LogError(ex, "[Sync] Exception details - Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
            SyncError?.Invoke(this, ex.Message);
        }
        finally
        {
            IsSyncing = false;
            _logger.LogDebug("[Sync] Sync operation finished, IsSyncing set to false");
        }
    }

    public async Task<bool> HasPendingSyncsAsync()
    {
        try
        {
            _logger.LogDebug("[Sync] Checking for pending syncs");
            var userId = await _authService.GetUserIdOrAnonymousAsync();
            var pendingSessions = await _workSessionService.GetPendingSyncAsync(userId);
            var pendingDays = await _vacationDayService.GetPendingSyncAsync(userId);

            var hasPending = pendingSessions.Any() || pendingDays.Any();
            _logger.LogDebug("[Sync] Pending syncs check - WorkSessions: {SessionCount}, VacationDays: {DayCount}, HasPending: {HasPending}",
                pendingSessions.Count, pendingDays.Count, hasPending);
            
            return hasPending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] Error checking for pending syncs");
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
            _logger.LogDebug("[Sync] [WorkSessions] Starting sync for user {UserId}", userId);
            
            // Get local pending sessions
            var pendingSessions = await _workSessionService.GetPendingSyncAsync(userId);
            _logger.LogInformation("[Sync] [WorkSessions] Found {Count} pending work sessions", pendingSessions.Count);

            List<WorkSessionModel> backendSessions;

            if (pendingSessions.Any())
            {
                _logger.LogDebug("[Sync] [WorkSessions] Preparing sync request with {Count} sessions", pendingSessions.Count);
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

                _logger.LogDebug("[Sync] [WorkSessions] Calling API: SyncWorkSessionsAsync");
                var apiStartTime = DateTime.UtcNow;
                backendSessions = await _workSessionsApiClient.SyncWorkSessionsAsync(syncRequest);
                var apiDuration = (DateTime.UtcNow - apiStartTime).TotalMilliseconds;
                _logger.LogInformation("[Sync] [WorkSessions] API call completed in {Duration}ms - Synced {PendingCount} pending sessions, received {BackendCount} from backend",
                    apiDuration, pendingSessions.Count, backendSessions.Count);
            }
            else
            {
                _logger.LogDebug("[Sync] [WorkSessions] No pending sessions, fetching all from backend");
                var apiStartTime = DateTime.UtcNow;
                backendSessions = await _workSessionsApiClient.GetWorkSessionsAsync();
                var apiDuration = (DateTime.UtcNow - apiStartTime).TotalMilliseconds;
                _logger.LogInformation("[Sync] [WorkSessions] API call completed in {Duration}ms - Fetched {Count} sessions from backend",
                    apiDuration, backendSessions.Count);
            }

            // Merge backend sessions into local database
            _logger.LogDebug("[Sync] [WorkSessions] Merging backend sessions into local database");
            var localSessions = await _workSessionService.GetByUserIdAsync(userId);
            var localDict = localSessions.ToDictionary(s => s.Id);
            _logger.LogDebug("[Sync] [WorkSessions] Found {LocalCount} local sessions", localSessions.Count);

            int conflictsResolved = 0;
            int newSessionsAdded = 0;
            int sessionsUpdated = 0;

            foreach (var backendModel in backendSessions)
            {
                if (localDict.TryGetValue(backendModel.Id, out var localEntity))
                {
                    // Resolve conflict
                    _logger.LogDebug("[Sync] [WorkSessions] Conflict detected for session {SessionId} - resolving", backendModel.Id);
                    var resolved = await _conflictResolver.ResolveWorkSessionConflictAsync(localEntity, backendModel);
                    await _workSessionService.UpdateAsync(resolved);
                    conflictsResolved++;
                    sessionsUpdated++;
                }
                else
                {
                    // New session from backend
                    _logger.LogDebug("[Sync] [WorkSessions] Adding new session {SessionId} from backend", backendModel.Id);
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
                    newSessionsAdded++;
                }
            }

            _logger.LogInformation("[Sync] [WorkSessions] Merge completed - Conflicts resolved: {Conflicts}, New sessions: {New}, Updated: {Updated}",
                conflictsResolved, newSessionsAdded, sessionsUpdated);

            // Mark pending sessions as synced
            int markedAsSynced = 0;
            foreach (var pending in pendingSessions)
            {
                if (backendSessions.Any(b => b.Id == pending.Id))
                {
                    _logger.LogDebug("[Sync] [WorkSessions] Marking session {SessionId} as synced", pending.Id);
                    pending.IsSynced = true;
                    pending.IsPendingSync = false;
                    pending.SyncedAt = DateTime.UtcNow;
                    await _workSessionService.UpdateAsync(pending);
                    markedAsSynced++;
                }
            }

            _logger.LogInformation("[Sync] [WorkSessions] Marked {Count} pending sessions as synced", markedAsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] [WorkSessions] Error syncing work sessions - Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    private async Task SyncVacationDaysAsync(string userId)
    {
        try
        {
            _logger.LogDebug("[Sync] [VacationDays] Starting sync for user {UserId}", userId);
            
            // Get local pending vacation days
            var pendingDays = await _vacationDayService.GetPendingSyncAsync(userId);
            _logger.LogInformation("[Sync] [VacationDays] Found {Count} pending vacation days", pendingDays.Count);

            // Always fetch backend first
            _logger.LogDebug("[Sync] [VacationDays] Fetching vacation days from backend");
            var apiStartTime = DateTime.UtcNow;
            var backendDays = await _vacationApiClient.GetVacationDaysAsync();
            var apiDuration = (DateTime.UtcNow - apiStartTime).TotalMilliseconds;
            _logger.LogInformation("[Sync] [VacationDays] API call completed in {Duration}ms - Fetched {Count} vacation days from backend",
                apiDuration, backendDays.Count);

            if (pendingDays.Any())
            {
                _logger.LogDebug("[Sync] [VacationDays] Preparing sync request with {PendingCount} pending days and {BackendCount} backend days",
                    pendingDays.Count, backendDays.Count);
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

                _logger.LogDebug("[Sync] [VacationDays] Calling API: SyncVacationDaysAsync with {TotalCount} days", allDaysForSync.Count);
                var syncApiStartTime = DateTime.UtcNow;
                var response = await _vacationApiClient.SyncVacationDaysAsync(syncRequest);
                var syncApiDuration = (DateTime.UtcNow - syncApiStartTime).TotalMilliseconds;
                backendDays = response.ServerVacationDays;
                _logger.LogInformation("[Sync] [VacationDays] Sync API call completed in {Duration}ms - Synced {PendingCount} pending days, received {BackendCount} from backend",
                    syncApiDuration, pendingDays.Count, backendDays.Count);
            }

            // Merge backend days into local database
            _logger.LogDebug("[Sync] [VacationDays] Merging backend days into local database");
            var localDays = await _vacationDayService.GetByUserIdAsync(userId);
            var localDict = localDays.ToDictionary(d => d.Id);
            _logger.LogDebug("[Sync] [VacationDays] Found {LocalCount} local vacation days", localDays.Count);

            int conflictsResolved = 0;
            int newDaysAdded = 0;
            int daysUpdated = 0;

            foreach (var backendModel in backendDays)
            {
                if (localDict.TryGetValue(backendModel.Id, out var localEntity))
                {
                    _logger.LogDebug("[Sync] [VacationDays] Conflict detected for day {DayId} ({Date}) - resolving", backendModel.Id, backendModel.Date);
                    var resolved = await _conflictResolver.ResolveVacationDayConflictAsync(localEntity, backendModel);
                    await _vacationDayService.UpdateAsync(resolved);
                    conflictsResolved++;
                    daysUpdated++;
                }
                else
                {
                    _logger.LogDebug("[Sync] [VacationDays] Adding new vacation day {DayId} ({Date}) from backend", backendModel.Id, backendModel.Date);
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
                    newDaysAdded++;
                }
            }

            _logger.LogInformation("[Sync] [VacationDays] Merge completed - Conflicts resolved: {Conflicts}, New days: {New}, Updated: {Updated}",
                conflictsResolved, newDaysAdded, daysUpdated);

            // Mark pending days as synced
            int markedAsSynced = 0;
            foreach (var pending in pendingDays)
            {
                if (backendDays.Any(b => b.Id == pending.Id))
                {
                    _logger.LogDebug("[Sync] [VacationDays] Marking day {DayId} ({Date}) as synced", pending.Id, pending.Date);
                    pending.IsSynced = true;
                    pending.IsPendingSync = false;
                    pending.SyncedAt = DateTime.UtcNow;
                    await _vacationDayService.UpdateAsync(pending);
                    markedAsSynced++;
                }
            }

            _logger.LogInformation("[Sync] [VacationDays] Marked {Count} pending days as synced", markedAsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] [VacationDays] Error syncing vacation days - Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    private async Task SyncUserSettingsAsync(string userId)
    {
        try
        {
            _logger.LogDebug("[Sync] [UserSettings] Starting sync for user {UserId}", userId);
            
            _logger.LogDebug("[Sync] [UserSettings] Fetching settings from backend");
            var apiStartTime = DateTime.UtcNow;
            var backendSettings = await _settingsApiClient.GetUserSettingsAsync();
            var apiDuration = (DateTime.UtcNow - apiStartTime).TotalMilliseconds;
            _logger.LogDebug("[Sync] [UserSettings] API call completed in {Duration}ms", apiDuration);
            
            _logger.LogDebug("[Sync] [UserSettings] Getting or creating local settings");
            var localSettings = await _userSettingsService.GetOrCreateAsync(userId);

            _logger.LogDebug("[Sync] [UserSettings] Resolving conflicts between local and backend settings");
            var resolved = await _conflictResolver.ResolveUserSettingsConflictAsync(localSettings, backendSettings);
            await _userSettingsService.UpdateAsync(resolved);
            
            _logger.LogInformation("[Sync] [UserSettings] Settings sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] [UserSettings] Error syncing user settings - Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);
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
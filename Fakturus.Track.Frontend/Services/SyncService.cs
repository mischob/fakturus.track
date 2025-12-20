using Fakturus.Track.Frontend.Models;
using Timer = System.Timers.Timer;

namespace Fakturus.Track.Frontend.Services;

public class SyncService(ILocalStorageService localStorageService, IWorkSessionsApiClient apiClient)
    : ISyncService, IDisposable
{
    private const int SyncIntervalSeconds = 30;
    private Timer? _syncTimer;

    public void Dispose()
    {
        StopPeriodicSync();
    }

    public event EventHandler? SyncCompleted;

    public async Task SyncAsync()
    {
        try
        {
            // Step 1: Get local sessions
            var localSessions = await localStorageService.GetWorkSessionsAsync();
            Console.WriteLine($"SyncService.SyncAsync: Found {localSessions.Count} local sessions");

            // Step 2: Identify pending sessions (need to be synced to backend)
            // Only sync finished sessions - current/in-edit sessions should not be synced
            var pendingSessions = localSessions
                .Where(s => s.IsPendingSync && !s.IsSynced && s.IsFinished)
                .ToList();

            Console.WriteLine($"SyncService.SyncAsync: Found {pendingSessions.Count} pending sessions to sync");

            // Step 3: Push pending sessions to backend (if any) and get all backend sessions
            List<WorkSessionModel> backendSessions;

            if (pendingSessions.Any())
            {
                var syncRequest = new SyncWorkSessionsRequest
                {
                    WorkSessions = pendingSessions.Select(s => new CreateWorkSessionRequest
                    {
                        Id = s.Id, // Include client-generated UUID
                        Date = s.Date,
                        StartTime = s.StartTime,
                        StopTime = s.StopTime
                    }).ToList()
                };

                // Backend will upsert and return ALL user's sessions
                backendSessions = await apiClient.SyncWorkSessionsAsync(syncRequest);
                Console.WriteLine(
                    $"SyncService.SyncAsync: Synced pending sessions, received {backendSessions.Count} sessions from backend");
            }
            else
            {
                // No pending sessions, just fetch all from backend
                backendSessions = await apiClient.GetWorkSessionsAsync();
                Console.WriteLine(
                    $"SyncService.SyncAsync: No pending sessions, fetched {backendSessions.Count} sessions from backend");
            }

            // Step 4: Merge logic - Backend is source of truth
            var mergedSessions = new Dictionary<Guid, WorkSessionModel>();

            // Add all backend sessions (marked as synced and finished)
            foreach (var backendSession in backendSessions)
                mergedSessions[backendSession.Id] = new WorkSessionModel
                {
                    Id = backendSession.Id,
                    UserId = backendSession.UserId,
                    Date = backendSession.Date,
                    StartTime = backendSession.StartTime,
                    StopTime = backendSession.StopTime,
                    CreatedAt = backendSession.CreatedAt,
                    UpdatedAt = backendSession.UpdatedAt,
                    SyncedAt = backendSession.SyncedAt,
                    IsSynced = true,
                    IsPendingSync = false,
                    IsFinished = true // Backend sessions are always finished
                };

            // Add local pending sessions that failed to sync (keep for retry)
            // Also preserve unfinished sessions (current/in-edit) in local storage
            foreach (var localSession in localSessions)
                if (localSession is { IsPendingSync: true, IsSynced: false } &&
                    mergedSessions.TryAdd(localSession.Id, localSession))
                {
                    if (localSession.IsFinished)
                        // Session is finished but still pending (sync might have failed)
                        Console.WriteLine(
                            $"SyncService.SyncAsync: Keeping pending finished session {localSession.Id} for retry");
                    else
                        // Session is unfinished (current/in-edit) - preserve in local storage
                        Console.WriteLine(
                            $"SyncService.SyncAsync: Keeping unfinished session {localSession.Id} in local storage");
                }

            // Step 5: Save merged sessions to local storage
            var finalSessions = mergedSessions.Values.OrderByDescending(s => s.Date).ThenByDescending(s => s.StartTime)
                .ToList();
            Console.WriteLine($"SyncService.SyncAsync: Saving {finalSessions.Count} merged sessions to local storage");

            await localStorageService.SaveWorkSessionsAsync(finalSessions);

            // Step 6: Check if we still have pending finished sessions and manage background sync
            var stillPending = finalSessions.Any(s => s.IsPendingSync && !s.IsSynced && s.IsFinished);
            if (!stillPending && _syncTimer != null)
            {
                Console.WriteLine("SyncService.SyncAsync: No pending sessions, stopping background sync");
                StopPeriodicSync();
            }

            // Notify subscribers that sync completed
            SyncCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Log error and re-throw so UI can show error
            Console.WriteLine($"Sync error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> HasPendingSyncsAsync()
    {
        var localSessions = await localStorageService.GetWorkSessionsAsync();
        return localSessions.Any(s => s.IsPendingSync && !s.IsSynced && s.IsFinished);
    }

    public async Task StartPeriodicSyncAsync()
    {
        // Only start if there are pending syncs
        var hasPending = await HasPendingSyncsAsync();
        if (!hasPending)
        {
            Console.WriteLine("SyncService.StartPeriodicSyncAsync: No pending syncs, not starting background sync");
            return;
        }

        if (_syncTimer != null)
        {
            Console.WriteLine("SyncService.StartPeriodicSyncAsync: Background sync already running");
            return;
        }

        Console.WriteLine(
            $"SyncService.StartPeriodicSyncAsync: Starting background sync with {SyncIntervalSeconds}s interval");
        _syncTimer = new Timer(TimeSpan.FromSeconds(SyncIntervalSeconds).TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = true
        };

        _syncTimer.Elapsed += async (_, e) =>
        {
            try
            {
                await SyncAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background sync error: {ex.Message}");
                // Continue running timer for retry
            }
        };
    }

    public void StopPeriodicSync()
    {
        if (_syncTimer != null)
        {
            Console.WriteLine("SyncService.StopPeriodicSync: Stopping background sync");
            _syncTimer.Stop();
            _syncTimer.Dispose();
            _syncTimer = null;
        }
    }
}
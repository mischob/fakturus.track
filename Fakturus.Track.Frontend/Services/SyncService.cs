using Fakturus.Track.Frontend.Models;
using Timer = System.Timers.Timer;

namespace Fakturus.Track.Frontend.Services;

public class SyncService(ILocalStorageService localStorageService, IWorkSessionsApiClient apiClient)
    : ISyncService, IDisposable
{
    private Timer? _syncTimer;
    private const int SyncIntervalMinutes = 5;

    public async Task SyncAsync()
    {
        try
        {
            // Step 1: Fetch all sessions from backend
            var backendSessions = await apiClient.GetWorkSessionsAsync();
            
            // Step 2: Get local sessions
            var localSessions = await localStorageService.GetWorkSessionsAsync();
            
            // Step 3: Convert backend DTOs to Models
            var backendModels = backendSessions.Select(bs => new WorkSessionModel
            {
                Id = bs.Id,
                UserId = bs.UserId,
                Date = bs.Date,
                StartTime = bs.StartTime,
                StopTime = bs.StopTime,
                CreatedAt = bs.CreatedAt,
                UpdatedAt = bs.UpdatedAt,
                SyncedAt = bs.SyncedAt,
                IsSynced = true,
                IsPendingSync = false
            }).ToList();
            
            // Step 4: Merge backend and local sessions
            var mergedSessions = new Dictionary<Guid, WorkSessionModel>();
            
            // Add all backend sessions first (they are the source of truth for synced data)
            foreach (var backendModel in backendModels)
            {
                mergedSessions[backendModel.Id] = backendModel;
            }
            
            // Add/update with local sessions
            foreach (var localSession in localSessions)
            {
                if (localSession.IsPendingSync && !localSession.IsSynced)
                {
                    // Local pending changes - keep them to push to backend
                    // If backend already has this ID, we'll update it after sync
                    mergedSessions[localSession.Id] = localSession;
                }
                else if (!mergedSessions.ContainsKey(localSession.Id))
                {
                    // Local-only session (not synced yet) - add it
                    mergedSessions[localSession.Id] = localSession;
                }
                // If session exists in both and local is synced, backend version already added above wins
            }
            
            // Step 5: Push pending local sessions to backend
            var pendingSessions = mergedSessions.Values
                .Where(s => s.IsPendingSync && !s.IsSynced)
                .ToList();
            
            if (pendingSessions.Any())
            {
                var syncRequest = new SyncWorkSessionsRequest
                {
                    WorkSessions = pendingSessions.Select(s => new CreateWorkSessionRequest
                    {
                        Date = s.Date,
                        StartTime = s.StartTime,
                        StopTime = s.StopTime
                    }).ToList()
                };

                var syncedSessions = await apiClient.SyncWorkSessionsAsync(syncRequest);
                
                // Update merged sessions with synced data
                foreach (var syncedSession in syncedSessions)
                {
                    // Find the matching pending session by comparing data (since ID might change)
                    var matchingPending = pendingSessions.FirstOrDefault(s => 
                        s.Date == syncedSession.Date &&
                        Math.Abs((s.StartTime - syncedSession.StartTime).TotalSeconds) < 1 &&
                        ((s.StopTime == null && syncedSession.StopTime == null) ||
                         (s.StopTime.HasValue && syncedSession.StopTime.HasValue && 
                          Math.Abs((s.StopTime.Value - syncedSession.StopTime.Value).TotalSeconds) < 1)));
                    
                    if (matchingPending != null)
                    {
                        // Remove old entry if ID changed
                        if (matchingPending.Id != syncedSession.Id)
                        {
                            mergedSessions.Remove(matchingPending.Id);
                        }
                        
                        // Add/update with synced data
                        var syncedModel = new WorkSessionModel
                        {
                            Id = syncedSession.Id,
                            UserId = syncedSession.UserId,
                            Date = syncedSession.Date,
                            StartTime = syncedSession.StartTime,
                            StopTime = syncedSession.StopTime,
                            CreatedAt = syncedSession.CreatedAt,
                            UpdatedAt = syncedSession.UpdatedAt,
                            SyncedAt = syncedSession.SyncedAt,
                            IsSynced = true,
                            IsPendingSync = false
                        };
                        mergedSessions[syncedModel.Id] = syncedModel;
                    }
                    else
                    {
                        // New session from backend (shouldn't happen, but handle it)
                        var syncedModel = new WorkSessionModel
                        {
                            Id = syncedSession.Id,
                            UserId = syncedSession.UserId,
                            Date = syncedSession.Date,
                            StartTime = syncedSession.StartTime,
                            StopTime = syncedSession.StopTime,
                            CreatedAt = syncedSession.CreatedAt,
                            UpdatedAt = syncedSession.UpdatedAt,
                            SyncedAt = syncedSession.SyncedAt,
                            IsSynced = true,
                            IsPendingSync = false
                        };
                        mergedSessions[syncedModel.Id] = syncedModel;
                    }
                }
            }
            
            // Step 6: Save merged sessions to local storage (bulk update to prevent duplicates)
            await localStorageService.SaveWorkSessionsAsync(mergedSessions.Values.ToList());
        }
        catch (Exception ex)
        {
            // Log error and re-throw so UI can show error
            Console.WriteLine($"Sync error: {ex.Message}");
            throw;
        }
    }

    public Task StartPeriodicSyncAsync()
    {
        if (_syncTimer != null)
            return Task.CompletedTask;

        _syncTimer = new Timer(TimeSpan.FromMinutes(SyncIntervalMinutes).TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = true
        };

        _syncTimer.Elapsed += async (sender, e) => await SyncAsync();

        return Task.CompletedTask;
    }

    public void StopPeriodicSync()
    {
        _syncTimer?.Stop();
        _syncTimer?.Dispose();
        _syncTimer = null;
    }

    public void Dispose()
    {
        StopPeriodicSync();
    }
}


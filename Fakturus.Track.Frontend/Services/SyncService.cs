using Fakturus.Track.Frontend.Models;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Fakturus.Track.Frontend.Services;

public class SyncService : ISyncService, IDisposable
{
    private readonly ILocalStorageService _localStorageService;
    private readonly IWorkSessionsApiClient _apiClient;
    private Timer? _syncTimer;
    private const int SyncIntervalMinutes = 5;

    public SyncService(ILocalStorageService localStorageService, IWorkSessionsApiClient apiClient)
    {
        _localStorageService = localStorageService;
        _apiClient = apiClient;
    }

    public async Task SyncAsync()
    {
        try
        {
            var pendingSessions = await _localStorageService.GetPendingSyncWorkSessionsAsync();
            if (!pendingSessions.Any())
                return;

            var syncRequest = new SyncWorkSessionsRequest
            {
                WorkSessions = pendingSessions.Select(s => new CreateWorkSessionRequest
                {
                    Date = s.Date,
                    StartTime = s.StartTime,
                    StopTime = s.StopTime
                }).ToList()
            };

            var syncedSessions = await _apiClient.SyncWorkSessionsAsync(syncRequest);

            // Mark local sessions as synced
            foreach (var session in pendingSessions)
            {
                await _localStorageService.MarkAsSyncedAsync(session.Id);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - sync will retry later
            Console.WriteLine($"Sync error: {ex.Message}");
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


namespace Fakturus.Track.Mobile.Services.Offline;

public interface ISyncService : IDisposable
{
    bool IsSyncing { get; }
    event EventHandler? SyncCompleted;
    event EventHandler<string>? SyncError;
    Task SyncAsync();
    Task<bool> HasPendingSyncsAsync();
    Task StartPeriodicSyncAsync();
    void StopPeriodicSync();
}
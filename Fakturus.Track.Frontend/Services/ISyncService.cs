namespace Fakturus.Track.Frontend.Services;

public interface ISyncService
{
    event EventHandler? SyncCompleted;
    Task SyncAsync();
    Task StartPeriodicSyncAsync();
    void StopPeriodicSync();
    Task<bool> HasPendingSyncsAsync();
}
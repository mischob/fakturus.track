namespace Fakturus.Track.Frontend.Services;

public interface ISyncService
{
    Task SyncAsync();
    Task StartPeriodicSyncAsync();
    void StopPeriodicSync();
    Task<bool> HasPendingSyncsAsync();
}


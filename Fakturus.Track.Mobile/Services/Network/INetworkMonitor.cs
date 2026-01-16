namespace Fakturus.Track.Mobile.Services.Network;

public interface INetworkMonitor
{
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
    void StartMonitoring();
    void StopMonitoring();
}

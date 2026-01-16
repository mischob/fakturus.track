using Microsoft.Maui.Networking;

namespace Fakturus.Track.Mobile.Services.Network;

public class NetworkMonitor : INetworkMonitor, IDisposable
{
    private bool _isMonitoring;
    
    public bool IsConnected { get; private set; }
    
    public event EventHandler<bool>? ConnectivityChanged;

    public NetworkMonitor()
    {
        IsConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        _isMonitoring = true; // Start monitoring immediately
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        var currentAccess = Connectivity.Current.NetworkAccess;
        var isConnected = currentAccess == NetworkAccess.Internet;
        
        if (IsConnected != isConnected)
        {
            IsConnected = isConnected;
            ConnectivityChanged?.Invoke(this, isConnected);
        }
        
        return isConnected;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring)
            return;
            
        _isMonitoring = true;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring)
            return;
            
        _isMonitoring = false;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isConnected = e.NetworkAccess == NetworkAccess.Internet;
        
        if (IsConnected != isConnected)
        {
            IsConnected = isConnected;
            ConnectivityChanged?.Invoke(this, isConnected);
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}

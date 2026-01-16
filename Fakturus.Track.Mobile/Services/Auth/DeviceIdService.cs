using Microsoft.Maui.Storage;

namespace Fakturus.Track.Mobile.Services.Auth;

public class DeviceIdService : IDeviceIdService
{
    private const string DeviceIdKey = "device_id";
    private string? _cachedDeviceId;

    public async Task<string> GetDeviceIdAsync()
    {
        if (!string.IsNullOrEmpty(_cachedDeviceId))
            return _cachedDeviceId;

        try
        {
            _cachedDeviceId = await SecureStorage.GetAsync(DeviceIdKey);
            if (string.IsNullOrEmpty(_cachedDeviceId))
            {
                _cachedDeviceId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync(DeviceIdKey, _cachedDeviceId);
            }
        }
        catch
        {
            // Fallback if SecureStorage fails
            _cachedDeviceId = Guid.NewGuid().ToString();
        }

        return _cachedDeviceId;
    }

    public string GetAnonymousUserId()
    {
        return $"anonymous_{_cachedDeviceId ?? Guid.NewGuid().ToString()}";
    }
}

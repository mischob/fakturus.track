namespace Fakturus.Track.Mobile.Services.Auth;

public interface IDeviceIdService
{
    Task<string> GetDeviceIdAsync();
    string GetAnonymousUserId();
}

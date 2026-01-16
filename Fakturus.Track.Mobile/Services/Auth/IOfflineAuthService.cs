namespace Fakturus.Track.Mobile.Services.Auth;

public interface IOfflineAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserIdAsync();
    Task<string> GetUserIdOrAnonymousAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> LoginAsync();
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    bool IsAnonymousMode { get; }
    event EventHandler<bool>? AuthenticationStateChanged;
}

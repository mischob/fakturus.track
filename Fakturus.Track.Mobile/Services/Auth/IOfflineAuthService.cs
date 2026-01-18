namespace Fakturus.Track.Mobile.Services.Auth;

public interface IOfflineAuthService
{
    bool IsAnonymousMode { get; }
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserIdAsync();
    Task<string> GetUserIdOrAnonymousAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> LoginAsync();
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    event EventHandler<bool>? AuthenticationStateChanged;
}
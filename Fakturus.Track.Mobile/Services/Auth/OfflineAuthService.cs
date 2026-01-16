using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Auth;

public class OfflineAuthService : IOfflineAuthService, IDisposable
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string UserIdKey = "user_id";
    private const string TokenExpiryKey = "token_expiry";

    private readonly IConfiguration _configuration;
    private readonly IDeviceIdService _deviceIdService;
    private readonly ILogger<OfflineAuthService> _logger;
    private IPublicClientApplication? _publicClientApp;
    private string? _cachedUserId;
    private bool _isAnonymousMode = true;

    public bool IsAnonymousMode => _isAnonymousMode;
    public event EventHandler<bool>? AuthenticationStateChanged;

    public OfflineAuthService(
        IConfiguration configuration,
        IDeviceIdService deviceIdService,
        ILogger<OfflineAuthService> logger)
    {
        _configuration = configuration;
        _deviceIdService = deviceIdService;
        _logger = logger;
    }

    private IPublicClientApplication GetPublicClientApp()
    {
        if (_publicClientApp != null)
            return _publicClientApp;

        var clientId = _configuration["AzureAdB2C:ClientId"];
        var authority = _configuration["AzureAdB2C:Authority"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(authority))
            throw new InvalidOperationException("Azure AD B2C configuration is missing");

        var builder = PublicClientApplicationBuilder
            .Create(clientId)
            .WithB2CAuthority(authority)
            .WithRedirectUri($"msal{clientId}://auth");

        _publicClientApp = builder.Build();
        return _publicClientApp;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var expiry = await SecureStorage.GetAsync(TokenExpiryKey);
            if (string.IsNullOrEmpty(expiry))
                return false;

            if (DateTime.TryParse(expiry, out var expiryDate))
            {
                if (expiryDate <= DateTime.UtcNow.AddMinutes(5))
                {
                    // Token expires soon, try to refresh
                    return await RefreshTokenAsync();
                }
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public async Task<string?> GetUserIdAsync()
    {
        if (_cachedUserId != null)
            return _cachedUserId;

        try
        {
            _cachedUserId = await SecureStorage.GetAsync(UserIdKey);
            if (!string.IsNullOrEmpty(_cachedUserId))
            {
                _isAnonymousMode = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user ID");
        }

        return _cachedUserId;
    }

    public async Task<string> GetUserIdOrAnonymousAsync()
    {
        var userId = await GetUserIdAsync();
        if (!string.IsNullOrEmpty(userId))
            return userId;

        var deviceId = await _deviceIdService.GetDeviceIdAsync();
        return _deviceIdService.GetAnonymousUserId();
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(AccessTokenKey);
            if (string.IsNullOrEmpty(token))
                return null;

            var expiry = await SecureStorage.GetAsync(TokenExpiryKey);
            if (string.IsNullOrEmpty(expiry))
                return null;

            if (DateTime.TryParse(expiry, out var expiryDate))
            {
                if (expiryDate <= DateTime.UtcNow.AddMinutes(5))
                {
                    // Token expires soon, try to refresh
                    await RefreshTokenAsync();
                    return await SecureStorage.GetAsync(AccessTokenKey);
                }
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token");
            return null;
        }
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            var app = GetPublicClientApp();
            var scopes = new[] { _configuration["AzureAdB2C:ApiScope"] ?? "" };

            var result = await app.AcquireTokenInteractive(scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                await SaveTokensAsync(result);
                _cachedUserId = result.Account?.HomeAccountId?.ObjectId ?? result.Account?.Username ?? "";
                _isAnonymousMode = false;
                AuthenticationStateChanged?.Invoke(this, true);
                return true;
            }

            return false;
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "MSAL error during login");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var app = GetPublicClientApp();
            var accounts = await app.GetAccountsAsync();
            
            foreach (var account in accounts)
            {
                await app.RemoveAsync(account);
            }

            await ClearTokensAsync();
            _cachedUserId = null;
            _isAnonymousMode = true;
            AuthenticationStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var app = GetPublicClientApp();
            var accounts = await app.GetAccountsAsync();
            
            if (accounts == null || !accounts.Any())
                return false;

            var scopes = new[] { _configuration["AzureAdB2C:ApiScope"] ?? "" };
            var result = await app.AcquireTokenSilent(scopes, accounts.First())
                .ExecuteAsync();

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                await SaveTokensAsync(result);
                return true;
            }

            return false;
        }
        catch (MsalUiRequiredException)
        {
            // User needs to login again
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }

    private async Task SaveTokensAsync(AuthenticationResult result)
    {
        try
        {
            await SecureStorage.SetAsync(AccessTokenKey, result.AccessToken);
            await SecureStorage.SetAsync(UserIdKey, result.Account?.HomeAccountId?.ObjectId ?? result.Account?.Username ?? "");
            
            await SecureStorage.SetAsync(TokenExpiryKey, result.ExpiresOn.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tokens");
            throw;
        }
    }

    private async Task ClearTokensAsync()
    {
        try
        {
            SecureStorage.Remove(AccessTokenKey);
            SecureStorage.Remove(RefreshTokenKey);
            SecureStorage.Remove(UserIdKey);
            SecureStorage.Remove(TokenExpiryKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tokens");
        }
    }

    public void Dispose()
    {
        _publicClientApp = null;
    }
}

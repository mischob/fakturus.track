using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
#if IOS
using UIKit;
#endif

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
    private string? _cachedUserId;
    private IPublicClientApplication? _publicClientApp;

    public OfflineAuthService(
        IConfiguration configuration,
        IDeviceIdService deviceIdService,
        ILogger<OfflineAuthService> logger)
    {
        _configuration = configuration;
        _deviceIdService = deviceIdService;
        _logger = logger;
    }

    public void Dispose()
    {
        _publicClientApp = null;
    }

    public bool IsAnonymousMode { get; private set; } = true;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            _logger.LogDebug("[Auth] Checking authentication status");
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("[Auth] No access token found - user not authenticated");
                return false;
            }

            var expiry = await SecureStorage.GetAsync(TokenExpiryKey);
            if (string.IsNullOrEmpty(expiry))
            {
                _logger.LogWarning("[Auth] Access token found but no expiry date - authentication invalid");
                return false;
            }

            if (DateTime.TryParse(expiry, out var expiryDate))
            {
                var timeUntilExpiry = expiryDate - DateTime.UtcNow;
                _logger.LogDebug("[Auth] Token expiry: {ExpiryDate}, Time until expiry: {TimeUntilExpiry}", expiryDate, timeUntilExpiry);
                
                if (expiryDate <= DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogInformation("[Auth] Token expires soon ({TimeUntilExpiry}), attempting refresh", timeUntilExpiry);
                    // Token expires soon, try to refresh
                    var refreshed = await RefreshTokenAsync();
                    _logger.LogDebug("[Auth] Token refresh result: {Refreshed}", refreshed);
                    return refreshed;
                }
                
                _logger.LogDebug("[Auth] Token is valid - user authenticated");
                return true;
            }

            _logger.LogWarning("[Auth] Could not parse token expiry date: {Expiry}", expiry);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error checking authentication status - Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<string?> GetUserIdAsync()
    {
        if (_cachedUserId != null)
        {
            _logger.LogDebug("[Auth] Returning cached user ID");
            return _cachedUserId;
        }

        try
        {
            _logger.LogDebug("[Auth] Retrieving user ID from SecureStorage");
            _cachedUserId = await SecureStorage.GetAsync(UserIdKey);
            if (!string.IsNullOrEmpty(_cachedUserId))
            {
                IsAnonymousMode = false;
                _logger.LogDebug("[Auth] User ID retrieved: {UserId} (AnonymousMode: {IsAnonymous})", _cachedUserId, IsAnonymousMode);
            }
            else
            {
                _logger.LogDebug("[Auth] No user ID found in SecureStorage - AnonymousMode: {IsAnonymous}", IsAnonymousMode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error getting user ID from SecureStorage - Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);
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
            _logger.LogDebug("[Auth] Retrieving access token from SecureStorage");
            var token = await SecureStorage.GetAsync(AccessTokenKey);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("[Auth] No access token found in SecureStorage");
                return null;
            }

            var expiry = await SecureStorage.GetAsync(TokenExpiryKey);
            if (string.IsNullOrEmpty(expiry))
            {
                _logger.LogWarning("[Auth] Access token found but no expiry date");
                return null;
            }

            if (DateTime.TryParse(expiry, out var expiryDate))
            {
                var timeUntilExpiry = expiryDate - DateTime.UtcNow;
                _logger.LogDebug("[Auth] Token expiry: {ExpiryDate}, Time until expiry: {TimeUntilExpiry}", expiryDate, timeUntilExpiry);
                
                if (expiryDate <= DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogInformation("[Auth] Token expires soon ({TimeUntilExpiry}), refreshing token", timeUntilExpiry);
                    // Token expires soon, try to refresh
                    var refreshed = await RefreshTokenAsync();
                    if (refreshed)
                    {
                        token = await SecureStorage.GetAsync(AccessTokenKey);
                        _logger.LogDebug("[Auth] Token refreshed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("[Auth] Token refresh failed");
                    }
                    return token;
                }

                _logger.LogDebug("[Auth] Token is still valid");
                return token;
            }

            _logger.LogWarning("[Auth] Could not parse token expiry date: {Expiry}", expiry);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error getting access token from SecureStorage - Exception: {ExceptionType}, Message: {Message}",
                ex.GetType().Name, ex.Message);
            return null;
        }
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            _logger.LogInformation("[Auth] ===== Starting login process =====");
            var app = GetPublicClientApp();
            var scopes = new[] { _configuration["AzureAdB2C:ApiScope"] ?? "" };
            _logger.LogDebug("[Auth] Using scopes: {Scopes}", string.Join(", ", scopes));

            _logger.LogDebug("[Auth] Initiating interactive token acquisition");
            var loginStartTime = DateTime.UtcNow;
            
            var builder = app.AcquireTokenInteractive(scopes)
                .WithPrompt(Prompt.SelectAccount);
            
#if IOS
            // On iOS, we need to provide the parent view controller
            // Try to get the topmost view controller
            var window = UIApplication.SharedApplication.KeyWindow;
            var rootViewController = window?.RootViewController;
            
            // Navigate to the topmost view controller (handles navigation controllers, tab controllers, etc.)
            while (rootViewController?.PresentedViewController != null)
            {
                rootViewController = rootViewController.PresentedViewController;
            }
            
            if (rootViewController != null)
            {
                _logger.LogDebug("[Auth] Using view controller for iOS parent: {ViewControllerType}", rootViewController.GetType().Name);
                builder = builder.WithParentActivityOrWindow(rootViewController);
            }
            else
            {
                _logger.LogWarning("[Auth] No view controller found, MSAL may not work correctly on iOS");
            }
#endif
            
            var result = await builder.ExecuteAsync();
            var loginDuration = (DateTime.UtcNow - loginStartTime).TotalMilliseconds;

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                _logger.LogInformation("[Auth] Login successful in {Duration}ms", loginDuration);
                _logger.LogDebug("[Auth] Token details - ExpiresOn: {ExpiresOn}, Account: {AccountId}",
                    result.ExpiresOn, result.Account?.HomeAccountId?.ObjectId ?? result.Account?.Username ?? "Unknown");
                
                await SaveTokensAsync(result);
                _cachedUserId = result.Account?.HomeAccountId?.ObjectId ?? result.Account?.Username ?? "";
                IsAnonymousMode = false;
                _logger.LogInformation("[Auth] User ID set: {UserId}, AnonymousMode: {IsAnonymous}", _cachedUserId, IsAnonymousMode);
                
                AuthenticationStateChanged?.Invoke(this, true);
                _logger.LogInformation("[Auth] ===== Login process completed successfully =====");
                return true;
            }

            _logger.LogWarning("[Auth] Login completed but no access token received");
            return false;
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "[Auth] MSAL error during login - ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}, CorrelationId: {CorrelationId}",
                ex.ErrorCode, ex.Message, ex.CorrelationId);
            _logger.LogError(ex, "[Auth] MSAL exception details - Type: {ExceptionType}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.StackTrace);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error during login - Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("[Auth] ===== Starting logout process =====");
            var app = GetPublicClientApp();
            var accounts = await app.GetAccountsAsync();
            _logger.LogDebug("[Auth] Found {AccountCount} accounts to remove", accounts?.Count() ?? 0);

            if (accounts != null && accounts.Any())
            {
                foreach (var account in accounts)
                {
                    _logger.LogDebug("[Auth] Removing account: {AccountId}", account.HomeAccountId?.ObjectId ?? account.Username ?? "Unknown");
                    await app.RemoveAsync(account);
                }
            }

            await ClearTokensAsync();
            _cachedUserId = null;
            IsAnonymousMode = true;
            _logger.LogInformation("[Auth] Tokens cleared, UserId reset, AnonymousMode: {IsAnonymous}", IsAnonymousMode);
            
            AuthenticationStateChanged?.Invoke(this, false);
            _logger.LogInformation("[Auth] ===== Logout process completed successfully =====");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error during logout - Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            _logger.LogInformation("[Auth] ===== Starting token refresh =====");
            var app = GetPublicClientApp();
            var accounts = await app.GetAccountsAsync();

            if (accounts == null || !accounts.Any())
            {
                _logger.LogWarning("[Auth] No accounts found for token refresh");
                return false;
            }

            var account = accounts.First();
            _logger.LogDebug("[Auth] Using account: {AccountId} for silent token acquisition",
                account.HomeAccountId?.ObjectId ?? account.Username ?? "Unknown");

            var scopes = new[] { _configuration["AzureAdB2C:ApiScope"] ?? "" };
            _logger.LogDebug("[Auth] Acquiring token silently with scopes: {Scopes}", string.Join(", ", scopes));
            
            var refreshStartTime = DateTime.UtcNow;
            var result = await app.AcquireTokenSilent(scopes, account)
                .ExecuteAsync();
            var refreshDuration = (DateTime.UtcNow - refreshStartTime).TotalMilliseconds;

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                _logger.LogInformation("[Auth] Token refresh successful in {Duration}ms", refreshDuration);
                _logger.LogDebug("[Auth] New token expires on: {ExpiresOn}", result.ExpiresOn);
                await SaveTokensAsync(result);
                _logger.LogInformation("[Auth] ===== Token refresh completed successfully =====");
                return true;
            }

            _logger.LogWarning("[Auth] Token refresh completed but no access token received");
            return false;
        }
        catch (MsalUiRequiredException ex)
        {
            _logger.LogWarning(ex, "[Auth] User needs to login again - ErrorCode: {ErrorCode}, Message: {Message}",
                ex.ErrorCode, ex.Message);
            return false;
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "[Auth] MSAL error during token refresh - ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}, CorrelationId: {CorrelationId}",
                ex.ErrorCode, ex.Message, ex.CorrelationId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error refreshing token - Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
            return false;
        }
    }

    private IPublicClientApplication GetPublicClientApp()
    {
        if (_publicClientApp != null)
        {
            _logger.LogDebug("[Auth] Returning cached PublicClientApplication instance");
            return _publicClientApp;
        }

        _logger.LogDebug("[Auth] Creating new PublicClientApplication instance");
        var clientId = _configuration["AzureAdB2C:ClientId"];
        var authority = _configuration["AzureAdB2C:Authority"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(authority))
        {
            _logger.LogError("[Auth] Azure AD B2C configuration is missing - ClientId: {HasClientId}, Authority: {HasAuthority}",
                !string.IsNullOrEmpty(clientId), !string.IsNullOrEmpty(authority));
            throw new InvalidOperationException("Azure AD B2C configuration is missing");
        }

        _logger.LogDebug("[Auth] Building PublicClientApplication - ClientId: {ClientId}, Authority: {Authority}",
            clientId, authority);
        
        var redirectUri = $"msal{clientId}://auth";
        _logger.LogDebug("[Auth] Using redirect URI: {RedirectUri}", redirectUri);

        var builder = PublicClientApplicationBuilder
            .Create(clientId)
            .WithB2CAuthority(authority)
            .WithRedirectUri(redirectUri);

#if IOS
        // Set the iOS keychain security group to match the app's bundle identifier
        // This must match the keychain-access-groups in Entitlements.plist
        var keychainSecurityGroup = "com.fakturus.track.mobile";
        _logger.LogDebug("[Auth] Setting iOS keychain security group: {KeychainGroup}", keychainSecurityGroup);
        builder = builder.WithIosKeychainSecurityGroup(keychainSecurityGroup);
#endif

        _publicClientApp = builder.Build();
        _logger.LogDebug("[Auth] PublicClientApplication created successfully");
        return _publicClientApp;
    }

    private async Task SaveTokensAsync(AuthenticationResult result)
    {
        try
        {
            _logger.LogDebug("[Auth] Saving tokens to SecureStorage");
            var userId = result.Account?.HomeAccountId?.ObjectId ?? result.Account?.Username ?? "";
            var expiry = result.ExpiresOn.ToString("O");
            
            await SecureStorage.SetAsync(AccessTokenKey, result.AccessToken);
            _logger.LogDebug("[Auth] Access token saved to SecureStorage");
            
            await SecureStorage.SetAsync(UserIdKey, userId);
            _logger.LogDebug("[Auth] User ID saved to SecureStorage: {UserId}", userId);

            await SecureStorage.SetAsync(TokenExpiryKey, expiry);
            _logger.LogDebug("[Auth] Token expiry saved to SecureStorage: {Expiry}", expiry);
            
            _logger.LogInformation("[Auth] All tokens saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error saving tokens to SecureStorage - Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
            throw;
        }
    }

    private async Task ClearTokensAsync()
    {
        try
        {
            _logger.LogDebug("[Auth] Clearing tokens from SecureStorage");
            SecureStorage.Remove(AccessTokenKey);
            _logger.LogDebug("[Auth] Access token removed");
            
            SecureStorage.Remove(RefreshTokenKey);
            _logger.LogDebug("[Auth] Refresh token removed");
            
            SecureStorage.Remove(UserIdKey);
            _logger.LogDebug("[Auth] User ID removed");
            
            SecureStorage.Remove(TokenExpiryKey);
            _logger.LogDebug("[Auth] Token expiry removed");
            
            _logger.LogInformation("[Auth] All tokens cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error clearing tokens from SecureStorage - Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
        }
    }
}
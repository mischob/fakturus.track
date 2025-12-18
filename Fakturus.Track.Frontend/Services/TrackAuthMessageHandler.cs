using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Fakturus.Track.Frontend.Services;

public class TrackAuthMessageHandler(IAccessTokenProvider tokenProvider, IConfiguration configuration)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the API scope from configuration
        var apiScope = configuration["AzureAdB2C:ApiScope"];
        if (string.IsNullOrEmpty(apiScope))
            throw new InvalidOperationException("AzureAdB2C:ApiScope is not configured in appsettings.json");

        // Request access token with the required scope to ensure proper token refresh
        var tokenRequestOptions = new AccessTokenRequestOptions
        {
            Scopes = new[] { apiScope }
        };

        try
        {
            // Get the access token from Azure AD B2C
            // This will automatically refresh the token if needed
            var tokenResult = await tokenProvider.RequestAccessToken(tokenRequestOptions);

            if (tokenResult.TryGetToken(out var token))
                // Add the Bearer token to the Authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            // Token is not available or needs refresh - redirect to login
            ex.Redirect();
            throw;
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If we get a 401, the token might have expired - try to refresh and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            try
            {
                // Request a fresh token
                var tokenResult = await tokenProvider.RequestAccessToken(tokenRequestOptions);

                if (tokenResult.TryGetToken(out var refreshedToken))
                {
                    // Retry the request with the refreshed token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken.Value);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                // Token refresh failed - redirect to login
                ex.Redirect();
                throw;
            }

        return response;
    }
}
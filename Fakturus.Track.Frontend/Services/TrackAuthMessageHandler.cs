using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Fakturus.Track.Frontend.Services;

public class TrackAuthMessageHandler(IAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the access token from Azure AD B2C
        var tokenResult = await tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out var token))
            // Add the Bearer token to the Authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return await base.SendAsync(request, cancellationToken);
    }
}


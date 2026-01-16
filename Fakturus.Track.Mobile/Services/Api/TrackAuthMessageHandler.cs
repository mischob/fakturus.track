using System.Net.Http;
using System.Net.Http.Headers;
using Fakturus.Track.Mobile.Services.Auth;

namespace Fakturus.Track.Mobile.Services.Api;

public class TrackAuthMessageHandler : DelegatingHandler
{
    private readonly IOfflineAuthService _authService;

    public TrackAuthMessageHandler(IOfflineAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If we get a 401, try to refresh token and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await _authService.RefreshTokenAsync();
            if (refreshed)
            {
                token = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
        }

        return response;
    }
}

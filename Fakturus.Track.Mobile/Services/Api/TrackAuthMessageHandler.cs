using System.Net;
using System.Net.Http.Headers;
using Fakturus.Track.Mobile.Services.Auth;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Api;

public class TrackAuthMessageHandler : DelegatingHandler
{
    private readonly IOfflineAuthService _authService;
    private readonly ILogger<TrackAuthMessageHandler> _logger;

    public TrackAuthMessageHandler(
        IOfflineAuthService authService,
        ILogger<TrackAuthMessageHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestStartTime = DateTime.UtcNow;
        var method = request.Method.Method;
        var uri = request.RequestUri?.ToString() ?? "Unknown";
        
        _logger.LogDebug("[API] {Method} {Uri} - Preparing request", method, uri);

        // Get access token
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("[API] {Method} {Uri} - Authorization header added", method, uri);
        }
        else
        {
            _logger.LogWarning("[API] {Method} {Uri} - No access token available, request will be unauthenticated", method, uri);
        }

        // Send request
        _logger.LogDebug("[API] {Method} {Uri} - Sending request", method, uri);
        var response = await base.SendAsync(request, cancellationToken);
        var requestDuration = (DateTime.UtcNow - requestStartTime).TotalMilliseconds;
        
        _logger.LogInformation("[API] {Method} {Uri} - Response: {StatusCode} ({StatusCodeInt}) in {Duration}ms",
            method, uri, response.StatusCode, (int)response.StatusCode, requestDuration);

        // If we get a 401, try to refresh token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("[API] {Method} {Uri} - Received 401 Unauthorized, attempting token refresh", method, uri);
            
            var refreshStartTime = DateTime.UtcNow;
            var refreshed = await _authService.RefreshTokenAsync();
            var refreshDuration = (DateTime.UtcNow - refreshStartTime).TotalMilliseconds;
            
            if (refreshed)
            {
                _logger.LogInformation("[API] {Method} {Uri} - Token refreshed successfully in {Duration}ms, retrying request", 
                    method, uri, refreshDuration);
                
                token = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("[API] {Method} {Uri} - Retry request with new token", method, uri);
                    
                    var retryStartTime = DateTime.UtcNow;
                    response = await base.SendAsync(request, cancellationToken);
                    var retryDuration = (DateTime.UtcNow - retryStartTime).TotalMilliseconds;
                    
                    _logger.LogInformation("[API] {Method} {Uri} - Retry response: {StatusCode} ({StatusCodeInt}) in {Duration}ms",
                        method, uri, response.StatusCode, (int)response.StatusCode, retryDuration);
                }
                else
                {
                    _logger.LogError("[API] {Method} {Uri} - Token refresh succeeded but no token available for retry", method, uri);
                }
            }
            else
            {
                _logger.LogError("[API] {Method} {Uri} - Token refresh failed after {Duration}ms, request will fail with 401", 
                    method, uri, refreshDuration);
            }
        }

        // Log error status codes
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("[API] {Method} {Uri} - Error response body: {ErrorContent}", method, uri, errorContent);
        }

        return response;
    }
}
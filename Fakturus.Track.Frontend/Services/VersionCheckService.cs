using System.Net.Http.Json;
using Fakturus.Track.Frontend.Models;
using Microsoft.JSInterop;
using Timer = System.Timers.Timer;

namespace Fakturus.Track.Frontend.Services;

public class VersionCheckService(HttpClient httpClient, IJSRuntime jsRuntime) : IVersionCheckService, IDisposable
{
    private const string VersionKey = "app_version";
    private const int CheckIntervalMinutes = 5;
    private Timer? _checkTimer;

    public void Dispose()
    {
        StopPeriodicCheck();
    }

    public async Task CheckVersionAsync()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<VersionResponse>("/v1/version");
            if (response?.Version == null) return;

            var storedVersion = await GetStoredVersionAsync();

            if (storedVersion != null && storedVersion != response.Version)
            {
                Console.WriteLine($"Version changed: {storedVersion} -> {response.Version}. Reloading...");
                // Store the new version BEFORE reloading to prevent endless loop
                await StoreVersionAsync(response.Version);
                await jsRuntime.InvokeVoidAsync("eval", "location.reload()");
            }
            else
            {
                await StoreVersionAsync(response.Version);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Version check failed: {ex.Message}");
        }
    }

    public Task StartPeriodicCheckAsync()
    {
        if (_checkTimer != null)
            return Task.CompletedTask;

        _checkTimer = new Timer(TimeSpan.FromMinutes(CheckIntervalMinutes).TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = true
        };

        _checkTimer.Elapsed += async (sender, e) => await CheckVersionAsync();

        return Task.CompletedTask;
    }

    public void StopPeriodicCheck()
    {
        _checkTimer?.Stop();
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    private async Task<string?> GetStoredVersionAsync()
    {
        try
        {
            return await jsRuntime.InvokeAsync<string>("localStorage.getItem", VersionKey);
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreVersionAsync(string version)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", VersionKey, version);
        }
        catch
        {
            // Ignore storage errors
        }
    }
}
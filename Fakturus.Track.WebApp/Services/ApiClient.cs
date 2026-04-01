using System.Net.Http.Json;
using System.Net.Http.Headers;
using Fakturus.Track.WebApp.Models;
using Microsoft.Identity.Web;

namespace Fakturus.Track.WebApp.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenAcquisition _tokenAcquisition;

    private static readonly string[] ApiScopes = new[]
    {
        "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
    };

    public ApiClient(HttpClient http, ITokenAcquisition tokenAcquisition)
    {
        _http = http;
        _tokenAcquisition = tokenAcquisition;
    }

    private async Task SetBearerTokenAsync()
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(ApiScopes);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch
        {
            // Token acquisition failed (user not logged in or token expired)
        }
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        await SetBearerTokenAsync();
        return await _http.GetFromJsonAsync<T>(path);
    }

    private async Task<TResponse?> PostAsync<TResponse, TBody>(string path, TBody body)
    {
        await SetBearerTokenAsync();
        var response = await _http.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    private async Task PutAsync<TBody>(string path, TBody body)
    {
        await SetBearerTokenAsync();
        var response = await _http.PutAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAsync(string path)
    {
        await SetBearerTokenAsync();
        var response = await _http.DeleteAsync(path);
        response.EnsureSuccessStatusCode();
    }

    // Work Sessions
    public async Task<List<WorkSession>> GetWorkSessionsAsync()
    {
        var result = await GetAsync<List<WorkSession>>("v1/work-sessions");
        return result ?? new();
    }

    public async Task<WorkSession?> CreateWorkSessionAsync(CreateWorkSessionRequest request)
    {
        return await PostAsync<WorkSession, CreateWorkSessionRequest>("v1/work-sessions", request);
    }

    public async Task UpdateWorkSessionAsync(Guid id, UpdateWorkSessionRequest request)
    {
        await PutAsync($"v1/work-sessions/{id}", request);
    }

    public async Task DeleteWorkSessionAsync(Guid id)
    {
        await DeleteAsync($"v1/work-sessions/{id}");
    }

    // Vacation Days
    public async Task<List<VacationDay>> GetVacationDaysAsync()
    {
        var result = await GetAsync<List<VacationDay>>("v1/vacation-days");
        return result ?? new();
    }

    public async Task<VacationDay?> CreateVacationDayAsync(CreateVacationDayRequest request)
    {
        return await PostAsync<VacationDay, CreateVacationDayRequest>("v1/vacation-days", request);
    }

    public async Task DeleteVacationDayAsync(Guid id)
    {
        await DeleteAsync($"v1/vacation-days/{id}");
    }

    // Settings
    public async Task<UserSettings> GetUserSettingsAsync()
    {
        var result = await GetAsync<UserSettings>("v1/settings");
        return result ?? new();
    }

    public async Task SaveUserSettingsAsync(UserSettings settings)
    {
        await PutAsync("v1/settings", settings);
    }

    // Overtime
    public async Task<OvertimeSummary> GetOvertimeSummaryAsync(int year)
    {
        var result = await GetAsync<OvertimeSummary>($"v1/overtime-summary?year={year}");
        return result ?? new();
    }

    // Legal / Consent (authenticated)
    public async Task<ConsentStatusResponse?> GetConsentStatusAsync()
    {
        try
        {
            return await GetAsync<ConsentStatusResponse>("api/legal/consent");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ConsentStatusResponse?> SubmitConsentAsync(ConsentSubmitRequest request)
    {
        return await PostAsync<ConsentStatusResponse, ConsentSubmitRequest>("api/legal/consent", request);
    }

    // Legal versions (public, no token needed)
    public async Task<LegalVersionsResponse?> GetLegalVersionsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<LegalVersionsResponse>("api/legal/versions");
        }
        catch
        {
            return null;
        }
    }

    // Account
    public async Task DeleteAccountAsync()
    {
        await DeleteAsync("api/account");
    }
}

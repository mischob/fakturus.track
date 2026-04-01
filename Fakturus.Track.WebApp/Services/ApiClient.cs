using Fakturus.Track.WebApp.Models;

namespace Fakturus.Track.WebApp.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<WorkSession>> GetWorkSessionsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<WorkSession>>("api/worksessions");
        return result ?? new();
    }

    public async Task<List<WorkSession>> GetWorkSessionsByMonthAsync(int year, int month)
    {
        var result = await _http.GetFromJsonAsync<List<WorkSession>>($"api/worksessions?year={year}&month={month}");
        return result ?? new();
    }

    public async Task<WorkSession?> GetTodaySessionAsync()
    {
        return await _http.GetFromJsonAsync<WorkSession?>("api/worksessions/today");
    }

    public async Task<WorkSession> StartSessionAsync()
    {
        var response = await _http.PostAsync("api/worksessions/start", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkSession>())!;
    }

    public async Task<WorkSession> StopSessionAsync(Guid id)
    {
        var response = await _http.PostAsync($"api/worksessions/{id}/stop", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkSession>())!;
    }

    public async Task<WorkSession> FinishSessionAsync(Guid id)
    {
        var response = await _http.PostAsync($"api/worksessions/{id}/finish", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkSession>())!;
    }

    public async Task<WorkSession> ResumeSessionAsync(Guid id)
    {
        var response = await _http.PostAsync($"api/worksessions/{id}/resume", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkSession>())!;
    }

    public async Task<List<VacationDay>> GetVacationDaysAsync(int year)
    {
        var result = await _http.GetFromJsonAsync<List<VacationDay>>($"api/vacation?year={year}");
        return result ?? new();
    }

    public async Task<VacationDay> AddVacationDayAsync(DateOnly date, VacationDayType type)
    {
        var response = await _http.PostAsJsonAsync("api/vacation", new { Date = date, Type = type });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VacationDay>())!;
    }

    public async Task DeleteVacationDayAsync(Guid id)
    {
        await _http.DeleteAsync($"api/vacation/{id}");
    }

    public async Task<UserSettings> GetUserSettingsAsync()
    {
        var result = await _http.GetFromJsonAsync<UserSettings>("api/settings");
        return result ?? new();
    }

    public async Task SaveUserSettingsAsync(UserSettings settings)
    {
        await _http.PutAsJsonAsync("api/settings", settings);
    }

    public async Task<OvertimeSummary> GetOvertimeSummaryAsync(int year)
    {
        var result = await _http.GetFromJsonAsync<OvertimeSummary>($"api/reports/overtime?year={year}");
        return result ?? new();
    }

    public async Task<string> GetLegalContentAsync(string type)
    {
        return await _http.GetStringAsync($"api/legal/{type}");
    }

    public async Task AcceptConsentsAsync()
    {
        await _http.PostAsync("api/consent/accept", null);
    }

    public async Task<bool> HasAcceptedConsentsAsync()
    {
        var result = await _http.GetFromJsonAsync<bool>("api/consent/status");
        return result;
    }

    public async Task DeleteAccountAsync()
    {
        await _http.DeleteAsync("api/account");
    }
}

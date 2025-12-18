using System.Text.Json;
using Fakturus.Track.Frontend.Models;
using Microsoft.JSInterop;

namespace Fakturus.Track.Frontend.Services;

public class LocalStorageService : ILocalStorageService
{
    private const string StorageKey = "workSessions";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveWorkSessionAsync(WorkSessionModel workSession)
    {
        var sessions = await GetWorkSessionsAsync();
        var existingIndex = sessions.FindIndex(s => s.Id == workSession.Id);

        if (existingIndex >= 0)
            sessions[existingIndex] = workSession;
        else
            sessions.Add(workSession);

        await SaveToStorageAsync(sessions);
    }

    public async Task SaveWorkSessionsAsync(List<WorkSessionModel> workSessions)
    {
        // Replace all sessions with the provided list
        // This is more efficient for bulk updates during sync
        await SaveToStorageAsync(workSessions);
    }

    public async Task<List<WorkSessionModel>> GetWorkSessionsAsync()
    {
        try
        {
            var json = await GetFromStorageAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new List<WorkSessionModel>();

            var sessions = JsonSerializer.Deserialize<List<WorkSessionModel>>(json, _jsonOptions);
            return sessions ?? new List<WorkSessionModel>();
        }
        catch
        {
            return new List<WorkSessionModel>();
        }
    }

    public async Task<WorkSessionModel?> GetWorkSessionByIdAsync(Guid id)
    {
        var sessions = await GetWorkSessionsAsync();
        return sessions.FirstOrDefault(s => s.Id == id);
    }

    public async Task DeleteWorkSessionAsync(Guid id)
    {
        var sessions = await GetWorkSessionsAsync();
        sessions.RemoveAll(s => s.Id == id);
        await SaveToStorageAsync(sessions);
    }

    public async Task<List<WorkSessionModel>> GetPendingSyncWorkSessionsAsync()
    {
        var sessions = await GetWorkSessionsAsync();
        return sessions.Where(s => s.IsPendingSync && !s.IsSynced).ToList();
    }

    public async Task MarkAsSyncedAsync(Guid id)
    {
        var session = await GetWorkSessionByIdAsync(id);
        if (session != null)
        {
            session.IsSynced = true;
            session.IsPendingSync = false;
            session.SyncedAt = DateTime.UtcNow;
            await SaveWorkSessionAsync(session);
        }
    }

    private async Task SaveToStorageAsync(List<WorkSessionModel> sessions)
    {
        var json = JsonSerializer.Serialize(sessions, _jsonOptions);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    private async Task<string> GetFromStorageAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey) ?? string.Empty;
    }
}
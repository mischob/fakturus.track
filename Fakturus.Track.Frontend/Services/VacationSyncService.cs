using System.Text.Json;
using Fakturus.Track.Frontend.Models;
using Microsoft.JSInterop;

namespace Fakturus.Track.Frontend.Services;

public class VacationSyncService(
    IVacationApiClient vacationApiClient,
    IJSRuntime jsRuntime,
    ILogger<VacationSyncService> logger) : IVacationSyncService
{
    private const string StorageKey = "vacationDays";
    private Timer? _syncTimer;
    private bool _isSyncing;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public event EventHandler? SyncCompleted;

    public async Task SyncAsync()
    {
        if (_isSyncing)
        {
            logger.LogInformation("Sync already in progress, skipping");
            return;
        }

        _isSyncing = true;

        try
        {
            logger.LogInformation("Starting vacation days sync...");

            // Get local vacation days
            var localVacationDays = await GetVacationDaysAsync();
            var pendingVacationDays = localVacationDays.Where(v => v.IsPendingSync && !v.IsSynced).ToList();

            if (!pendingVacationDays.Any())
            {
                logger.LogInformation("No pending vacation days to sync");
                return;
            }

            logger.LogInformation("Found {Count} pending vacation days to sync", pendingVacationDays.Count);

            // Prepare sync request
            var syncRequest = new SyncVacationDaysRequest
            {
                VacationDays = pendingVacationDays.Select(v => new VacationDayDto
                {
                    Id = v.Id,
                    Date = v.Date,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt,
                    SyncedAt = v.SyncedAt
                }).ToList()
            };

            // Call sync endpoint
            var response = await vacationApiClient.SyncVacationDaysAsync(syncRequest);

            logger.LogInformation("Sync response received: {ServerCount} vacation days from server, {DeletedCount} deleted",
                response.ServerVacationDays.Count, response.DeletedIds.Count);

            // Update local storage with server response
            var updatedVacationDays = new List<VacationDayModel>();

            // Add all server vacation days (marked as synced)
            foreach (var serverVacationDay in response.ServerVacationDays)
            {
                serverVacationDay.IsSynced = true;
                serverVacationDay.IsPendingSync = false;
                updatedVacationDays.Add(serverVacationDay);
            }

            // Save updated vacation days to local storage
            await SaveVacationDaysAsync(updatedVacationDays);

            logger.LogInformation("Vacation days sync completed successfully");

            // Notify listeners
            SyncCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during vacation days sync");
            throw;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public async Task StartPeriodicSyncAsync()
    {
        // Stop any existing timer
        StopPeriodicSync();

        // Check if there are pending vacation days
        var vacationDays = await GetVacationDaysAsync();
        var hasPending = vacationDays.Any(v => v.IsPendingSync && !v.IsSynced);

        if (!hasPending)
        {
            logger.LogInformation("No pending vacation days, periodic sync not started");
            return;
        }

        logger.LogInformation("Starting periodic vacation days sync (every 30 seconds)");

        // Start periodic sync every 30 seconds
        _syncTimer = new Timer(async _ =>
        {
            try
            {
                await SyncAsync();

                // Check if there are still pending vacation days
                var currentVacationDays = await GetVacationDaysAsync();
                var stillHasPending = currentVacationDays.Any(v => v.IsPendingSync && !v.IsSynced);

                if (!stillHasPending)
                {
                    logger.LogInformation("No more pending vacation days, stopping periodic sync");
                    StopPeriodicSync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during periodic vacation days sync");
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void StopPeriodicSync()
    {
        if (_syncTimer != null)
        {
            logger.LogInformation("Stopping periodic vacation days sync");
            _syncTimer.Dispose();
            _syncTimer = null;
        }
    }

    // Local storage methods
    public async Task SaveVacationDayAsync(VacationDayModel vacationDay)
    {
        var vacationDays = await GetVacationDaysAsync();
        var existingIndex = vacationDays.FindIndex(v => v.Id == vacationDay.Id);

        if (existingIndex >= 0)
            vacationDays[existingIndex] = vacationDay;
        else
            vacationDays.Add(vacationDay);

        await SaveToStorageAsync(vacationDays);
    }

    public async Task SaveVacationDaysAsync(List<VacationDayModel> vacationDays)
    {
        await SaveToStorageAsync(vacationDays);
    }

    public async Task<List<VacationDayModel>> GetVacationDaysAsync()
    {
        try
        {
            var json = await GetFromStorageAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new List<VacationDayModel>();

            var vacationDays = JsonSerializer.Deserialize<List<VacationDayModel>>(json, _jsonOptions);
            return vacationDays ?? new List<VacationDayModel>();
        }
        catch
        {
            return new List<VacationDayModel>();
        }
    }

    public async Task DeleteVacationDayAsync(Guid id)
    {
        var vacationDays = await GetVacationDaysAsync();
        vacationDays.RemoveAll(v => v.Id == id);
        await SaveToStorageAsync(vacationDays);
    }

    private async Task SaveToStorageAsync(List<VacationDayModel> vacationDays)
    {
        var json = JsonSerializer.Serialize(vacationDays, _jsonOptions);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    private async Task<string> GetFromStorageAsync()
    {
        return await jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey) ?? string.Empty;
    }
}


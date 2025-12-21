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

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private bool _isSyncing;
    private Timer? _syncTimer;

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

            // Step 1: Get local vacation days
            var localVacationDays = await GetVacationDaysAsync();
            logger.LogInformation("VacationSyncService.SyncAsync: Found {Count} local vacation days", localVacationDays.Count);

            // Step 2: Identify pending vacation days (need to be synced to backend)
            var pendingVacationDays = localVacationDays
                .Where(v => v.IsPendingSync && !v.IsSynced)
                .ToList();

            logger.LogInformation("VacationSyncService.SyncAsync: Found {Count} pending vacation days to sync", pendingVacationDays.Count);

            // Step 3: Push ALL local vacation days to backend (if any) and get all backend vacation days
            // Note: Backend uses this list to determine what exists - anything not in the list gets deleted
            List<VacationDayModel> backendVacationDays;

            if (localVacationDays.Any())
            {
                // Prepare sync request with ALL local vacation days (not just pending)
                // This prevents backend from deleting already-synced items
                var syncRequest = new SyncVacationDaysRequest
                {
                    VacationDays = localVacationDays.Select(v => new VacationDayDto
                    {
                        Id = v.Id,
                        Date = v.Date,
                        CreatedAt = v.CreatedAt,
                        UpdatedAt = v.UpdatedAt,
                        SyncedAt = v.SyncedAt
                    }).ToList()
                };

                // Backend will upsert and return ALL user's vacation days
                var response = await vacationApiClient.SyncVacationDaysAsync(syncRequest);
                backendVacationDays = response.ServerVacationDays;
                logger.LogInformation(
                    "VacationSyncService.SyncAsync: Synced {Total} vacation days ({Pending} pending), received {Count} vacation days from backend",
                    localVacationDays.Count, pendingVacationDays.Count, backendVacationDays.Count);
            }
            else
            {
                // No local vacation days, just fetch all from backend
                backendVacationDays = await vacationApiClient.GetVacationDaysAsync();
                logger.LogInformation(
                    "VacationSyncService.SyncAsync: No local vacation days, fetched {Count} vacation days from backend",
                    backendVacationDays.Count);
            }

            // Step 4: Merge logic - Backend is source of truth
            var mergedVacationDays = new Dictionary<Guid, VacationDayModel>();

            // Add all backend vacation days (marked as synced)
            foreach (var backendVacationDay in backendVacationDays)
            {
                mergedVacationDays[backendVacationDay.Id] = new VacationDayModel
                {
                    Id = backendVacationDay.Id,
                    Date = backendVacationDay.Date,
                    CreatedAt = backendVacationDay.CreatedAt,
                    UpdatedAt = backendVacationDay.UpdatedAt,
                    SyncedAt = backendVacationDay.SyncedAt,
                    IsSynced = true,
                    IsPendingSync = false
                };
            }

            // Add local pending vacation days that aren't in backend (keep for retry)
            foreach (var localVacationDay in localVacationDays)
            {
                if (localVacationDay is { IsPendingSync: true, IsSynced: false } &&
                    mergedVacationDays.TryAdd(localVacationDay.Id, localVacationDay))
                {
                    // Vacation day is pending but still not in backend (sync might have failed)
                    logger.LogInformation(
                        "VacationSyncService.SyncAsync: Keeping pending vacation day {Id} for retry",
                        localVacationDay.Id);
                }
            }

            // Step 5: Save merged vacation days to local storage
            var finalVacationDays = mergedVacationDays.Values
                .OrderBy(v => v.Date)
                .ToList();
            logger.LogInformation(
                "VacationSyncService.SyncAsync: Saving {Count} merged vacation days to local storage",
                finalVacationDays.Count);

            await SaveVacationDaysAsync(finalVacationDays);

            // Step 6: Check if we still have pending vacation days and manage background sync
            var stillPending = finalVacationDays.Any(v => v.IsPendingSync && !v.IsSynced);
            if (!stillPending && _syncTimer != null)
            {
                logger.LogInformation("VacationSyncService.SyncAsync: No pending vacation days, stopping background sync");
                StopPeriodicSync();
            }

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
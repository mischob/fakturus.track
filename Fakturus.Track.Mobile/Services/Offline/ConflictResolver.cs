using Fakturus.Track.Mobile.Data.Entities;
using Fakturus.Track.Mobile.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Offline;

public class ConflictResolver : IConflictResolver
{
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(ILogger<ConflictResolver> logger)
    {
        _logger = logger;
    }

    public Task<WorkSessionEntity> ResolveWorkSessionConflictAsync(
        WorkSessionEntity localEntity,
        WorkSessionModel backendModel)
    {
        // Backend is source of truth - use backend data
        // Preserve mobile-specific fields (CalendarEventId) from local entity
        var resolved = new WorkSessionEntity
        {
            Id = backendModel.Id,
            UserId = backendModel.UserId,
            Date = backendModel.Date,
            StartTime = backendModel.StartTime,
            StopTime = backendModel.StopTime,
            CalendarEventId = localEntity.CalendarEventId, // Preserve mobile-specific field
            CreatedAt = backendModel.CreatedAt,
            UpdatedAt = backendModel.UpdatedAt,
            SyncedAt = backendModel.SyncedAt ?? DateTime.UtcNow,
            IsSynced = true,
            IsPendingSync = false,
            IsFinished = backendModel.StopTime.HasValue
        };

        _logger.LogDebug("Resolved WorkSession conflict - Backend is source of truth, preserved CalendarEventId");
        return Task.FromResult(resolved);
    }

    public Task<VacationDayEntity> ResolveVacationDayConflictAsync(
        VacationDayEntity localEntity,
        VacationDayModel backendModel)
    {
        // Backend is source of truth - use backend data
        // Keep local userId in case of anonymous mode
        var resolved = new VacationDayEntity
        {
            Id = backendModel.Id,
            UserId = localEntity.UserId, // Preserve local userId
            Date = backendModel.Date,
            CreatedAt = backendModel.CreatedAt,
            UpdatedAt = backendModel.UpdatedAt,
            SyncedAt = backendModel.SyncedAt ?? DateTime.UtcNow,
            IsSynced = true,
            IsPendingSync = false
        };

        _logger.LogDebug("Resolved VacationDay conflict - Backend is source of truth");
        return Task.FromResult(resolved);
    }

    public Task<UserSettingsEntity> ResolveUserSettingsConflictAsync(
        UserSettingsEntity localEntity,
        UserSettingsModel backendModel)
    {
        // Last-Write-Wins: Compare UpdatedAt timestamps
        // If backend is newer, use backend; otherwise keep local
        var backendUpdatedAt = DateTime.UtcNow; // Backend model doesn't have UpdatedAt, assume now

        if (localEntity.UpdatedAt > backendUpdatedAt)
        {
            // Local is newer, keep local but mark as pending sync
            localEntity.IsPendingSync = true;
            _logger.LogInformation("Local UserSettings is newer, keeping local version");
            return Task.FromResult(localEntity);
        }

        // Backend is newer or equal, use backend
        localEntity.VacationDaysPerYear = backendModel.VacationDaysPerYear;
        localEntity.WorkHoursPerWeek = backendModel.WorkHoursPerWeek;
        localEntity.WorkDays = backendModel.WorkDays;
        localEntity.Bundesland = backendModel.Bundesland;
        localEntity.UpdatedAt = DateTime.UtcNow;
        localEntity.SyncedAt = DateTime.UtcNow;
        localEntity.IsSynced = true;
        localEntity.IsPendingSync = false;

        _logger.LogInformation("Backend UserSettings is newer, using backend version");
        return Task.FromResult(localEntity);
    }
}
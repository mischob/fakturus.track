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
        // WorkSessions are UUID-based, so conflicts shouldn't happen
        // Backend upserts, so backend is always source of truth
        var resolved = new WorkSessionEntity
        {
            Id = backendModel.Id,
            UserId = backendModel.UserId,
            Date = backendModel.Date,
            StartTime = backendModel.StartTime,
            StopTime = backendModel.StopTime,
            CreatedAt = backendModel.CreatedAt,
            UpdatedAt = backendModel.UpdatedAt,
            SyncedAt = backendModel.SyncedAt ?? DateTime.UtcNow,
            IsSynced = true,
            IsPendingSync = false,
            IsFinished = backendModel.StopTime.HasValue // Backend sessions are finished if StopTime is set
        };

        return Task.FromResult(resolved);
    }

    public Task<VacationDayEntity> ResolveVacationDayConflictAsync(
        VacationDayEntity localEntity, 
        VacationDayModel backendModel)
    {
        // VacationDays are UUID-based, so conflicts shouldn't happen
        // Backend merges, so backend is source of truth
        var resolved = new VacationDayEntity
        {
            Id = backendModel.Id,
            UserId = localEntity.UserId, // Keep local userId in case of anonymous mode
            Date = backendModel.Date,
            CreatedAt = backendModel.CreatedAt,
            UpdatedAt = backendModel.UpdatedAt,
            SyncedAt = backendModel.SyncedAt ?? DateTime.UtcNow,
            IsSynced = true,
            IsPendingSync = false
        };

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
        var resolved = new UserSettingsEntity
        {
            UserId = localEntity.UserId,
            CalendarUrl = localEntity.CalendarUrl, // Keep local calendar URL
            VacationDaysPerYear = backendModel.VacationDaysPerYear,
            WorkHoursPerWeek = backendModel.WorkHoursPerWeek,
            WorkDays = backendModel.WorkDays,
            Bundesland = backendModel.Bundesland,
            CreatedAt = localEntity.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            IsSynced = true,
            IsPendingSync = false
        };

        _logger.LogInformation("Backend UserSettings is newer, using backend version");
        return Task.FromResult(resolved);
    }
}

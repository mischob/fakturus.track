using Fakturus.Track.Mobile.Data.Entities;
using Fakturus.Track.Mobile.Shared.Models;

namespace Fakturus.Track.Mobile.Shared.Extensions;

public static class WorkSessionExtensions
{
    public static WorkSessionModel ToModel(this WorkSessionEntity entity)
    {
        return new WorkSessionModel
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Date = entity.Date,
            StartTime = entity.StartTime,
            StopTime = entity.StopTime,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            SyncedAt = entity.SyncedAt,
            IsSynced = entity.IsSynced,
            IsPendingSync = entity.IsPendingSync,
            IsFinished = entity.IsFinished
        };
    }

    public static WorkSessionEntity ToEntity(this WorkSessionModel model, string? calendarEventId = null)
    {
        return new WorkSessionEntity
        {
            Id = model.Id,
            UserId = model.UserId,
            Date = model.Date,
            StartTime = model.StartTime,
            StopTime = model.StopTime,
            CalendarEventId = calendarEventId,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            SyncedAt = model.SyncedAt,
            IsSynced = model.IsSynced,
            IsPendingSync = model.IsPendingSync,
            IsFinished = model.IsFinished
        };
    }

    public static List<WorkSessionModel> ToModels(this IEnumerable<WorkSessionEntity> entities)
    {
        return entities.Select(e => e.ToModel()).ToList();
    }

    public static List<WorkSessionEntity> ToEntities(this IEnumerable<WorkSessionModel> models,
        string? calendarEventId = null)
    {
        return models.Select(m => m.ToEntity(calendarEventId)).ToList();
    }
}
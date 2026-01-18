using Fakturus.Track.Mobile.Data.Entities;
using Fakturus.Track.Mobile.Shared.Models;

namespace Fakturus.Track.Mobile.Services.Offline;

public interface IConflictResolver
{
    Task<WorkSessionEntity> ResolveWorkSessionConflictAsync(
        WorkSessionEntity localEntity,
        WorkSessionModel backendModel);

    Task<VacationDayEntity> ResolveVacationDayConflictAsync(
        VacationDayEntity localEntity,
        VacationDayModel backendModel);

    Task<UserSettingsEntity> ResolveUserSettingsConflictAsync(
        UserSettingsEntity localEntity,
        UserSettingsModel backendModel);
}
using Fakturus.Track.Mobile.Data.Entities;

namespace Fakturus.Track.Mobile.Services.Offline;

public interface IVacationDayService : IOfflineDataService<VacationDayEntity>
{
    Task<List<VacationDayEntity>> GetByUserIdAsync(string userId);
    Task<List<VacationDayEntity>> GetByYearAsync(string userId, int year);
    Task<List<VacationDayEntity>> GetPendingSyncAsync(string userId);
    Task<List<VacationDayEntity>> GetSyncedAsync(string userId);
    Task<VacationDayEntity?> GetByDateAsync(string userId, DateOnly date);
}
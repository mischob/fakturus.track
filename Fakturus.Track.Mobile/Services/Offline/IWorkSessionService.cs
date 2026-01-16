using Fakturus.Track.Mobile.Data.Entities;

namespace Fakturus.Track.Mobile.Services.Offline;

public interface IWorkSessionService : IOfflineDataService<WorkSessionEntity>
{
    Task<List<WorkSessionEntity>> GetByUserIdAsync(string userId);
    Task<List<WorkSessionEntity>> GetByDateRangeAsync(string userId, DateOnly startDate, DateOnly endDate);
    Task<List<WorkSessionEntity>> GetPendingSyncAsync(string userId);
    Task<WorkSessionEntity?> GetByDateAsync(string userId, DateOnly date);
}

using Fakturus.Track.Mobile.Data.Entities;

namespace Fakturus.Track.Mobile.Services.Offline;

public interface ISyncQueueService : IOfflineDataService<SyncQueueEntity>
{
    Task<List<SyncQueueEntity>> GetPendingAsync(string userId);
    Task<SyncQueueEntity?> GetByEntityAsync(string entityType, Guid entityId, string userId);
    Task AddToQueueAsync(string entityType, Guid entityId, string operation, string userId);
    Task RemoveFromQueueAsync(string entityType, Guid entityId, string userId);
    Task IncrementRetryAsync(Guid queueId, string? errorMessage = null);
}

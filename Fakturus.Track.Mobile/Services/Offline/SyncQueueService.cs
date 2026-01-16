using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Mobile.Services.Offline;

public class SyncQueueService : OfflineDataService<SyncQueueEntity>, ISyncQueueService
{
    public SyncQueueService(MobileDbContext context) : base(context)
    {
    }

    public async Task<List<SyncQueueEntity>> GetPendingAsync(string userId)
    {
        return await Context.SyncQueue
            .Where(sq => sq.UserId == userId)
            .OrderBy(sq => sq.CreatedAt)
            .ToListAsync();
    }

    public async Task<SyncQueueEntity?> GetByEntityAsync(string entityType, Guid entityId, string userId)
    {
        return await Context.SyncQueue
            .FirstOrDefaultAsync(sq => sq.EntityType == entityType && 
                                      sq.EntityId == entityId && 
                                      sq.UserId == userId);
    }

    public async Task AddToQueueAsync(string entityType, Guid entityId, string operation, string userId)
    {
        var existing = await GetByEntityAsync(entityType, entityId, userId);
        if (existing != null)
        {
            existing.Operation = operation;
            existing.CreatedAt = DateTime.UtcNow;
            existing.RetryCount = 0;
            existing.ErrorMessage = null;
            await UpdateAsync(existing);
        }
        else
        {
            var queueItem = new SyncQueueEntity
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };
            await AddAsync(queueItem);
        }
    }

    public async Task RemoveFromQueueAsync(string entityType, Guid entityId, string userId)
    {
        var queueItem = await GetByEntityAsync(entityType, entityId, userId);
        if (queueItem != null)
        {
            await DeleteAsync(queueItem);
        }
    }

    public async Task IncrementRetryAsync(Guid queueId, string? errorMessage = null)
    {
        var queueItem = await GetByIdAsync(queueId);
        if (queueItem != null)
        {
            queueItem.RetryCount++;
            queueItem.LastRetryAt = DateTime.UtcNow;
            queueItem.ErrorMessage = errorMessage;
            await UpdateAsync(queueItem);
        }
    }
}

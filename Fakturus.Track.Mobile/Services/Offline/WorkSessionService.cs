using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Mobile.Services.Offline;

public class WorkSessionService : OfflineDataService<WorkSessionEntity>, IWorkSessionService
{
    public WorkSessionService(MobileDbContext context) : base(context)
    {
    }

    public async Task<List<WorkSessionEntity>> GetByUserIdAsync(string userId)
    {
        return await Context.WorkSessions
            .Where(ws => ws.UserId == userId)
            .OrderByDescending(ws => ws.Date)
            .ThenByDescending(ws => ws.StartTime)
            .ToListAsync();
    }

    public async Task<List<WorkSessionEntity>> GetByDateRangeAsync(string userId, DateOnly startDate, DateOnly endDate)
    {
        return await Context.WorkSessions
            .Where(ws => ws.UserId == userId && ws.Date >= startDate && ws.Date <= endDate)
            .OrderByDescending(ws => ws.Date)
            .ThenByDescending(ws => ws.StartTime)
            .ToListAsync();
    }

    public async Task<List<WorkSessionEntity>> GetPendingSyncAsync(string userId)
    {
        return await Context.WorkSessions
            .Where(ws => ws.UserId == userId && ws.IsPendingSync && !ws.IsSynced && ws.IsFinished)
            .ToListAsync();
    }

    public async Task<WorkSessionEntity?> GetByDateAsync(string userId, DateOnly date)
    {
        return await Context.WorkSessions
            .FirstOrDefaultAsync(ws => ws.UserId == userId && ws.Date == date);
    }
}
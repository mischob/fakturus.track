using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Offline;

public class WorkSessionService : OfflineDataService<WorkSessionEntity>, IWorkSessionService
{
    private readonly ILogger<WorkSessionService> _logger;

    public WorkSessionService(MobileDbContext context, ILogger<WorkSessionService> logger) 
        : base(context, logger)
    {
        _logger = logger;
    }

    public async Task<List<WorkSessionEntity>> GetByUserIdAsync(string userId)
    {
        _logger.LogDebug("[Database] [WorkSession] GetByUserIdAsync - UserId: {UserId}", userId);
        try
        {
            var result = await Context.WorkSessions
                .Where(ws => ws.UserId == userId)
                .OrderByDescending(ws => ws.Date)
                .ThenByDescending(ws => ws.StartTime)
                .ToListAsync();
            _logger.LogDebug("[Database] [WorkSession] GetByUserIdAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [WorkSession] Error in GetByUserIdAsync - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<WorkSessionEntity>> GetByDateRangeAsync(string userId, DateOnly startDate, DateOnly endDate)
    {
        _logger.LogDebug("[Database] [WorkSession] GetByDateRangeAsync - UserId: {UserId}, StartDate: {StartDate}, EndDate: {EndDate}",
            userId, startDate, endDate);
        try
        {
            var result = await Context.WorkSessions
                .Where(ws => ws.UserId == userId && ws.Date >= startDate && ws.Date <= endDate)
                .OrderByDescending(ws => ws.Date)
                .ThenByDescending(ws => ws.StartTime)
                .ToListAsync();
            _logger.LogDebug("[Database] [WorkSession] GetByDateRangeAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [WorkSession] Error in GetByDateRangeAsync - UserId: {UserId}, StartDate: {StartDate}, EndDate: {EndDate}",
                userId, startDate, endDate);
            throw;
        }
    }

    public async Task<List<WorkSessionEntity>> GetPendingSyncAsync(string userId)
    {
        _logger.LogDebug("[Database] [WorkSession] GetPendingSyncAsync - UserId: {UserId}", userId);
        try
        {
            var result = await Context.WorkSessions
                .Where(ws => ws.UserId == userId && ws.IsPendingSync && !ws.IsSynced && ws.IsFinished)
                .ToListAsync();
            _logger.LogDebug("[Database] [WorkSession] GetPendingSyncAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [WorkSession] Error in GetPendingSyncAsync - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<WorkSessionEntity?> GetByDateAsync(string userId, DateOnly date)
    {
        _logger.LogDebug("[Database] [WorkSession] GetByDateAsync - UserId: {UserId}, Date: {Date}", userId, date);
        try
        {
            var result = await Context.WorkSessions
                .FirstOrDefaultAsync(ws => ws.UserId == userId && ws.Date == date);
            _logger.LogDebug("[Database] [WorkSession] GetByDateAsync completed - Found: {Found}", result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [WorkSession] Error in GetByDateAsync - UserId: {UserId}, Date: {Date}", userId, date);
            throw;
        }
    }
}
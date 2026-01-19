using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Offline;

public class VacationDayService : OfflineDataService<VacationDayEntity>, IVacationDayService
{
    private readonly ILogger<VacationDayService> _logger;

    public VacationDayService(MobileDbContext context, ILogger<VacationDayService> logger) 
        : base(context, logger)
    {
        _logger = logger;
    }

    public async Task<List<VacationDayEntity>> GetByUserIdAsync(string userId)
    {
        _logger.LogDebug("[Database] [VacationDay] GetByUserIdAsync - UserId: {UserId}", userId);
        try
        {
            var result = await Context.VacationDays
                .AsNoTracking()
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.Date)
                .ToListAsync();
            _logger.LogDebug("[Database] [VacationDay] GetByUserIdAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [VacationDay] Error in GetByUserIdAsync - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<VacationDayEntity>> GetByYearAsync(string userId, int year)
    {
        _logger.LogDebug("[Database] [VacationDay] GetByYearAsync - UserId: {UserId}, Year: {Year}", userId, year);
        try
        {
            var result = await Context.VacationDays
                .AsNoTracking()
                .Where(v => v.UserId == userId && v.Date.Year == year)
                .OrderBy(v => v.Date)
                .ToListAsync();
            _logger.LogDebug("[Database] [VacationDay] GetByYearAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [VacationDay] Error in GetByYearAsync - UserId: {UserId}, Year: {Year}", userId, year);
            throw;
        }
    }

    public async Task<List<VacationDayEntity>> GetPendingSyncAsync(string userId)
    {
        _logger.LogDebug("[Database] [VacationDay] GetPendingSyncAsync - UserId: {UserId}", userId);
        try
        {
            var result = await Context.VacationDays
                .AsNoTracking()
                .Where(v => v.UserId == userId && v.IsPendingSync && !v.IsSynced)
                .ToListAsync();
            _logger.LogDebug("[Database] [VacationDay] GetPendingSyncAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [VacationDay] Error in GetPendingSyncAsync - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<VacationDayEntity>> GetSyncedAsync(string userId)
    {
        _logger.LogDebug("[Database] [VacationDay] GetSyncedAsync - UserId: {UserId}", userId);
        try
        {
            var result = await Context.VacationDays
                .AsNoTracking()
                .Where(v => v.UserId == userId && v.IsSynced)
                .ToListAsync();
            _logger.LogDebug("[Database] [VacationDay] GetSyncedAsync completed - Count: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [VacationDay] Error in GetSyncedAsync - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<VacationDayEntity?> GetByDateAsync(string userId, DateOnly date)
    {
        _logger.LogDebug("[Database] [VacationDay] GetByDateAsync - UserId: {UserId}, Date: {Date}", userId, date);
        try
        {
            var result = await Context.VacationDays
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId && v.Date == date);
            _logger.LogDebug("[Database] [VacationDay] GetByDateAsync completed - Found: {Found}", result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [VacationDay] Error in GetByDateAsync - UserId: {UserId}, Date: {Date}", userId, date);
            throw;
        }
    }
}
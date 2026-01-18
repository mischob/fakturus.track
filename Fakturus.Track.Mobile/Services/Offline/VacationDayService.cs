using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Mobile.Services.Offline;

public class VacationDayService : OfflineDataService<VacationDayEntity>, IVacationDayService
{
    public VacationDayService(MobileDbContext context) : base(context)
    {
    }

    public async Task<List<VacationDayEntity>> GetByUserIdAsync(string userId)
    {
        return await Context.VacationDays
            .Where(v => v.UserId == userId)
            .OrderBy(v => v.Date)
            .ToListAsync();
    }

    public async Task<List<VacationDayEntity>> GetByYearAsync(string userId, int year)
    {
        return await Context.VacationDays
            .Where(v => v.UserId == userId && v.Date.Year == year)
            .OrderBy(v => v.Date)
            .ToListAsync();
    }

    public async Task<List<VacationDayEntity>> GetPendingSyncAsync(string userId)
    {
        return await Context.VacationDays
            .Where(v => v.UserId == userId && v.IsPendingSync && !v.IsSynced)
            .ToListAsync();
    }

    public async Task<VacationDayEntity?> GetByDateAsync(string userId, DateOnly date)
    {
        return await Context.VacationDays
            .FirstOrDefaultAsync(v => v.UserId == userId && v.Date == date);
    }
}
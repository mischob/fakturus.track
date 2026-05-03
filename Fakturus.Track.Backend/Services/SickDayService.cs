using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class SickDayService(ApplicationDbContext context) : ISickDayService
{
    public async Task<SickDayDto> CreateSickDayAsync(CreateSickDayRequest request, string userId)
    {
        var existing = await context.SickDays
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == request.Date);

        if (existing != null) throw new InvalidOperationException("Sick day already exists for this date");

        var sickDay = new SickDay
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = request.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow
        };

        context.SickDays.Add(sickDay);
        await context.SaveChangesAsync();

        return MapToDto(sickDay);
    }

    public async Task<List<SickDayDto>> GetSickDaysAsync(string userId, int? year = null)
    {
        var query = context.SickDays.Where(s => s.UserId == userId);

        if (year.HasValue)
        {
            var startDate = new DateOnly(year.Value, 1, 1);
            var endDate = new DateOnly(year.Value, 12, 31);
            query = query.Where(s => s.Date >= startDate && s.Date <= endDate);
        }

        var sickDays = await query.OrderBy(s => s.Date).ToListAsync();
        return sickDays.Select(MapToDto).ToList();
    }

    public async Task DeleteSickDayAsync(Guid id, string userId)
    {
        var sickDay = await context.SickDays
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (sickDay == null) throw new InvalidOperationException("Sick day not found");

        context.SickDays.Remove(sickDay);
        await context.SaveChangesAsync();
    }

    public async Task<SyncSickDaysResponse> SyncSickDaysAsync(SyncSickDaysRequest request, string userId)
    {
        var existing = await context.SickDays
            .Where(s => s.UserId == userId)
            .ToListAsync();

        // Dedupe the incoming request by date — clients can occasionally end
        // up with two local sick days on the same day (race in the picker,
        // re-import, etc.). The DB has a unique (UserId, Date) constraint,
        // so we must not forward duplicates. Keep the most-recently-updated
        // entry for each date.
        var deduped = request.SickDays
            .GroupBy(s => s.Date)
            .Select(g => g.OrderByDescending(s => s.UpdatedAt).First())
            .ToList();

        var clientIds = deduped.Select(s => s.Id).ToHashSet();
        var serverIds = existing.Select(s => s.Id).ToHashSet();
        var serverByDate = existing.ToDictionary(s => s.Date);

        // Delete: server entries whose Id the client no longer has.
        var toDelete = existing.Where(s => !clientIds.Contains(s.Id)).ToList();
        var deletedDates = toDelete.Select(s => s.Date).ToHashSet();
        foreach (var s in toDelete) context.SickDays.Remove(s);

        // Update: matched by Id, only when client carries a newer timestamp.
        foreach (var dto in deduped.Where(s => serverIds.Contains(s.Id)))
        {
            var ex = existing.First(s => s.Id == dto.Id);
            if (dto.UpdatedAt > ex.UpdatedAt)
            {
                ex.Date = dto.Date;
                ex.UpdatedAt = dto.UpdatedAt;
                ex.SyncedAt = DateTime.UtcNow;
            }
        }

        // Add: client carries an Id the server doesn't know. Before inserting,
        // make sure no surviving server row already occupies the same date —
        // otherwise we'd hit IX_SickDays_UserId_Date and abort the whole
        // batch. Date-collision wins the existing server row and the client
        // will reconcile its local Id on the next pull.
        foreach (var dto in deduped.Where(s => !serverIds.Contains(s.Id)))
        {
            var dateClash = serverByDate.TryGetValue(dto.Date, out var clash)
                && !deletedDates.Contains(clash.Date);
            if (dateClash) continue;

            context.SickDays.Add(new SickDay
            {
                Id = dto.Id,
                UserId = userId,
                Date = dto.Date,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                SyncedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        var all = await context.SickDays
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Date)
            .ToListAsync();

        return new SyncSickDaysResponse(
            all.Select(MapToDto).ToList(),
            toDelete.Select(s => s.Id).ToList()
        );
    }

    private static SickDayDto MapToDto(SickDay s) => new(s.Id, s.Date, s.CreatedAt, s.UpdatedAt, s.SyncedAt);
}

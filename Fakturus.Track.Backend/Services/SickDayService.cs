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

        var clientIds = request.SickDays.Select(s => s.Id).ToHashSet();
        var serverIds = existing.Select(s => s.Id).ToHashSet();

        // Delete: on server but not in client
        var toDelete = existing.Where(s => !clientIds.Contains(s.Id)).ToList();

        // Add: in client but not on server
        var toAdd = request.SickDays.Where(s => !serverIds.Contains(s.Id)).ToList();

        // Update: in both, client newer
        var toUpdate = request.SickDays.Where(s => serverIds.Contains(s.Id)).ToList();

        foreach (var s in toDelete) context.SickDays.Remove(s);

        foreach (var dto in toAdd)
        {
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

        foreach (var dto in toUpdate)
        {
            var ex = existing.First(s => s.Id == dto.Id);
            if (dto.UpdatedAt > ex.UpdatedAt)
            {
                ex.Date = dto.Date;
                ex.UpdatedAt = dto.UpdatedAt;
                ex.SyncedAt = DateTime.UtcNow;
            }
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

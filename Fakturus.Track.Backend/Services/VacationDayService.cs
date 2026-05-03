using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class VacationDayService(ApplicationDbContext context) : IVacationDayService
{
    public async Task<VacationDayDto> CreateVacationDayAsync(CreateVacationDayRequest request, string userId)
    {
        // Check if vacation day already exists
        var existing = await context.VacationDays
            .FirstOrDefaultAsync(v => v.UserId == userId && v.Date == request.Date);

        if (existing != null) throw new InvalidOperationException("Vacation day already exists for this date");

        var vacationDay = new VacationDay
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = request.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow
        };

        context.VacationDays.Add(vacationDay);
        await context.SaveChangesAsync();

        return MapToDto(vacationDay);
    }

    public async Task<List<VacationDayDto>> GetVacationDaysAsync(string userId, int? year = null)
    {
        var query = context.VacationDays.Where(v => v.UserId == userId);

        if (year.HasValue)
        {
            var startDate = new DateOnly(year.Value, 1, 1);
            var endDate = new DateOnly(year.Value, 12, 31);
            query = query.Where(v => v.Date >= startDate && v.Date <= endDate);
        }

        var vacationDays = await query
            .OrderBy(v => v.Date)
            .ToListAsync();

        return vacationDays.Select(MapToDto).ToList();
    }

    public async Task DeleteVacationDayAsync(Guid id, string userId)
    {
        var vacationDay = await context.VacationDays
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

        if (vacationDay == null) throw new InvalidOperationException("Vacation day not found");

        context.VacationDays.Remove(vacationDay);
        await context.SaveChangesAsync();
    }

    public async Task<SyncVacationDaysResponse> SyncVacationDaysAsync(SyncVacationDaysRequest request, string userId)
    {
        var existingVacationDays = await context.VacationDays
            .Where(v => v.UserId == userId)
            .ToListAsync();

        // Dedupe client request by date — same defensive pattern as SickDays:
        // the DB has a unique (UserId, Date) constraint and a duplicate from
        // the client would abort the entire batch.
        var deduped = request.VacationDays
            .GroupBy(v => v.Date)
            .Select(g => g.OrderByDescending(v => v.UpdatedAt).First())
            .ToList();

        var clientVacationDayIds = deduped.Select(v => v.Id).ToHashSet();
        var serverVacationDayIds = existingVacationDays.Select(v => v.Id).ToHashSet();
        var serverByDate = existingVacationDays.ToDictionary(v => v.Date);

        var vacationDaysToDelete = existingVacationDays
            .Where(v => !clientVacationDayIds.Contains(v.Id))
            .ToList();
        var deletedDates = vacationDaysToDelete.Select(v => v.Date).ToHashSet();
        foreach (var vacationDay in vacationDaysToDelete) context.VacationDays.Remove(vacationDay);

        // Update: matched by Id, only if client carries a newer timestamp.
        foreach (var vacationDayDto in deduped.Where(v => serverVacationDayIds.Contains(v.Id)))
        {
            var existingVacationDay = existingVacationDays.First(v => v.Id == vacationDayDto.Id);
            if (vacationDayDto.UpdatedAt > existingVacationDay.UpdatedAt)
            {
                existingVacationDay.Date = vacationDayDto.Date;
                existingVacationDay.UpdatedAt = vacationDayDto.UpdatedAt;
                existingVacationDay.SyncedAt = DateTime.UtcNow;
            }
        }

        // Add: skip if a surviving server row already occupies the same date.
        foreach (var vacationDayDto in deduped.Where(v => !serverVacationDayIds.Contains(v.Id)))
        {
            var dateClash = serverByDate.TryGetValue(vacationDayDto.Date, out var clash)
                && !deletedDates.Contains(clash.Date);
            if (dateClash) continue;

            var vacationDay = new VacationDay
            {
                Id = vacationDayDto.Id,
                UserId = userId,
                Date = vacationDayDto.Date,
                CreatedAt = vacationDayDto.CreatedAt,
                UpdatedAt = vacationDayDto.UpdatedAt,
                SyncedAt = DateTime.UtcNow
            };
            context.VacationDays.Add(vacationDay);
        }

        await context.SaveChangesAsync();

        // Get all current vacation days from server
        var allVacationDays = await context.VacationDays
            .Where(v => v.UserId == userId)
            .OrderBy(v => v.Date)
            .ToListAsync();

        return new SyncVacationDaysResponse(
            allVacationDays.Select(MapToDto).ToList(),
            vacationDaysToDelete.Select(v => v.Id).ToList()
        );
    }

    private static VacationDayDto MapToDto(VacationDay vacationDay)
    {
        return new VacationDayDto(
            vacationDay.Id,
            vacationDay.Date,
            vacationDay.CreatedAt,
            vacationDay.UpdatedAt,
            vacationDay.SyncedAt
        );
    }
}
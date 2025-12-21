using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class SchoolHolidayService(ApplicationDbContext context) : ISchoolHolidayService
{
    public async Task<List<SchoolHolidayPeriodDto>> GetSchoolHolidayPeriodsAsync(string userId, int? year = null)
    {
        var query = context.SchoolHolidayPeriods
            .Where(p => p.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(p => p.Year == year.Value);
        }

        var periods = await query
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        return periods.Select(MapToDto).ToList();
    }

    public async Task<SchoolHolidayPeriodDto> CreateSchoolHolidayPeriodAsync(string userId, CreateSchoolHolidayPeriodRequest request)
    {
        var period = new SchoolHolidayPeriod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Year = request.StartDate.Year,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SchoolHolidayPeriods.Add(period);
        await context.SaveChangesAsync();

        return MapToDto(period);
    }

    public async Task<SchoolHolidayPeriodDto> UpdateSchoolHolidayPeriodAsync(string userId, Guid id, UpdateSchoolHolidayPeriodRequest request)
    {
        var period = await context.SchoolHolidayPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (period == null)
        {
            throw new InvalidOperationException("School holiday period not found");
        }

        period.Name = request.Name;
        period.StartDate = request.StartDate;
        period.EndDate = request.EndDate;
        period.Year = request.StartDate.Year;
        period.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return MapToDto(period);
    }

    public async Task DeleteSchoolHolidayPeriodAsync(string userId, Guid id)
    {
        var period = await context.SchoolHolidayPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (period == null)
        {
            throw new InvalidOperationException("School holiday period not found");
        }

        context.SchoolHolidayPeriods.Remove(period);
        await context.SaveChangesAsync();
    }

    public bool IsDateInSchoolHoliday(DateOnly date, List<SchoolHolidayPeriodDto> periods)
    {
        return periods.Any(p => date >= p.StartDate && date <= p.EndDate);
    }

    private static SchoolHolidayPeriodDto MapToDto(SchoolHolidayPeriod period)
    {
        return new SchoolHolidayPeriodDto(
            period.Id,
            period.Name,
            period.StartDate,
            period.EndDate,
            period.Year,
            period.CreatedAt,
            period.UpdatedAt
        );
    }
}


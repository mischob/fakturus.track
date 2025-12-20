using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Fakturus.Track.Backend.Services;

public class OvertimeCalculationService(ApplicationDbContext context) : IOvertimeCalculationService
{
    public async Task<OvertimeSummaryDto> CalculateOvertimeAsync(string userId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;

        // Get user settings
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var workHoursPerWeek = user.WorkHoursPerWeek;
        var vacationDaysPerYear = user.VacationDaysPerYear;
        var workDays = user.WorkDays;

        // Get all work sessions for the year
        var startDate = new DateOnly(targetYear, 1, 1);
        var endDate = new DateOnly(targetYear, 12, 31);

        var workSessions = await context.WorkSessions
            .Where(s => s.UserId == userId && s.Date >= startDate && s.Date <= endDate)
            .Where(s => s.StartTime != default && s.StopTime != null)
            .ToListAsync();

        // Get vacation days for the year
        var vacationDays = await context.VacationDays
            .Where(v => v.UserId == userId && v.Date >= startDate && v.Date <= endDate)
            .ToListAsync();

        // Calculate monthly overtime
        var monthlyOvertime = new List<MonthlyOvertimeDto>();
        decimal totalOvertimeHours = 0;

        for (int month = 1; month <= 12; month++)
        {
            var monthStart = new DateOnly(targetYear, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Get sessions for this month
            var monthSessions = workSessions
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .ToList();

            // Calculate worked hours
            decimal workedHours = 0;
            foreach (var session in monthSessions)
            {
                if (session.StopTime.HasValue)
                {
                    var duration = session.StopTime.Value - session.StartTime;
                    workedHours += (decimal)duration.TotalHours;
                }
            }

            // Calculate expected hours for the month
            // Count working days based on user's workday selection
            var workingDays = CountWorkingDays(monthStart, monthEnd, vacationDays, workDays);
            var selectedWorkDaysCount = CountSelectedWorkDays(workDays);
            var expectedHoursPerDay = selectedWorkDaysCount > 0 ? workHoursPerWeek / selectedWorkDaysCount : 0;
            var expectedHours = workingDays * expectedHoursPerDay;

            // Calculate overtime for this month
            var overtimeHours = workedHours - expectedHours;
            totalOvertimeHours += overtimeHours;

            monthlyOvertime.Add(new MonthlyOvertimeDto(
                targetYear,
                month,
                GetGermanMonthName(month),
                Math.Round(overtimeHours, 2),
                Math.Round(workedHours, 2),
                Math.Round(expectedHours, 2)
            ));
        }

        return new OvertimeSummaryDto(
            Math.Round(totalOvertimeHours, 2),
            monthlyOvertime,
            vacationDays.Count,
            vacationDaysPerYear - vacationDays.Count,
            vacationDaysPerYear
        );
    }

    private int CountWorkingDays(DateOnly startDate, DateOnly endDate, List<Data.Entities.VacationDay> vacationDays, int workDaysBitmask)
    {
        int workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Check if this day is in the user's workdays bitmask
            if (IsWorkDay(currentDate.DayOfWeek, workDaysBitmask))
            {
                // Check if it's not a vacation day
                if (!vacationDays.Any(v => v.Date == currentDate))
                {
                    workingDays++;
                }
            }
            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    private bool IsWorkDay(DayOfWeek dayOfWeek, int workDaysBitmask)
    {
        // Convert DayOfWeek (Sunday=0, Monday=1, ...) to our bitmask (Monday=bit0, Tuesday=bit1, ...)
        int bitPosition = dayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => -1
        };

        if (bitPosition < 0) return false;

        return (workDaysBitmask & (1 << bitPosition)) != 0;
    }

    private int CountSelectedWorkDays(int workDaysBitmask)
    {
        int count = 0;
        for (int i = 0; i < 7; i++)
        {
            if ((workDaysBitmask & (1 << i)) != 0)
            {
                count++;
            }
        }
        return count;
    }

    private string GetGermanMonthName(int month) => month switch
    {
        1 => "Januar",
        2 => "Februar",
        3 => "März",
        4 => "April",
        5 => "Mai",
        6 => "Juni",
        7 => "Juli",
        8 => "August",
        9 => "September",
        10 => "Oktober",
        11 => "November",
        12 => "Dezember",
        _ => ""
    };
}


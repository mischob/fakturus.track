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
            // Count working days (excluding weekends and vacation days)
            var workingDays = CountWorkingDays(monthStart, monthEnd, vacationDays);
            var expectedHoursPerDay = workHoursPerWeek / 5; // Assuming 5-day work week
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

    private int CountWorkingDays(DateOnly startDate, DateOnly endDate, List<Data.Entities.VacationDay> vacationDays)
    {
        int workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Check if it's a weekday (Monday-Friday)
            var dayOfWeek = currentDate.DayOfWeek;
            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
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


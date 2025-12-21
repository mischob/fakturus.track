using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class OvertimeCalculationService(ApplicationDbContext context, IHolidayService holidayService, ISchoolHolidayService schoolHolidayService)
    : IOvertimeCalculationService
{
    public async Task<OvertimeSummaryDto> CalculateOvertimeAsync(string userId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;

        // Get user settings
        var user = await context.Users.FindAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found");

        var workHoursPerWeek = user.WorkHoursPerWeek;
        var vacationDaysPerYear = user.VacationDaysPerYear;
        var workDays = user.WorkDays;
        var bundesland = user.Bundesland;

        // Get holidays for the year
        var holidays = holidayService.GetHolidaysForYear(bundesland, targetYear);

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

        for (var month = 1; month <= 12; month++)
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
                if (session.StopTime.HasValue)
                {
                    var duration = session.StopTime.Value - session.StartTime;
                    workedHours += (decimal)duration.TotalHours;
                }

            // Calculate expected hours for the month
            // Count working days based on user's workday selection
            var monthHolidays = holidays.Where(h => h >= monthStart && h <= monthEnd).ToList();
            var workingDays = CountWorkingDays(monthStart, monthEnd, vacationDays, workDays, monthHolidays);
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

        // Count holidays that fall on workdays
        var holidaysTaken = CountHolidaysOnWorkdays(holidays, workDays);

        // Calculate school holiday hours not worked
        var schoolHolidayPeriods = await schoolHolidayService.GetSchoolHolidayPeriodsAsync(userId, targetYear);
        var schoolHolidayHoursNotWorked = CalculateSchoolHolidayHoursNotWorked(
            startDate, endDate, schoolHolidayPeriods, vacationDays, workDays, holidays, workHoursPerWeek);

        return new OvertimeSummaryDto(
            Math.Round(totalOvertimeHours, 2),
            monthlyOvertime,
            vacationDays.Count,
            vacationDaysPerYear - vacationDays.Count,
            vacationDaysPerYear,
            holidaysTaken,
            Math.Round(schoolHolidayHoursNotWorked, 2)
        );
    }

    private int CountWorkingDays(DateOnly startDate, DateOnly endDate, List<VacationDay> vacationDays,
        int workDaysBitmask, List<DateOnly> holidays)
    {
        var workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Check if this day is in the user's workdays bitmask
            if (IsWorkDay(currentDate.DayOfWeek, workDaysBitmask))
                // Check if it's not a vacation day or holiday
                if (!vacationDays.Any(v => v.Date == currentDate) &&
                    !holidays.Contains(currentDate))
                    workingDays++;

            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    private int CountHolidaysOnWorkdays(List<DateOnly> holidays, int workDaysBitmask)
    {
        return holidays.Count(holiday => IsWorkDay(holiday.DayOfWeek, workDaysBitmask));
    }

    private bool IsWorkDay(DayOfWeek dayOfWeek, int workDaysBitmask)
    {
        // Convert DayOfWeek (Sunday=0, Monday=1, ...) to our bitmask (Monday=bit0, Tuesday=bit1, ...)
        var bitPosition = dayOfWeek switch
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
        var count = 0;
        for (var i = 0; i < 7; i++)
            if ((workDaysBitmask & (1 << i)) != 0)
                count++;

        return count;
    }

    private string GetGermanMonthName(int month)
    {
        return month switch
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

    private decimal CalculateSchoolHolidayHoursNotWorked(
        DateOnly startDate,
        DateOnly endDate,
        List<DTOs.SchoolHolidayPeriodDto> schoolHolidayPeriods,
        List<VacationDay> vacationDays,
        int workDaysBitmask,
        List<DateOnly> holidays,
        decimal workHoursPerWeek)
    {
        if (!schoolHolidayPeriods.Any())
        {
            return 0;
        }

        var selectedWorkDaysCount = CountSelectedWorkDays(workDaysBitmask);
        if (selectedWorkDaysCount == 0)
        {
            return 0;
        }

        var expectedHoursPerDay = workHoursPerWeek / selectedWorkDaysCount;
        decimal totalHours = 0;

        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            // Check if date is in a school holiday period
            if (schoolHolidayService.IsDateInSchoolHoliday(currentDate, schoolHolidayPeriods))
            {
                // Check if it's a workday
                if (IsWorkDay(currentDate.DayOfWeek, workDaysBitmask))
                {
                    // Check if it's NOT a vacation day
                    if (!vacationDays.Any(v => v.Date == currentDate))
                    {
                        // Check if it's NOT a public holiday
                        if (!holidays.Contains(currentDate))
                        {
                            // This is a workday in school holidays that was not worked
                            totalHours += expectedHoursPerDay;
                        }
                    }
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return totalHours;
    }
}
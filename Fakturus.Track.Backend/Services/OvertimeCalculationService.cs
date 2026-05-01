using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class OvertimeCalculationService(
    ApplicationDbContext context,
    IHolidayService holidayService,
    ISchoolHolidayService schoolHolidayService)
    : IOvertimeCalculationService
{
    public async Task<OvertimeSummaryDto> CalculateOvertimeAsync(string userId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isCurrentYear = targetYear == today.Year;

        var user = await context.Users.FindAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found");

        var vacationDaysPerYear = user.VacationDaysPerYear;
        var bundesland = user.Bundesland;

        // Stage 2: WorkDays / WorkHoursPerWeek timeline. For each calendar day
        // we look up the entry whose [ValidFrom, ValidTo] window covers it. If
        // no history rows exist (legacy account), we synthesize a single
        // open-ended entry from the User row so calculations stay deterministic.
        var settingsTimeline = await BuildSettingsTimelineAsync(userId, user);

        var holidays = holidayService.GetHolidaysForYear(bundesland, targetYear);

        var startDate = new DateOnly(targetYear, 1, 1);
        var endDate = new DateOnly(targetYear, 12, 31);

        var workSessions = await context.WorkSessions
            .Where(s => s.UserId == userId && s.Date >= startDate && s.Date <= endDate)
            .Where(s => s.StartTime != default && s.StopTime != null)
            .ToListAsync();

        var vacationDays = await context.VacationDays
            .Where(v => v.UserId == userId && v.Date >= startDate && v.Date <= endDate)
            .ToListAsync();

        // Stage 1: actually load sick days. Treating them like vacation days
        // means a sick day on a configured workday cancels that day's expected
        // hours and contributes 0 worked → net zero, i.e. "fully worked".
        var sickDays = await context.SickDays
            .Where(s => s.UserId == userId && s.Date >= startDate && s.Date <= endDate)
            .ToListAsync();

        var monthlyOvertime = new List<MonthlyOvertimeDto>();
        decimal totalOvertimeHours = 0;

        var maxMonth = isCurrentYear ? today.Month : 12;

        for (var month = 1; month <= maxMonth; month++)
        {
            var monthStart = new DateOnly(targetYear, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            if (isCurrentYear && month == today.Month) monthEnd = today;

            var monthSessions = workSessions
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .ToList();

            decimal workedHours = 0;
            foreach (var session in monthSessions)
                if (session.StopTime.HasValue)
                {
                    var duration = session.StopTime.Value - session.StartTime;
                    workedHours += (decimal)duration.TotalHours;
                }

            var monthHolidays = holidays.Where(h => h >= monthStart && h <= monthEnd).ToList();

            // Expected hours: walk every day, sum the day's expected workload
            // using the settings entry valid on that day. Vacation, sick, and
            // public holidays are skipped (they're "covered").
            decimal expectedHours = 0;
            var sickDaysInMonth = 0;
            var cursor = monthStart;
            while (cursor <= monthEnd)
            {
                var settings = settingsTimeline.SettingsFor(cursor);
                var isWorkday = IsWorkDay(cursor.DayOfWeek, settings.WorkDays);
                var isVacation = vacationDays.Any(v => v.Date == cursor);
                var isSick = sickDays.Any(s => s.Date == cursor);
                var isHoliday = monthHolidays.Contains(cursor);

                if (isSick) sickDaysInMonth++;

                if (isWorkday && !isVacation && !isSick && !isHoliday)
                {
                    expectedHours += settings.ExpectedHoursPerDay;
                }

                cursor = cursor.AddDays(1);
            }

            var overtimeHours = workedHours - expectedHours;
            totalOvertimeHours += overtimeHours;

            monthlyOvertime.Add(new MonthlyOvertimeDto(
                targetYear,
                month,
                GetGermanMonthName(month),
                Math.Round(overtimeHours, 2),
                Math.Round(workedHours, 2),
                Math.Round(expectedHours, 2),
                sickDaysInMonth
            ));
        }

        var holidaysTaken = CountHolidaysOnWorkdays(holidays, settingsTimeline);

        var schoolHolidayEndDate = isCurrentYear ? today : endDate;
        var schoolHolidayPeriods = await schoolHolidayService.GetSchoolHolidayPeriodsAsync(userId, targetYear);
        var schoolHolidayHoursNotWorked = CalculateSchoolHolidayHoursNotWorked(
            startDate, schoolHolidayEndDate, schoolHolidayPeriods,
            vacationDays, sickDays, holidays, settingsTimeline);

        return new OvertimeSummaryDto(
            Math.Round(totalOvertimeHours, 2),
            monthlyOvertime,
            vacationDays.Count,
            vacationDaysPerYear - vacationDays.Count,
            vacationDaysPerYear,
            holidaysTaken,
            Math.Round(schoolHolidayHoursNotWorked, 2),
            sickDays.Count
        );
    }

    // ---- Settings timeline (Stage 2) ----------------------------------------

    private async Task<SettingsTimeline> BuildSettingsTimelineAsync(string userId, User userFallback)
    {
        var rows = await context.UserSettingsHistory
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.ValidFrom)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return new SettingsTimeline(new List<UserSettingsHistory>
            {
                new()
                {
                    Id = Guid.Empty,
                    UserId = userId,
                    ValidFrom = DateOnly.MinValue,
                    ValidTo = null,
                    WorkDays = userFallback.WorkDays,
                    WorkHoursPerWeek = userFallback.WorkHoursPerWeek,
                    CreatedAt = userFallback.CreatedAt,
                    UpdatedAt = userFallback.UpdatedAt
                }
            });
        }

        return new SettingsTimeline(rows);
    }

    private sealed class SettingsTimeline(List<UserSettingsHistory> rows)
    {
        public ResolvedSettings SettingsFor(DateOnly date)
        {
            // Windows are [ValidFrom, ValidTo] inclusive, ValidTo == null means
            // open-ended. `LastOrDefault` picks the most recent matching entry
            // when multiple cover the date (defensive against bad data).
            var match = rows.LastOrDefault(r => r.ValidFrom <= date && (r.ValidTo == null || r.ValidTo >= date));
            match ??= rows.FirstOrDefault();
            if (match == null) return new ResolvedSettings(31, 40m);
            return new ResolvedSettings(match.WorkDays, match.WorkHoursPerWeek);
        }
    }

    private readonly record struct ResolvedSettings(int WorkDays, decimal WorkHoursPerWeek)
    {
        public decimal ExpectedHoursPerDay
        {
            get
            {
                var n = CountSelectedWorkDays(WorkDays);
                return n == 0 ? 0 : WorkHoursPerWeek / n;
            }
        }
    }

    // ---- helpers ------------------------------------------------------------

    private static int CountHolidaysOnWorkdays(List<DateOnly> holidays, SettingsTimeline timeline)
    {
        return holidays.Count(h => IsWorkDay(h.DayOfWeek, timeline.SettingsFor(h).WorkDays));
    }

    private static bool IsWorkDay(DayOfWeek dayOfWeek, int workDaysBitmask)
    {
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

    private static int CountSelectedWorkDays(int workDaysBitmask)
    {
        var count = 0;
        for (var i = 0; i < 7; i++)
            if ((workDaysBitmask & (1 << i)) != 0)
                count++;
        return count;
    }

    private static string GetGermanMonthName(int month)
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
        List<SickDay> sickDays,
        List<DateOnly> holidays,
        SettingsTimeline timeline)
    {
        if (schoolHolidayPeriods.Count == 0) return 0;

        decimal totalHours = 0;
        var cursor = startDate;
        while (cursor <= endDate)
        {
            if (schoolHolidayService.IsDateInSchoolHoliday(cursor, schoolHolidayPeriods))
            {
                var settings = timeline.SettingsFor(cursor);
                if (IsWorkDay(cursor.DayOfWeek, settings.WorkDays)
                    && !vacationDays.Any(v => v.Date == cursor)
                    && !sickDays.Any(s => s.Date == cursor)
                    && !holidays.Contains(cursor))
                {
                    totalHours += settings.ExpectedHoursPerDay;
                }
            }
            cursor = cursor.AddDays(1);
        }
        return totalHours;
    }
}

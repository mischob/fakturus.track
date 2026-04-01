using System.Globalization;
using System.Text;
using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Fakturus.Track.Backend.Services;

public class ExportService(ApplicationDbContext context, IHolidayService holidayService) : IExportService
{
    private static readonly CultureInfo DeLocale = new("de-DE");

    private static readonly string[] GermanWeekdays =
        { "Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag" };

    public async Task<ExportResult> GenerateExportAsync(string userId, string format, string period, int year,
        int? month = null, int? quarter = null)
    {
        var user = await context.Users.FindAsync(userId)
                   ?? throw new InvalidOperationException("User not found");

        return format.ToLowerInvariant() switch
        {
            "csv" => await GenerateCsvAsync(userId, user, period, year, month, quarter),
            "datev" => await GenerateDatevAsync(userId, user, year, month ?? DateTime.UtcNow.Month),
            "pdf" => await GeneratePdfAsync(userId, user, period, year, month),
            _ => throw new ArgumentException($"Unknown export format: {format}")
        };
    }

    private (DateOnly start, DateOnly end) GetDateRange(string period, int year, int? month, int? quarter)
    {
        return period.ToLowerInvariant() switch
        {
            "month" => (new DateOnly(year, month ?? DateTime.UtcNow.Month, 1),
                new DateOnly(year, month ?? DateTime.UtcNow.Month, 1).AddMonths(1).AddDays(-1)),
            "quarter" =>
                GetQuarterRange(year, quarter ?? ((DateTime.UtcNow.Month - 1) / 3 + 1)),
            "year" => (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)),
            _ => throw new ArgumentException($"Unknown period: {period}")
        };
    }

    private static (DateOnly start, DateOnly end) GetQuarterRange(int year, int quarter)
    {
        var startMonth = (quarter - 1) * 3 + 1;
        return (new DateOnly(year, startMonth, 1), new DateOnly(year, startMonth, 1).AddMonths(3).AddDays(-1));
    }

    private async Task<(List<WorkSession> sessions, List<VacationDay> vacationDays, List<DateOnly> holidays)>
        LoadDataAsync(string userId, User user, DateOnly start, DateOnly end)
    {
        var sessions = await context.WorkSessions
            .Where(s => s.UserId == userId && s.Date >= start && s.Date <= end && s.StopTime != null)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync();

        var vacationDays = await context.VacationDays
            .Where(v => v.UserId == userId && v.Date >= start && v.Date <= end)
            .ToListAsync();

        var holidays = new List<DateOnly>();
        for (var y = start.Year; y <= end.Year; y++)
            holidays.AddRange(holidayService.GetHolidaysForYear(user.Bundesland, y));

        return (sessions, vacationDays, holidays);
    }

    private bool IsWorkDay(DayOfWeek dayOfWeek, int workDaysBitmask)
    {
        var bitPosition = dayOfWeek switch
        {
            DayOfWeek.Monday => 0, DayOfWeek.Tuesday => 1, DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3, DayOfWeek.Friday => 4, DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6, _ => -1
        };
        return bitPosition >= 0 && (workDaysBitmask & (1 << bitPosition)) != 0;
    }

    private static string GetGermanWeekday(DayOfWeek dow) => GermanWeekdays[(int)dow];

    private decimal GetDailyTargetHours(User user)
    {
        var workDaysCount = Enumerable.Range(0, 7).Count(i => (user.WorkDays & (1 << i)) != 0);
        return workDaysCount > 0 ? user.WorkHoursPerWeek / workDaysCount : 0;
    }

    // ===== CSV =====
    private async Task<ExportResult> GenerateCsvAsync(string userId, User user, string period, int year,
        int? month, int? quarter)
    {
        var (start, end) = GetDateRange(period, year, month, quarter);
        var (sessions, vacationDays, holidays) = await LoadDataAsync(userId, user, start, end);

        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("Datum;Wochentag;Start;Ende;Pause (min);Netto (h);Typ");

        var current = start;
        while (current <= end)
        {
            var date = current;
            var weekday = GetGermanWeekday(date.DayOfWeek);
            var daySessions = sessions.Where(s => s.Date == date).ToList();
            var isVacation = vacationDays.Any(v => v.Date == date);
            var isHoliday = holidays.Contains(date);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            if (daySessions.Count > 0)
            {
                foreach (var session in daySessions)
                {
                    var startTime = session.StartTime.ToString("HH:mm");
                    var endTime = session.StopTime?.ToString("HH:mm") ?? "";
                    var netHours = session.StopTime.HasValue
                        ? (decimal)(session.StopTime.Value - session.StartTime).TotalHours
                        : 0m;
                    sb.AppendLine(
                        $"{date:dd.MM.yyyy};{weekday};{startTime};{endTime};0;{netHours.ToString("F2", DeLocale)};Arbeit");
                }
            }
            else if (isHoliday && IsWorkDay(date.DayOfWeek, user.WorkDays))
            {
                sb.AppendLine($"{date:dd.MM.yyyy};{weekday};;;;0,00;Feiertag");
            }
            else if (isVacation)
            {
                var dailyHours = GetDailyTargetHours(user);
                sb.AppendLine(
                    $"{date:dd.MM.yyyy};{weekday};;;;{dailyHours.ToString("F2", DeLocale)};Urlaub");
            }
            else if (!isWeekend && IsWorkDay(date.DayOfWeek, user.WorkDays))
            {
                sb.AppendLine($"{date:dd.MM.yyyy};{weekday};;;;0,00;");
            }
            else
            {
                sb.AppendLine($"{date:dd.MM.yyyy};{weekday};;;;0,00;");
            }

            current = current.AddDays(1);
        }

        var fileName = period.ToLowerInvariant() switch
        {
            "month" => $"Arbeitszeiten_{year}-{(month ?? DateTime.UtcNow.Month):D2}.csv",
            "quarter" => $"Arbeitszeiten_{year}-Q{quarter ?? ((DateTime.UtcNow.Month - 1) / 3 + 1)}.csv",
            "year" => $"Arbeitszeiten_{year}.csv",
            _ => $"Arbeitszeiten_{year}.csv"
        };

        return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }

    // ===== DATEV =====
    private async Task<ExportResult> GenerateDatevAsync(string userId, User user, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var (sessions, vacationDays, holidays) = await LoadDataAsync(userId, user, start, end);

        var sb = new StringBuilder();
        sb.Append("Personalnummer;Datum;Lohnart;Stunden;Von;Bis;Pause\r\n");

        var dailyTarget = GetDailyTargetHours(user);
        var current = start;
        while (current <= end)
        {
            var date = current;
            var daySessions = sessions.Where(s => s.Date == date).ToList();
            var isVacation = vacationDays.Any(v => v.Date == date);
            var isHoliday = holidays.Contains(date);

            if (daySessions.Count > 0)
            {
                foreach (var session in daySessions)
                {
                    var hours = session.StopTime.HasValue
                        ? (decimal)(session.StopTime.Value - session.StartTime).TotalHours
                        : 0m;
                    var von = session.StartTime.ToString("HH:mm");
                    var bis = session.StopTime?.ToString("HH:mm") ?? "";
                    sb.Append($"00000;{date:dd.MM.yyyy};200;{hours.ToString("F2", CultureInfo.InvariantCulture)};{von};{bis};0\r\n");
                }
            }
            else if (isVacation && IsWorkDay(date.DayOfWeek, user.WorkDays))
            {
                sb.Append($"00000;{date:dd.MM.yyyy};400;{dailyTarget.ToString("F2", CultureInfo.InvariantCulture)};;;\r\n");
            }
            // Holidays and non-workdays are not included in DATEV

            current = current.AddDays(1);
        }

        var fileName = $"DATEV_Lohn_{year}-{month:D2}.csv";
        // DATEV: no BOM, UTF-8
        return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }

    // ===== PDF =====
    private async Task<ExportResult> GeneratePdfAsync(string userId, User user, string period, int year, int? month)
    {
        if (period == "year")
            return await GenerateYearlyPdfAsync(userId, user, year);

        return await GenerateMonthlyPdfAsync(userId, user, year, month ?? DateTime.UtcNow.Month);
    }

    private async Task<ExportResult> GenerateYearlyPdfAsync(string userId, User user, int year)
    {
        // Generate one page per month
        var allPages = new List<(int month, string monthName, List<DayRow> rows, MonthSummary summary)>();

        for (var m = 1; m <= 12; m++)
        {
            var (rows, summary) = await BuildMonthData(userId, user, year, m);
            var monthName = new DateOnly(year, m, 1).ToString("MMMM", DeLocale);
            allPages.Add((m, monthName, rows, summary));
        }

        var pdf = Document.Create(container =>
        {
            foreach (var (m, monthName, rows, summary) in allPages)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("fakturus.track — Arbeitszeitnachweis").FontSize(16).Bold();
                        col.Item().Text($"{monthName} {year}").FontSize(12).FontColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        BuildPdfTable(col, rows);
                        col.Item().PaddingTop(10);
                        BuildPdfSummary(col, summary);
                    });

                    page.Footer().AlignCenter()
                        .Text($"Generiert am {DateTime.UtcNow:dd.MM.yyyy} mit fakturus.track")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }
        });

        var bytes = pdf.GeneratePdf();
        return new ExportResult(bytes, "application/pdf", $"Arbeitszeitnachweis_{year}.pdf");
    }

    private async Task<ExportResult> GenerateMonthlyPdfAsync(string userId, User user, int year, int month)
    {
        var (rows, summary) = await BuildMonthData(userId, user, year, month);
        var monthName = new DateOnly(year, month, 1).ToString("MMMM", DeLocale);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("fakturus.track — Arbeitszeitnachweis").FontSize(16).Bold();
                    col.Item().Text($"{monthName} {year}").FontSize(12).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    BuildPdfTable(col, rows);
                    col.Item().PaddingTop(10);
                    BuildPdfSummary(col, summary);
                });

                page.Footer().AlignCenter()
                    .Text($"Generiert am {DateTime.UtcNow:dd.MM.yyyy} mit fakturus.track")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        var bytes = pdf.GeneratePdf();
        return new ExportResult(bytes, "application/pdf", $"Arbeitszeitnachweis_{year}-{month:D2}.pdf");
    }

    private record DayRow(
        DateOnly Date, string Weekday, string Start, string End,
        string Pause, string Net, string Type, string RowColor);

    private record MonthSummary(
        decimal ExpectedHours, decimal WorkedHours, decimal OvertimeHours,
        int VacationDays, int Holidays);

    private async Task<(List<DayRow> rows, MonthSummary summary)> BuildMonthData(string userId, User user, int year,
        int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var (sessions, vacationDays, holidays) = await LoadDataAsync(userId, user, start, end);

        var dailyTarget = GetDailyTargetHours(user);
        var rows = new List<DayRow>();
        decimal totalWorked = 0;
        decimal totalExpected = 0;
        int vacCount = 0, holidayCount = 0;

        var current = start;
        while (current <= end)
        {
            var date = current;
            var weekday = GetGermanWeekday(date.DayOfWeek);
            var daySessions = sessions.Where(s => s.Date == date).ToList();
            var isVacation = vacationDays.Any(v => v.Date == date);
            var isHoliday = holidays.Contains(date);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isWorkday = IsWorkDay(date.DayOfWeek, user.WorkDays);

            var rowColor = isWeekend ? "#fafafa" : isHoliday ? "#f0e6ff" : isVacation ? "#e0f7fa" : "#ffffff";

            if (daySessions.Count > 0)
            {
                foreach (var session in daySessions)
                {
                    var hours = session.StopTime.HasValue
                        ? (decimal)(session.StopTime.Value - session.StartTime).TotalHours
                        : 0m;
                    totalWorked += hours;
                    rows.Add(new DayRow(date, weekday,
                        session.StartTime.ToString("HH:mm"),
                        session.StopTime?.ToString("HH:mm") ?? "",
                        "0 min", $"{hours:F2}h", "Arbeit", rowColor));
                }

                if (isWorkday) totalExpected += dailyTarget;
            }
            else if (isHoliday && isWorkday)
            {
                holidayCount++;
                rows.Add(new DayRow(date, weekday, "", "", "", "0,00h", "Feiertag", rowColor));
            }
            else if (isVacation && isWorkday)
            {
                vacCount++;
                rows.Add(new DayRow(date, weekday, "", "", "", $"{dailyTarget:F2}h", "Urlaub", rowColor));
            }
            else if (isWorkday)
            {
                totalExpected += dailyTarget;
                rows.Add(new DayRow(date, weekday, "", "", "", "", "", rowColor));
            }
            else
            {
                rows.Add(new DayRow(date, weekday, "", "", "", "", "", rowColor));
            }

            current = current.AddDays(1);
        }

        var summary = new MonthSummary(
            Math.Round(totalExpected, 2),
            Math.Round(totalWorked, 2),
            Math.Round(totalWorked - totalExpected, 2),
            vacCount, holidayCount);

        return (rows, summary);
    }

    private static void BuildPdfTable(ColumnDescriptor col, List<DayRow> rows)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(70);  // Datum
                c.ConstantColumn(75);  // Wochentag
                c.ConstantColumn(45);  // Start
                c.ConstantColumn(45);  // Ende
                c.ConstantColumn(50);  // Pause
                c.ConstantColumn(55);  // Netto
                c.RelativeColumn();    // Typ
            });

            // Header
            table.Header(header =>
            {
                var headerStyle = TextStyle.Default.FontSize(8).Bold();
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Datum").Style(headerStyle);
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Wochentag").Style(headerStyle);
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Start").Style(headerStyle);
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Ende").Style(headerStyle);
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Pause").Style(headerStyle);
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Netto").Style(headerStyle);
                header.Cell().BorderBottom(2).BorderColor(Colors.Grey.Darken3).Padding(4)
                    .Text("Typ").Style(headerStyle);
            });

            // Rows
            foreach (var row in rows)
            {
                var bgColor = row.RowColor;
                var textStyle = TextStyle.Default.FontSize(8);
                if (bgColor == "#fafafa") textStyle = textStyle.FontColor(Colors.Grey.Medium);

                void Cell(IContainer c, string text) =>
                    c.Background(bgColor).BorderBottom(1).BorderColor("#eeeeee").Padding(3)
                        .Text(text).Style(textStyle);

                Cell(table.Cell(), row.Date.ToString("dd.MM.yyyy"));
                Cell(table.Cell(), row.Weekday);
                Cell(table.Cell(), row.Start);
                Cell(table.Cell(), row.End);
                Cell(table.Cell(), row.Pause);
                Cell(table.Cell(), row.Net);
                Cell(table.Cell(), row.Type);
            }
        });
    }

    private static void BuildPdfSummary(ColumnDescriptor col, MonthSummary summary)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(100);
            });

            void Row(string label, string value, string? color = null)
            {
                table.Cell().Padding(4).Text(label).FontSize(9).Bold();
                var cell = table.Cell().Padding(4).AlignRight();
                var text = cell.Text(value).FontSize(9).Bold();
                if (color != null) text.FontColor(color);
            }

            Row("Soll-Stunden", $"{summary.ExpectedHours:F1}h");
            Row("Ist-Stunden", $"{summary.WorkedHours:F1}h");
            Row("Ueberstunden",
                $"{(summary.OvertimeHours >= 0 ? "+" : "")}{summary.OvertimeHours:F1}h",
                summary.OvertimeHours >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
            Row("Urlaubstage", $"{summary.VacationDays}");
            Row("Feiertage", $"{summary.Holidays}");
        });
    }
}

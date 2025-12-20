namespace Fakturus.Track.Frontend.Models;

public class OvertimeSummaryModel
{
    public decimal TotalOvertimeHours { get; set; }
    public List<MonthlyOvertimeModel> MonthlyOvertime { get; set; } = new();
    public int VacationDaysTaken { get; set; }
    public int VacationDaysRemaining { get; set; }
    public int VacationDaysPerYear { get; set; }
    public int HolidaysTaken { get; set; }
}

public class MonthlyOvertimeModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal OvertimeHours { get; set; }
    public decimal WorkedHours { get; set; }
    public decimal ExpectedHours { get; set; }
}


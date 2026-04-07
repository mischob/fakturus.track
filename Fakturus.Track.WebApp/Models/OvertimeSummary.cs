namespace Fakturus.Track.WebApp.Models;

public class OvertimeSummary
{
    public decimal TotalOvertimeHours { get; set; }
    public List<MonthlyOvertime> MonthlyOvertime { get; set; } = new();
    public int VacationDaysTaken { get; set; }
    public int VacationDaysRemaining { get; set; }
    public int VacationDaysPerYear { get; set; }
    public int HolidaysTaken { get; set; }
}

public class MonthlyOvertime
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal OvertimeHours { get; set; }
    public decimal WorkedHours { get; set; }
    public decimal ExpectedHours { get; set; }
}

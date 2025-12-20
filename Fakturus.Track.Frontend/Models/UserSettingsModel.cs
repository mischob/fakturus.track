namespace Fakturus.Track.Frontend.Models;

public class UserSettingsModel
{
    public int VacationDaysPerYear { get; set; } = 30;
    public decimal WorkHoursPerWeek { get; set; } = 40;
    public int WorkDays { get; set; } = 31; // Default Mo-Fr (0b0011111)
}


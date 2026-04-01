namespace Fakturus.Track.WebApp.Models;

public class UserSettings
{
    public int VacationDaysPerYear { get; set; } = 30;
    public decimal WorkHoursPerWeek { get; set; } = 40;
    public int WorkDays { get; set; } = 31; // Bitmask: Mo-Fr = 0b0011111
    public string Bundesland { get; set; } = "NW";
}

namespace Fakturus.Track.Backend.Data.Entities;

public class User
{
    public string Id { get; set; } = string.Empty; // Azure AD B2C user ID
    public string? CalendarUrl { get; set; } // Public calendar feed URL
    public int VacationDaysPerYear { get; set; } = 30; // Default 30 days per year
    public decimal WorkHoursPerWeek { get; set; } = 40; // Default 40 hours per week
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

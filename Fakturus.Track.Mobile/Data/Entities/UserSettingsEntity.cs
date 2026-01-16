namespace Fakturus.Track.Mobile.Data.Entities;

public class UserSettingsEntity
{
    public string UserId { get; set; } = string.Empty; // Primary key
    public string? CalendarUrl { get; set; }
    public int VacationDaysPerYear { get; set; } = 30;
    public decimal WorkHoursPerWeek { get; set; } = 40;
    public int WorkDays { get; set; } = 31; // Mo-Fr (0b0011111 = 31)
    public string Bundesland { get; set; } = "NW";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    
    // Mobile-specific fields for offline sync
    public bool IsSynced { get; set; }
    public bool IsPendingSync { get; set; }
}

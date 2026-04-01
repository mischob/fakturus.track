namespace Fakturus.Track.WebApp.Models;

public enum VacationDayType
{
    Vacation,
    Sick,
    Holiday
}

public class VacationDay
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public VacationDayType Type { get; set; } = VacationDayType.Vacation;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    public bool IsPendingSync { get; set; }
    public bool IsSynced { get; set; }
}

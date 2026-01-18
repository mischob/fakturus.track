namespace Fakturus.Track.Mobile.Data.Entities;

public class VacationDayEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }

    // Mobile-specific fields for offline sync
    public bool IsSynced { get; set; }
    public bool IsPendingSync { get; set; }
}
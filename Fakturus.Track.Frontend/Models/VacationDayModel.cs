namespace Fakturus.Track.Frontend.Models;

public class VacationDayModel
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    public bool IsPendingSync { get; set; }
    public bool IsSynced { get; set; }
}


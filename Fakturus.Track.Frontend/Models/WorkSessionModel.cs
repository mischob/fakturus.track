namespace Fakturus.Track.Frontend.Models;

public class WorkSessionModel
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    public bool IsSynced { get; set; }
    public bool IsPendingSync { get; set; }
}


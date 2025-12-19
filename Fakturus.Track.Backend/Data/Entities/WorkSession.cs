namespace Fakturus.Track.Backend.Data.Entities;

public class WorkSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public string? CalendarEventId { get; set; } // Optional UID from imported calendar event
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}
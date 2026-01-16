namespace Fakturus.Track.Mobile.Data.Entities;

public class CalendarEventEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty; // Calendar event UID
    public string Summary { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Read-only cache, no sync fields needed
}

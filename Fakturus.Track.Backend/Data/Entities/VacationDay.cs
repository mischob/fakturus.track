namespace Fakturus.Track.Backend.Data.Entities;

public class VacationDay
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}


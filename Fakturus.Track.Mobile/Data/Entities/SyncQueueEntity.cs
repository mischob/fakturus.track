namespace Fakturus.Track.Mobile.Data.Entities;

public class SyncQueueEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // "WorkSession", "VacationDay", "UserSettings"
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty; // "Create", "Update", "Delete"
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
}
namespace Fakturus.Track.Backend.DTOs;

public class CreateWorkSessionRequest
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
}
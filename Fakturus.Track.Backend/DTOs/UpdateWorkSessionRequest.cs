namespace Fakturus.Track.Backend.DTOs;

public class UpdateWorkSessionRequest
{
    public DateOnly? Date { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? StopTime { get; set; }
}


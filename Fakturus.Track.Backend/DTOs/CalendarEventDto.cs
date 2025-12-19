namespace Fakturus.Track.Backend.DTOs;

public class CalendarEventDto
{
    public string Uid { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
}

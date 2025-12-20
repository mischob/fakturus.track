namespace Fakturus.Track.Frontend.Models;

public class CalendarEventModel
{
    public string Uid { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsSelected { get; set; } = false; // For UI binding

    public TimeSpan Duration => EndTime - StartTime;
}
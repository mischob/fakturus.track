namespace Fakturus.Track.Frontend.Models;

public class MonthGroup
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public List<WorkSessionModel> Sessions { get; set; } = [];
    public int TotalEntries { get; set; }
    public int CompletedEntries { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public bool IsExpanded { get; set; }
}

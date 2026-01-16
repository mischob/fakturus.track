namespace Fakturus.Track.Mobile.Data.Entities;

public class SchoolHolidayPeriodEntity
{
    public Guid Id { get; set; }
    public string Bundesland { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Read-only cache, no sync fields needed
}

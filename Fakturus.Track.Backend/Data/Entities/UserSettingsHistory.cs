namespace Fakturus.Track.Backend.Data.Entities;

/// <summary>
/// Time-versioned snapshot of those user settings whose values affect
/// historical calculations (overtime, expected hours). When a user changes
/// their workdays or weekly hours, the previous active row is sealed
/// (ValidTo = newEffectiveDate - 1) and a new row is inserted with
/// ValidFrom = newEffectiveDate, ValidTo = null.
/// </summary>
public class UserSettingsHistory
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Inclusive lower bound of the validity window.</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Inclusive upper bound; null means "still in effect".</summary>
    public DateOnly? ValidTo { get; set; }

    public int WorkDays { get; set; }
    public decimal WorkHoursPerWeek { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

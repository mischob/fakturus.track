namespace Fakturus.Track.Backend.Data.Entities;

public class User
{
    public string Id { get; set; } = string.Empty; // Azure AD B2C user ID
    public string? CalendarUrl { get; set; } // Public calendar feed URL
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

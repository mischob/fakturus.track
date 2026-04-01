namespace Fakturus.Track.Backend.Data.Entities;

public class UserConsent
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // "privacy_policy", "terms_of_service"
    public int DocumentVersion { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentTimestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? AppVersion { get; set; }
    public string? Platform { get; set; } // "ios", "android"
}

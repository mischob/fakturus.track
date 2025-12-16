namespace Fakturus.Track.Backend.DTOs;

public class HealthResponse
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


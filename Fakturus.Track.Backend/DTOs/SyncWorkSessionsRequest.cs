namespace Fakturus.Track.Backend.DTOs;

public class SyncWorkSessionsRequest
{
    public List<CreateWorkSessionRequest> WorkSessions { get; set; } = new();
}
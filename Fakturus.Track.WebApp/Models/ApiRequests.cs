namespace Fakturus.Track.WebApp.Models;

public class CreateWorkSessionRequest
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
}

public class UpdateWorkSessionRequest
{
    public DateOnly? Date { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? StopTime { get; set; }
}

public class SyncWorkSessionsRequest
{
    public List<WorkSession> Sessions { get; set; } = new();
}

public class CreateVacationDayRequest
{
    public DateOnly Date { get; set; }
}

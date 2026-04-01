namespace Fakturus.Track.WebApp.Models;

public class CreateWorkSessionRequest
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public int PauseMinutes { get; set; }
}

public class UpdateWorkSessionRequest
{
    public DateOnly? Date { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public int? PauseMinutes { get; set; }
    public bool? IsFinished { get; set; }
}

public class CreateVacationDayRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
}

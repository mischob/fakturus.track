using Fakturus.Track.Frontend.Models;
using Refit;

namespace Fakturus.Track.Frontend.Services;

public interface IWorkSessionsApiClient
{
    [Get("/v1/work-sessions")]
    Task<List<WorkSessionModel>> GetWorkSessionsAsync();

    [Get("/v1/work-sessions/{id}")]
    Task<WorkSessionModel> GetWorkSessionAsync(Guid id);

    [Post("/v1/work-sessions")]
    Task<WorkSessionModel> CreateWorkSessionAsync([Body] CreateWorkSessionRequest request);

    [Put("/v1/work-sessions/{id}")]
    Task<WorkSessionModel> UpdateWorkSessionAsync(Guid id, [Body] UpdateWorkSessionRequest request);

    [Delete("/v1/work-sessions/{id}")]
    Task DeleteWorkSessionAsync(Guid id);

    [Post("/v1/work-sessions/sync")]
    Task<List<WorkSessionModel>> SyncWorkSessionsAsync([Body] SyncWorkSessionsRequest request);
}

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
    public List<CreateWorkSessionRequest> WorkSessions { get; set; } = new();
}
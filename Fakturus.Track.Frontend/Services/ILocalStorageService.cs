using Fakturus.Track.Frontend.Models;

namespace Fakturus.Track.Frontend.Services;

public interface ILocalStorageService
{
    Task SaveWorkSessionAsync(WorkSessionModel workSession);
    Task<List<WorkSessionModel>> GetWorkSessionsAsync();
    Task<WorkSessionModel?> GetWorkSessionByIdAsync(Guid id);
    Task DeleteWorkSessionAsync(Guid id);
    Task<List<WorkSessionModel>> GetPendingSyncWorkSessionsAsync();
    Task MarkAsSyncedAsync(Guid id);
}


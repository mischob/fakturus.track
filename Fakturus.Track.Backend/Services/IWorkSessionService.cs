using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface IWorkSessionService
{
    Task<List<WorkSessionDto>> GetWorkSessionsByUserIdAsync(string userId);
    Task<WorkSessionDto?> GetWorkSessionByIdAsync(Guid id, string userId);
    Task<WorkSessionDto> CreateWorkSessionAsync(CreateWorkSessionRequest request, string userId);
    Task<WorkSessionDto> UpdateWorkSessionAsync(Guid id, UpdateWorkSessionRequest request, string userId);
    Task<bool> DeleteWorkSessionAsync(Guid id, string userId);
    Task<List<WorkSessionDto>> SyncWorkSessionsAsync(List<CreateWorkSessionRequest> workSessions, string userId);
}
using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface ISickDayService
{
    Task<SickDayDto> CreateSickDayAsync(CreateSickDayRequest request, string userId);
    Task<List<SickDayDto>> GetSickDaysAsync(string userId, int? year = null);
    Task DeleteSickDayAsync(Guid id, string userId);
    Task<SyncSickDaysResponse> SyncSickDaysAsync(SyncSickDaysRequest request, string userId);
}

using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface IVacationDayService
{
    Task<VacationDayDto> CreateVacationDayAsync(CreateVacationDayRequest request, string userId);
    Task<List<VacationDayDto>> GetVacationDaysAsync(string userId, int? year = null);
    Task DeleteVacationDayAsync(Guid id, string userId);
    Task<SyncVacationDaysResponse> SyncVacationDaysAsync(SyncVacationDaysRequest request, string userId);
}
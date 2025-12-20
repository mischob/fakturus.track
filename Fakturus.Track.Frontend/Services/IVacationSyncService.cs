using Fakturus.Track.Frontend.Models;

namespace Fakturus.Track.Frontend.Services;

public interface IVacationSyncService
{
    Task SyncAsync();
    Task StartPeriodicSyncAsync();
    void StopPeriodicSync();
    event EventHandler? SyncCompleted;

    // Local storage methods
    Task SaveVacationDayAsync(VacationDayModel vacationDay);
    Task SaveVacationDaysAsync(List<VacationDayModel> vacationDays);
    Task<List<VacationDayModel>> GetVacationDaysAsync();
    Task DeleteVacationDayAsync(Guid id);
}
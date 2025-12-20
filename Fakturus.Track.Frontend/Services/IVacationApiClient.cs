using Fakturus.Track.Frontend.Models;
using Refit;

namespace Fakturus.Track.Frontend.Services;

public interface IVacationApiClient
{
    [Get("/v1/vacation-days")]
    Task<List<VacationDayModel>> GetVacationDaysAsync([Query] int? year = null);

    [Post("/v1/vacation-days")]
    Task<VacationDayModel> CreateVacationDayAsync([Body] CreateVacationDayRequest request);

    [Delete("/v1/vacation-days/{id}")]
    Task DeleteVacationDayAsync(Guid id);

    [Post("/v1/vacation-days/sync")]
    Task<SyncVacationDaysResponse> SyncVacationDaysAsync([Body] SyncVacationDaysRequest request);
}

public class CreateVacationDayRequest
{
    public DateOnly Date { get; set; }
}

public class SyncVacationDaysRequest
{
    public List<VacationDayDto> VacationDays { get; set; } = new();
}

public class VacationDayDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}

public class SyncVacationDaysResponse
{
    public List<VacationDayModel> ServerVacationDays { get; set; } = new();
    public List<Guid> DeletedIds { get; set; } = new();
}
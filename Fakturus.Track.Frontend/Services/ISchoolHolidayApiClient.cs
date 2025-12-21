using Fakturus.Track.Frontend.Models;
using Refit;

namespace Fakturus.Track.Frontend.Services;

public interface ISchoolHolidayApiClient
{
    [Get("/v1/school-holidays")]
    Task<List<SchoolHolidayPeriodModel>> GetSchoolHolidayPeriodsAsync([Query] int? year = null);

    [Post("/v1/school-holidays")]
    Task<SchoolHolidayPeriodModel> CreateSchoolHolidayPeriodAsync([Body] CreateSchoolHolidayPeriodRequest request);

    [Put("/v1/school-holidays/{id}")]
    Task<SchoolHolidayPeriodModel> UpdateSchoolHolidayPeriodAsync(Guid id, [Body] UpdateSchoolHolidayPeriodRequest request);

    [Delete("/v1/school-holidays/{id}")]
    Task DeleteSchoolHolidayPeriodAsync(Guid id);
}


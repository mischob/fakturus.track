using Fakturus.Track.Frontend.Models;
using Refit;

namespace Fakturus.Track.Frontend.Services;

public interface ISettingsApiClient
{
    [Get("/v1/settings")]
    Task<UserSettingsModel> GetUserSettingsAsync();

    [Put("/v1/settings")]
    Task<UserSettingsModel> UpdateUserSettingsAsync([Body] UpdateUserSettingsRequest request);

    [Get("/v1/overtime-summary")]
    Task<OvertimeSummaryModel> GetOvertimeSummaryAsync([Query] int? year = null);
}

public class UpdateUserSettingsRequest
{
    public int VacationDaysPerYear { get; set; }
    public decimal WorkHoursPerWeek { get; set; }
}


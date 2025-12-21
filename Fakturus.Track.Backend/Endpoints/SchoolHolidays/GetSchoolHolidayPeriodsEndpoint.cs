using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SchoolHolidays;

public class GetSchoolHolidayPeriodsEndpoint(ISchoolHolidayService schoolHolidayService)
    : EndpointWithoutRequest<List<SchoolHolidayPeriodDto>>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/school-holidays");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var year = Query<int?>("year", isRequired: false);

            var periods = await schoolHolidayService.GetSchoolHolidayPeriodsAsync(userId, year);
            Response = periods;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting school holiday periods");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while getting school holiday periods" }, ct);
        }
    }
}


using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SchoolHolidays;

public class CreateSchoolHolidayPeriodEndpoint(ISchoolHolidayService schoolHolidayService)
    : Endpoint<CreateSchoolHolidayPeriodRequest, SchoolHolidayPeriodDto>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/school-holidays");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));
    }

    public override async Task HandleAsync(CreateSchoolHolidayPeriodRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var period = await schoolHolidayService.CreateSchoolHolidayPeriodAsync(userId, req);
            Response = period;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating school holiday period");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while creating school holiday period" }, ct);
        }
    }
}


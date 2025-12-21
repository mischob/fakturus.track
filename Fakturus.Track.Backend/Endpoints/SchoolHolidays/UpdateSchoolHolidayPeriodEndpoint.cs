using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SchoolHolidays;

public class UpdateSchoolHolidayPeriodRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class UpdateSchoolHolidayPeriodEndpoint(ISchoolHolidayService schoolHolidayService)
    : Endpoint<UpdateSchoolHolidayPeriodRequest, SchoolHolidayPeriodDto>
{
    public override void Configure()
    {
        Put("v{version:apiVersion}/school-holidays/{id}");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));
    }

    public override async Task HandleAsync(UpdateSchoolHolidayPeriodRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var updateRequest = new DTOs.UpdateSchoolHolidayPeriodRequest(
                req.Name,
                req.StartDate,
                req.EndDate
            );
            var period = await schoolHolidayService.UpdateSchoolHolidayPeriodAsync(userId, req.Id, updateRequest);
            Response = period;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "School holiday period not found");
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = ex.Message }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating school holiday period");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while updating school holiday period" }, ct);
        }
    }
}


using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SchoolHolidays;

public class DeleteSchoolHolidayPeriodRequest
{
    public Guid Id { get; set; }
}

public class DeleteSchoolHolidayPeriodEndpoint(ISchoolHolidayService schoolHolidayService)
    : Endpoint<DeleteSchoolHolidayPeriodRequest>
{
    public override void Configure()
    {
        Delete("v{version:apiVersion}/school-holidays/{id}");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));
    }

    public override async Task HandleAsync(DeleteSchoolHolidayPeriodRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            await schoolHolidayService.DeleteSchoolHolidayPeriodAsync(userId, req.Id);
            HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
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
            Logger.LogError(ex, "Error deleting school holiday period");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while deleting school holiday period" }, ct);
        }
    }
}


using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.VacationDays;

public class DeleteVacationDayRequest
{
    public Guid Id { get; set; }
}

public class DeleteVacationDayEndpoint(IVacationDayService vacationDayService)
    : Endpoint<DeleteVacationDayRequest>
{
    public override void Configure()
    {
        Delete("v{version:apiVersion}/vacation-days/{Id}");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Delete a vacation day";
            s.Description = "Deletes a vacation day for the authenticated user";
        });
    }

    public override async Task HandleAsync(DeleteVacationDayRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            await vacationDayService.DeleteVacationDayAsync(req.Id, userId);
            HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Vacation day not found");
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = ex.Message }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting vacation day");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while deleting the vacation day" }, ct);
        }
    }
}
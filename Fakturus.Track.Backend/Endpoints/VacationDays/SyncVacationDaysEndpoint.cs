using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.VacationDays;

public class SyncVacationDaysEndpoint(IVacationDayService vacationDayService)
    : Endpoint<SyncVacationDaysRequest, SyncVacationDaysResponse>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/vacation-days/sync");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Sync vacation days";
            s.Description = "Synchronizes vacation days between client and server for the authenticated user";
        });
    }

    public override async Task HandleAsync(SyncVacationDaysRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var response = await vacationDayService.SyncVacationDaysAsync(req, userId);
            Response = response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error syncing vacation days");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while syncing vacation days" }, ct);
        }
    }
}
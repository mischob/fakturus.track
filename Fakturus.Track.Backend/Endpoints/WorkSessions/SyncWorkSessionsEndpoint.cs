using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.WorkSessions;

public class SyncWorkSessionsEndpoint(IWorkSessionService workSessionService)
    : Endpoint<SyncWorkSessionsRequest, List<WorkSessionDto>>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/work-sessions/sync");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Sync work sessions";
            s.Description = "Bulk sync work sessions from client to server";
        });
    }

    public override async Task HandleAsync(SyncWorkSessionsRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var syncedSessions = await workSessionService.SyncWorkSessionsAsync(req.WorkSessions, userId);
            Response = syncedSessions;
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error syncing work sessions");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "An error occurred while syncing work sessions" },
                ct);
        }
    }
}
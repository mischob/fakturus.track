using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.WorkSessions;

public class GetWorkSessionsEndpoint(IWorkSessionService workSessionService) : EndpointWithoutRequest<List<WorkSessionDto>>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/work-sessions");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get all work sessions";
            s.Description = "Retrieves all work sessions for the authenticated user";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var workSessions = await workSessionService.GetWorkSessionsByUserIdAsync(userId);
            Response = workSessions;
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving work sessions");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "An error occurred while retrieving work sessions" }, ct);
        }
    }
}


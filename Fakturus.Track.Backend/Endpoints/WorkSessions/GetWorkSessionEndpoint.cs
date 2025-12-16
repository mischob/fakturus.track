using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.WorkSessions;

public class GetWorkSessionEndpoint(IWorkSessionService workSessionService) : Endpoint<GetWorkSessionRequest, WorkSessionDto>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/work-sessions/{Id}");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get a work session by ID";
            s.Description = "Retrieves a specific work session by ID for the authenticated user";
        });
    }

    public override async Task HandleAsync(GetWorkSessionRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var workSession = await workSessionService.GetWorkSessionByIdAsync(req.Id, userId);

            if (workSession == null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            Response = workSession;
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving work session");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "An error occurred while retrieving the work session" }, ct);
        }
    }
}

public class GetWorkSessionRequest
{
    public Guid Id { get; set; }
}


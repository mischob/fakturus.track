using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.WorkSessions;

public class CreateWorkSessionEndpoint(IWorkSessionService workSessionService) : Endpoint<CreateWorkSessionRequest, WorkSessionDto>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/work-sessions");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Create a new work session";
            s.Description = "Creates a new work session for the authenticated user";
        });
    }

    public override async Task HandleAsync(CreateWorkSessionRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var workSession = await workSessionService.CreateWorkSessionAsync(req, userId);
            HttpContext.Response.Headers.Location = $"/v1/work-sessions/{workSession.Id}";
            Response = workSession;
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating work session");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "An error occurred while creating the work session" }, ct);
        }
    }
}


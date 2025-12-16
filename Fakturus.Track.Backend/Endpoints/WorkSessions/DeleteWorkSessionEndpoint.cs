using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.WorkSessions;

public class DeleteWorkSessionEndpoint(IWorkSessionService workSessionService) : Endpoint<DeleteWorkSessionRequest>
{
    public override void Configure()
    {
        Delete("v{version:apiVersion}/work-sessions/{Id}");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Delete a work session";
            s.Description = "Deletes a work session for the authenticated user";
        });
    }

    public override async Task HandleAsync(DeleteWorkSessionRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var deleted = await workSessionService.DeleteWorkSessionAsync(req.Id, userId);

            if (!deleted)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await HttpContext.Response.WriteAsJsonAsync(new { Error = "Work session not found" }, ct);
                return;
            }

            HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting work session");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "An error occurred while deleting the work session" }, ct);
        }
    }
}

public class DeleteWorkSessionRequest
{
    public Guid Id { get; set; }
}


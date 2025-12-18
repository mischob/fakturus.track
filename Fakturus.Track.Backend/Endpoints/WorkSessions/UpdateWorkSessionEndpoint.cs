using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.WorkSessions;

public class UpdateWorkSessionEndpoint(IWorkSessionService workSessionService)
    : Endpoint<UpdateWorkSessionRequest, WorkSessionDto>
{
    public override void Configure()
    {
        Put("v{version:apiVersion}/work-sessions/{Id}");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Update a work session";
            s.Description = "Updates an existing work session for the authenticated user";
        });
    }

    public override async Task HandleAsync(UpdateWorkSessionRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var routeId = Route<Guid>("Id");

            var updateRequest = new DTOs.UpdateWorkSessionRequest
            {
                Date = req.Date,
                StartTime = req.StartTime,
                StopTime = req.StopTime
            };

            var workSession = await workSessionService.UpdateWorkSessionAsync(routeId, updateRequest, userId);
            Response = workSession;
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Work session not found");
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = "Work session not found" }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating work session");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while updating the work session" }, ct);
        }
    }
}

public class UpdateWorkSessionRequest
{
    public DateOnly? Date { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? StopTime { get; set; }
}
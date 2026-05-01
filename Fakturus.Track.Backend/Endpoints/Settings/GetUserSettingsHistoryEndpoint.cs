using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Settings;

public class GetUserSettingsHistoryEndpoint(IUserSettingsService userSettingsService)
    : EndpointWithoutRequest<List<UserSettingsHistoryEntryDto>>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/settings/history");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get user settings history";
            s.Description = "Returns all WorkDays / WorkHoursPerWeek timeline entries for the authenticated user, newest first.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            Response = await userSettingsService.GetUserSettingsHistoryAsync(userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching user settings history");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while fetching user settings history" }, ct);
        }
    }
}

using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Settings;

public class GetUserSettingsEndpoint(IUserSettingsService userSettingsService)
    : EndpointWithoutRequest<UserSettingsDto>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/settings");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get user settings";
            s.Description = "Retrieves settings for the authenticated user";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var settings = await userSettingsService.GetUserSettingsAsync(userId);
            Response = settings;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving user settings");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while retrieving user settings" }, ct);
        }
    }
}


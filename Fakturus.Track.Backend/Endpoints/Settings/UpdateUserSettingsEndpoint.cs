using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Settings;

public class UpdateUserSettingsEndpoint(IUserSettingsService userSettingsService)
    : Endpoint<UpdateUserSettingsRequest, UserSettingsDto>
{
    public override void Configure()
    {
        Put("v{version:apiVersion}/settings");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Update user settings";
            s.Description = "Updates settings for the authenticated user";
        });
    }

    public override async Task HandleAsync(UpdateUserSettingsRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var settings = await userSettingsService.UpdateUserSettingsAsync(req, userId);
            Response = settings;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user settings");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while updating user settings" }, ct);
        }
    }
}
using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Calendar;

public class GetCalendarEventsEndpoint(ICalendarService calendarService, IConfiguration configuration)
    : EndpointWithoutRequest<List<CalendarEventDto>>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/calendar/events");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get calendar events";
            s.Description = "Retrieves calendar events from the user's configured calendar feed";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();

            // Check if user is authorized to access calendar
            var enabledUserId = configuration["Calendar:EnabledUserId"];
            if (string.IsNullOrEmpty(enabledUserId) || userId != enabledUserId)
            {
                // For now, only allow configured user. In future, check Users table
                Logger.LogWarning("User {UserId} attempted to access calendar but is not authorized", userId);
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await HttpContext.Response.WriteAsJsonAsync(
                    new { Error = "Calendar access is not enabled for your account" }, ct);
                return;
            }

            var events = await calendarService.GetCalendarEventsAsync(userId);
            Response = events;
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving calendar events");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while retrieving calendar events" }, ct);
        }
    }
}
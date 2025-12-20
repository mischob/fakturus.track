using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.VacationDays;

public class CreateVacationDayEndpoint(IVacationDayService vacationDayService)
    : Endpoint<CreateVacationDayRequest, VacationDayDto>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/vacation-days");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Create a new vacation day";
            s.Description = "Creates a new vacation day for the authenticated user";
        });
    }

    public override async Task HandleAsync(CreateVacationDayRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var vacationDay = await vacationDayService.CreateVacationDayAsync(req, userId);
            HttpContext.Response.Headers.Location = $"/v1/vacation-days/{vacationDay.Id}";
            Response = vacationDay;
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Validation error creating vacation day");
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = ex.Message }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating vacation day");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while creating the vacation day" }, ct);
        }
    }
}
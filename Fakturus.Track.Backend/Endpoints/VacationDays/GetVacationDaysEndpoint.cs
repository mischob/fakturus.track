using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.VacationDays;

public class GetVacationDaysRequest
{
    public int? Year { get; set; }
}

public class GetVacationDaysEndpoint(IVacationDayService vacationDayService)
    : Endpoint<GetVacationDaysRequest, List<VacationDayDto>>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/vacation-days");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get vacation days";
            s.Description = "Retrieves all vacation days for the authenticated user, optionally filtered by year";
        });
    }

    public override async Task HandleAsync(GetVacationDaysRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var vacationDays = await vacationDayService.GetVacationDaysAsync(userId, req.Year);
            Response = vacationDays;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving vacation days");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while retrieving vacation days" }, ct);
        }
    }
}


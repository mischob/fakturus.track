using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Settings;

public class GetOvertimeSummaryRequest
{
    public int? Year { get; set; }
}

public class GetOvertimeSummaryEndpoint(IOvertimeCalculationService overtimeCalculationService)
    : Endpoint<GetOvertimeSummaryRequest, OvertimeSummaryDto>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/overtime-summary");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Get overtime summary";
            s.Description = "Retrieves overtime and vacation summary for the authenticated user";
        });
    }

    public override async Task HandleAsync(GetOvertimeSummaryRequest req, CancellationToken ct)
    {
        try
        {
            var userId = User.GetObjectId();
            var summary = await overtimeCalculationService.CalculateOvertimeAsync(userId, req.Year);
            Response = summary;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating overtime summary");
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while calculating overtime summary" }, ct);
        }
    }
}
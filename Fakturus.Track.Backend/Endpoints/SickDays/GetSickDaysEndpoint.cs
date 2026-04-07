using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SickDays;

public class GetSickDaysEndpoint(ISickDayService sickDayService)
    : EndpointWithoutRequest<List<SickDayDto>>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/sick-days");
        Policies("RequireAuthentication");
        Options(x => x.WithVersionSet("FakturusTrack").MapToApiVersion(1.0));
        Summary(s => { s.Summary = "Get all sick days for the authenticated user"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetObjectId();
        var year = Query<int?>("year", isRequired: false);
        Response = await sickDayService.GetSickDaysAsync(userId, year);
    }
}

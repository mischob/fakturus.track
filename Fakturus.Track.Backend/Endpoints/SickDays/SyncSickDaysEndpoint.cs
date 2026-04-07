using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SickDays;

public class SyncSickDaysEndpoint(ISickDayService sickDayService)
    : Endpoint<SyncSickDaysRequest, SyncSickDaysResponse>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/sick-days/sync");
        Policies("RequireAuthentication");
        Options(x => x.WithVersionSet("FakturusTrack").MapToApiVersion(1.0));
        Summary(s =>
        {
            s.Summary = "Sync sick days";
            s.Description = "Synchronizes sick days between client and server";
        });
    }

    public override async Task HandleAsync(SyncSickDaysRequest req, CancellationToken ct)
    {
        var userId = User.GetObjectId();
        Response = await sickDayService.SyncSickDaysAsync(req, userId);
    }
}

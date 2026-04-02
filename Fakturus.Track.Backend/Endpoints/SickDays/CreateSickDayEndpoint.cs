using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SickDays;

public class CreateSickDayEndpoint(ISickDayService sickDayService)
    : Endpoint<CreateSickDayRequest, SickDayDto>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/sick-days");
        Policies("RequireAuthentication");
        Options(x => x.WithVersionSet("FakturusTrack").MapToApiVersion(1.0));
        Summary(s => { s.Summary = "Create a new sick day"; });
    }

    public override async Task HandleAsync(CreateSickDayRequest req, CancellationToken ct)
    {
        var userId = User.GetObjectId();
        Response = await sickDayService.CreateSickDayAsync(req, userId);
    }
}

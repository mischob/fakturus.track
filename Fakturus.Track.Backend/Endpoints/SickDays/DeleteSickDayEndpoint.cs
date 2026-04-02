using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.SickDays;

public class DeleteSickDayEndpoint(ISickDayService sickDayService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("v{version:apiVersion}/sick-days/{id}");
        Policies("RequireAuthentication");
        Options(x => x.WithVersionSet("FakturusTrack").MapToApiVersion(1.0));
        Summary(s => { s.Summary = "Delete a sick day"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetObjectId();
        var id = Route<Guid>("id");
        await sickDayService.DeleteSickDayAsync(id, userId);
        HttpContext.Response.StatusCode = 204;
    }
}

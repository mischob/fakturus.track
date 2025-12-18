using Fakturus.Track.Backend.DTOs;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints;

public class HealthCheckEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/health");

        AllowAnonymous();

        Options(x => x
            .WithVersionSet("health")
            .MapToApiVersion(1.0));

        Summary(s =>
        {
            s.Summary = "Health check endpoint";
            s.Description = "Returns the health status of the API";
        });
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        Response = new HealthResponse();
        return Task.CompletedTask;
    }
}
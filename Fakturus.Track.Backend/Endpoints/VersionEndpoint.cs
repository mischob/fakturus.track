using System.Reflection;
using Fakturus.Track.Backend.DTOs;
using FastEndpoints;

namespace Fakturus.Track.Backend.Endpoints;

public class VersionEndpoint : EndpointWithoutRequest<VersionResponse>
{
    public override void Configure()
    {
        Get("/v1/version");
        AllowAnonymous(); // Wichtig: Kein Auth erforderlich

        Summary(s =>
        {
            s.Summary = "Version endpoint";
            s.Description = "Returns the current application version for cache-busting";
        });
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        // Version aus Assembly oder Environment Variable
        var version = Environment.GetEnvironmentVariable("APP_VERSION")
                      ?? Assembly.GetExecutingAssembly()
                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion
                      ?? "1.0.0";

        Response = new VersionResponse { Version = version };
        return Task.CompletedTask;
    }
}
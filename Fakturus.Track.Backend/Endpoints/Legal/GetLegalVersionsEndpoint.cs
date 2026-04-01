using System.Text.Json;
using Fakturus.Track.Backend.DTOs;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Legal;

public class GetLegalVersionsEndpoint : EndpointWithoutRequest<LegalVersionsResponse>
{
    private static LegalVersionsResponse? _cached;

    public override void Configure()
    {
        Get("api/legal/versions");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get current legal document versions";
            s.Description = "Returns version info for all legal documents. Public endpoint.";
        });
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        if (_cached == null)
        {
            var json = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "legal-versions.json"));
            var doc = JsonDocument.Parse(json);
            var documents = new List<LegalDocumentInfo>();

            foreach (var el in doc.RootElement.GetProperty("documents").EnumerateArray())
            {
                documents.Add(new LegalDocumentInfo(
                    Type: el.GetProperty("type").GetString()!,
                    Version: el.GetProperty("version").GetInt32(),
                    EffectiveDate: el.GetProperty("effectiveDate").GetString()!,
                    Url: el.GetProperty("url").GetString()!,
                    RequiresConsent: el.GetProperty("requiresConsent").GetBoolean(),
                    RequiresReConsent: el.GetProperty("requiresReConsent").GetBoolean()
                ));
            }

            _cached = new LegalVersionsResponse(documents);
        }

        HttpContext.Response.Headers.CacheControl = "public, max-age=3600";
        Response = _cached;
        return Task.CompletedTask;
    }
}

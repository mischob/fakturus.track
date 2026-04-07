using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using FastEndpoints;

namespace Fakturus.Track.Backend.Endpoints.Legal;

public class GetConsentStatusEndpoint(ApplicationDbContext db)
    : EndpointWithoutRequest<UserConsentStatusResponse>
{
    public override void Configure()
    {
        Get("api/legal/consent");
        Policies("RequireAuthentication");
        Summary(s =>
        {
            s.Summary = "Get current consent status for authenticated user";
            s.Description = "Returns which legal documents the user has consented to.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetObjectId();

        var allConsents = db.UserConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentTimestamp)
            .AsEnumerable()
            .GroupBy(c => c.DocumentType)
            .Select(g => g.First())
            .ToList();

        var records = allConsents.Select(c => new UserConsentRecord(
            c.DocumentType, c.DocumentVersion, c.ConsentGiven, c.ConsentTimestamp
        )).ToList();

        var json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "legal-versions.json"), ct);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var requiredTypes = new List<string>();

        foreach (var el in doc.RootElement.GetProperty("documents").EnumerateArray())
        {
            if (el.GetProperty("requiresConsent").GetBoolean())
                requiredTypes.Add(el.GetProperty("type").GetString()!);
        }

        var pending = requiredTypes
            .Where(t => !allConsents.Any(c => c.DocumentType == t && c.ConsentGiven))
            .ToList();

        Response = new UserConsentStatusResponse(records, pending.Count == 0, pending);
    }
}

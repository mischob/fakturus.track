using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using FastEndpoints;

namespace Fakturus.Track.Backend.Endpoints.Legal;

public class SubmitConsentEndpoint(ApplicationDbContext db)
    : Endpoint<SubmitConsentRequest, UserConsentStatusResponse>
{
    public override void Configure()
    {
        Post("api/legal/consent");
        Policies("RequireAuthentication");
        Summary(s =>
        {
            s.Summary = "Submit user consent for legal documents";
            s.Description = "Records user consent. Each submission is appended (audit trail).";
        });
    }

    public override async Task HandleAsync(SubmitConsentRequest req, CancellationToken ct)
    {
        var userId = User.GetObjectId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var appVersion = HttpContext.Request.Headers["X-App-Version"].FirstOrDefault();
        var platform = HttpContext.Request.Headers["X-Platform"].FirstOrDefault();
        var now = DateTime.UtcNow;

        foreach (var item in req.Consents)
        {
            db.UserConsents.Add(new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DocumentType = item.DocumentType,
                DocumentVersion = item.DocumentVersion,
                ConsentGiven = item.ConsentGiven,
                ConsentTimestamp = now,
                IpAddress = ipAddress,
                AppVersion = appVersion,
                Platform = platform
            });
        }

        await db.SaveChangesAsync(ct);

        // Return current status
        Response = await BuildConsentStatus(userId, ct);
    }

    private async Task<UserConsentStatusResponse> BuildConsentStatus(string userId, CancellationToken ct)
    {
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

        // Load required documents from config
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

        return new UserConsentStatusResponse(records, pending.Count == 0, pending);
    }
}

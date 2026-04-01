using System.Text.Json;
using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Fakturus.Track.Backend.Extensions;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Endpoints.SchoolHolidays;

public class ImportSchoolHolidaysRequest
{
    public string Bundesland { get; set; } = "NW";
    public int Year { get; set; } = DateTime.Now.Year;
}

public class ImportSchoolHolidaysEndpoint(
    ApplicationDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<ImportSchoolHolidaysEndpoint> logger)
    : Endpoint<ImportSchoolHolidaysRequest, List<SchoolHolidayPeriodDto>>
{
    public override void Configure()
    {
        Post("v{version:apiVersion}/school-holidays/import");
        Policies("RequireAuthentication");
        Options(x => x.WithVersionSet("FakturusTrack").MapToApiVersion(1.0));
        Summary(s =>
        {
            s.Summary = "Import school holidays from ferien-api.de";
            s.Description =
                "Fetches school holidays for the given Bundesland and year, replaces existing entries for that year.";
        });
    }

    public override async Task HandleAsync(ImportSchoolHolidaysRequest req, CancellationToken ct)
    {
        var userId = User.GetObjectId();

        // Map 2-letter Bundesland codes to ferien-api.de codes
        var stateCode = req.Bundesland.ToUpperInvariant();

        try
        {
            var client = httpClientFactory.CreateClient();
            var apiUrl = $"https://ferien-api.de/api/v1/holidays/{stateCode}/{req.Year}";
            logger.LogInformation("Fetching school holidays from {Url}", apiUrl);

            var response = await client.GetAsync(apiUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ferien-api.de returned {StatusCode}", response.StatusCode);
                HttpContext.Response.StatusCode = (int)response.StatusCode;
                Response = new List<SchoolHolidayPeriodDto>();
                return;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var holidays = JsonSerializer.Deserialize<List<FerienApiHoliday>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (holidays == null || holidays.Count == 0)
            {
                Response = new List<SchoolHolidayPeriodDto>();
                return;
            }

            // Delete existing school holidays for this user + year
            var existing = await db.SchoolHolidayPeriods
                .Where(s => s.UserId == userId && s.Year == req.Year)
                .ToListAsync(ct);
            db.SchoolHolidayPeriods.RemoveRange(existing);

            // Insert new ones
            var newPeriods = new List<SchoolHolidayPeriod>();
            foreach (var h in holidays)
            {
                // Capitalize first letter of each word for nice display name
                var name = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                    .ToTitleCase(h.Name.Split(' ')[0]);

                var period = new SchoolHolidayPeriod
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = name,
                    StartDate = DateOnly.Parse(h.Start),
                    EndDate = DateOnly.Parse(h.End),
                    Year = req.Year,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                newPeriods.Add(period);
                db.SchoolHolidayPeriods.Add(period);
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Imported {Count} school holiday periods for {State}/{Year}",
                newPeriods.Count, stateCode, req.Year);

            Response = newPeriods.Select(p => new SchoolHolidayPeriodDto(
                p.Id, p.Name, p.StartDate, p.EndDate, p.Year, p.CreatedAt, p.UpdatedAt
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import school holidays from ferien-api.de");
            ThrowError("Import fehlgeschlagen: " + ex.Message);
        }
    }
}

internal class FerienApiHoliday
{
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public int Year { get; set; }
    public string StateCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

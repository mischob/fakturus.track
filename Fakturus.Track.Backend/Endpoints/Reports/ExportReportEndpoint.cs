using Fakturus.Track.Backend.Extensions;
using Fakturus.Track.Backend.Services;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Fakturus.Track.Backend.Endpoints.Reports;

public class ExportReportEndpoint(IExportService exportService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("v{version:apiVersion}/reports/export");

        Policies("RequireAuthentication");

        Options(x => x
            .WithVersionSet("FakturusTrack")
            .MapToApiVersion(1.0));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var format = Query<string>("format") ?? "csv";
        var period = Query<string>("period") ?? "month";
        var year = Query<int?>("year") ?? DateTime.UtcNow.Year;
        var month = Query<int?>("month", isRequired: false);
        var quarter = Query<int?>("quarter", isRequired: false);

        try
        {
            var userId = User.GetObjectId();
            var result = await exportService.GenerateExportAsync(userId, format, period, year, month, quarter);

            HttpContext.Response.ContentType = result.ContentType;
            HttpContext.Response.Headers.Append("Content-Disposition",
                $"attachment; filename=\"{result.FileName}\"");
            HttpContext.Response.Headers.ContentLength = result.Data.Length;

            await HttpContext.Response.Body.WriteAsync(result.Data, ct);
        }
        catch (ArgumentException ex)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new { Error = ex.Message }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating export");
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "An error occurred while generating the export" }, ct);
        }
    }
}

namespace Fakturus.Track.Backend.Services;

public interface IExportService
{
    Task<ExportResult> GenerateExportAsync(string userId, string format, string period, int year, int? month = null, int? quarter = null);
}

public record ExportResult(byte[] Data, string ContentType, string FileName);

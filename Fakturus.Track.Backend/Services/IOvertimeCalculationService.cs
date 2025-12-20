using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface IOvertimeCalculationService
{
    Task<OvertimeSummaryDto> CalculateOvertimeAsync(string userId, int? year = null);
}


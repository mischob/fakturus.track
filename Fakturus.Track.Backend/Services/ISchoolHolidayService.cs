using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface ISchoolHolidayService
{
    Task<List<SchoolHolidayPeriodDto>> GetSchoolHolidayPeriodsAsync(string userId, int? year = null);
    Task<SchoolHolidayPeriodDto> CreateSchoolHolidayPeriodAsync(string userId, CreateSchoolHolidayPeriodRequest request);
    Task<SchoolHolidayPeriodDto> UpdateSchoolHolidayPeriodAsync(string userId, Guid id, UpdateSchoolHolidayPeriodRequest request);
    Task DeleteSchoolHolidayPeriodAsync(string userId, Guid id);
    bool IsDateInSchoolHoliday(DateOnly date, List<SchoolHolidayPeriodDto> periods);
}


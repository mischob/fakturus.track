namespace Fakturus.Track.Backend.Services;

public interface IHolidayService
{
    List<DateOnly> GetHolidaysForYear(string bundesland, int year);
    bool IsHoliday(DateOnly date, string bundesland);
}


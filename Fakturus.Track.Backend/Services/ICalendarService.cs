using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface ICalendarService
{
    Task<List<CalendarEventDto>> GetCalendarEventsAsync(string userId);
}

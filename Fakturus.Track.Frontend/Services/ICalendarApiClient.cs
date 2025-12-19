using Fakturus.Track.Frontend.Models;
using Refit;

namespace Fakturus.Track.Frontend.Services;

[Headers("Authorization: Bearer")]
public interface ICalendarApiClient
{
    [Get("/v1/calendar/events")]
    Task<List<CalendarEventModel>> GetCalendarEventsAsync();
}

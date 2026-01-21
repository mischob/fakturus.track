using Fakturus.Track.Mobile.Shared.Models;

namespace Fakturus.Track.Mobile.Services.Api;

public interface ICalendarApiClient
{
    [Get("/v1/calendar/events")]
    Task<List<CalendarEventModel>> GetCalendarEventsAsync();
}
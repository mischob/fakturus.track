using Fakturus.Track.Frontend.Models;

namespace Fakturus.Track.Frontend.Services;

public interface ICalendarModalService
{
    event EventHandler<bool>? VisibilityChanged;
    event EventHandler<List<CalendarEventModel>>? EventsImported;

    void Show();
    void Hide();
    void NotifyImport(List<CalendarEventModel> events);
}
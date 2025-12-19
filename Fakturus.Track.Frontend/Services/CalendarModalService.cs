using Fakturus.Track.Frontend.Models;

namespace Fakturus.Track.Frontend.Services;

public class CalendarModalService : ICalendarModalService
{
    public event EventHandler<bool>? VisibilityChanged;
    public event EventHandler<List<CalendarEventModel>>? EventsImported;

    public void Show()
    {
        VisibilityChanged?.Invoke(this, true);
    }

    public void Hide()
    {
        VisibilityChanged?.Invoke(this, false);
    }

    public void NotifyImport(List<CalendarEventModel> events)
    {
        EventsImported?.Invoke(this, events);
    }
}

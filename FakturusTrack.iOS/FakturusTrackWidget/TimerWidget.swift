import WidgetKit
import SwiftUI

struct TimerWidget: Widget {
    let kind = "FakturusTrackTimerWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: TimerWidgetProvider()) { entry in
            TimerWidgetView(entry: entry)
                .containerBackground(.fill.tertiary, for: .widget)
        }
        .configurationDisplayName(String(localized: "widget_timer_title"))
        .description(String(localized: "widget_timer_description"))
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}

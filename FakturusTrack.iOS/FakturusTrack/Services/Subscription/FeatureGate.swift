import Foundation

enum FeatureGate: CaseIterable, Sendable {
    // STARTER Features
    case pdfExport
    case csvExport
    case sickDays
    case vacation

    // PRO Features
    case datevExport
    case schoolHolidays
    case calendarIntegration

    var requiredTier: Tier {
        switch self {
        case .pdfExport, .csvExport,
             .sickDays, .vacation:
            return .starter
        case .datevExport, .schoolHolidays, .calendarIntegration:
            return .pro
        }
    }

    /// Lokalisierter Feature-Name fuer Paywall
    var displayName: String {
        switch self {
        case .pdfExport: String(localized: "feature_pdf_export")
        case .csvExport: String(localized: "feature_csv_export")
        case .sickDays: String(localized: "feature_sick_days")
        case .vacation: String(localized: "feature_vacation")
        case .datevExport: String(localized: "feature_datev_export")
        case .schoolHolidays: String(localized: "feature_school_holidays")
        case .calendarIntegration: String(localized: "feature_calendar_integration")
        }
    }
}

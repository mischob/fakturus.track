package com.fakturus.track.services.subscription

import com.fakturus.track.R

enum class FeatureGate(
    val requiredTier: Tier,
    val displayNameRes: Int
) {
    // STARTER
    PDF_EXPORT(Tier.STARTER, R.string.feature_pdf_export),
    CSV_EXPORT(Tier.STARTER, R.string.feature_csv_export),
    SICK_DAYS(Tier.STARTER, R.string.feature_sick_days),
    VACATION(Tier.STARTER, R.string.feature_vacation),

    // PRO
    DATEV_EXPORT(Tier.PRO, R.string.feature_datev_export),
    SCHOOL_HOLIDAYS(Tier.PRO, R.string.feature_school_holidays),
    CALENDAR_INTEGRATION(Tier.PRO, R.string.feature_calendar_integration);
}

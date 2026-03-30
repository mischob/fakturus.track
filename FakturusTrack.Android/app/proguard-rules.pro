# Ktor
-keep class io.ktor.** { *; }
-dontwarn io.ktor.**

# MSAL
-keep class com.microsoft.identity.** { *; }

# Room Entities
-keep class com.fakturus.track.models.* { *; }

# Google Play Billing
-keep class com.android.vending.billing.** { *; }
-keep class com.android.billingclient.** { *; }

# kotlinx.serialization
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.AnnotationsKt
-keepclassmembers class com.fakturus.track.models.** {
    *** Companion;
}

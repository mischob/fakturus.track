namespace Fakturus.Track.Backend.DTOs;

public record UserSettingsDto(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek,
    int WorkDays,
    string Bundesland,
    /// <summary>
    /// Last server-side modification of the User row. Used by clients as the
    /// reference timestamp for last-write-wins sync. Without this iOS treats
    /// the local copy as always-newer and re-uploads on every sync.
    /// </summary>
    DateTime? UpdatedAt = null
);

public record UpdateUserSettingsRequest(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek,
    int WorkDays,
    string Bundesland,
    /// <summary>
    /// Effective date for changes to <c>WorkDays</c> / <c>WorkHoursPerWeek</c>.
    /// Defaults to today on the server when null. Used to write a new
    /// <c>UserSettingsHistory</c> row so historical calculations remain correct.
    /// </summary>
    DateOnly? EffectiveDate = null
);

public record UserSettingsHistoryEntryDto(
    Guid Id,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    int WorkDays,
    decimal WorkHoursPerWeek
);

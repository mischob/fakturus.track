namespace Fakturus.Track.Backend.DTOs;

public record UserSettingsDto(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek,
    int WorkDays,
    string Bundesland
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

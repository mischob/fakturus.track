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
    string Bundesland
);
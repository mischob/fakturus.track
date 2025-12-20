namespace Fakturus.Track.Backend.DTOs;

public record UserSettingsDto(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek,
    int WorkDays
);

public record UpdateUserSettingsRequest(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek,
    int WorkDays
);


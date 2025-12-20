namespace Fakturus.Track.Backend.DTOs;

public record UserSettingsDto(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek
);

public record UpdateUserSettingsRequest(
    int VacationDaysPerYear,
    decimal WorkHoursPerWeek
);


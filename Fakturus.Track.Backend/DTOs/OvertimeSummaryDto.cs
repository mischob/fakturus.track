namespace Fakturus.Track.Backend.DTOs;

public record OvertimeSummaryDto(
    decimal TotalOvertimeHours,
    List<MonthlyOvertimeDto> MonthlyOvertime,
    int VacationDaysTaken,
    int VacationDaysRemaining,
    int VacationDaysPerYear,
    int HolidaysTaken
);

public record MonthlyOvertimeDto(
    int Year,
    int Month,
    string MonthName,
    decimal OvertimeHours,
    decimal WorkedHours,
    decimal ExpectedHours
);


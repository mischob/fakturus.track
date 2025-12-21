namespace Fakturus.Track.Backend.DTOs;

public record SchoolHolidayPeriodDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int Year,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateSchoolHolidayPeriodRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate
);

public record UpdateSchoolHolidayPeriodRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate
);


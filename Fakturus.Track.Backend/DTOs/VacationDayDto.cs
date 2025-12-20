namespace Fakturus.Track.Backend.DTOs;

public record VacationDayDto(
    Guid Id,
    DateOnly Date,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SyncedAt
);

public record CreateVacationDayRequest(DateOnly Date);

public record SyncVacationDaysRequest(List<VacationDayDto> VacationDays);

public record SyncVacationDaysResponse(
    List<VacationDayDto> ServerVacationDays,
    List<Guid> DeletedIds
);


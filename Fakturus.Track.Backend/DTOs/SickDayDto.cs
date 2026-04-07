namespace Fakturus.Track.Backend.DTOs;

public record SickDayDto(
    Guid Id,
    DateOnly Date,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SyncedAt
);

public record CreateSickDayRequest(DateOnly Date);

public record SyncSickDaysRequest(List<SickDayDto> SickDays);

public record SyncSickDaysResponse(
    List<SickDayDto> ServerSickDays,
    List<Guid> DeletedIds
);

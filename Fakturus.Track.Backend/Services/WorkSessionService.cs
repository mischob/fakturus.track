using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class WorkSessionService : IWorkSessionService
{
    private readonly ApplicationDbContext _context;

    public WorkSessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkSessionDto>> GetWorkSessionsByUserIdAsync(string userId)
    {
        var workSessions = await _context.WorkSessions
            .Where(ws => ws.UserId == userId)
            .OrderByDescending(ws => ws.Date)
            .ThenByDescending(ws => ws.StartTime)
            .ToListAsync();

        return workSessions.Select(MapToDto).ToList();
    }

    public async Task<WorkSessionDto?> GetWorkSessionByIdAsync(Guid id, string userId)
    {
        var workSession = await _context.WorkSessions
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

        return workSession == null ? null : MapToDto(workSession);
    }

    public async Task<WorkSessionDto> CreateWorkSessionAsync(CreateWorkSessionRequest request, string userId)
    {
        var workSession = new WorkSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = request.Date,
            StartTime = request.StartTime.ToUniversalTime(),
            StopTime = request.StopTime?.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow
        };

        _context.WorkSessions.Add(workSession);
        await _context.SaveChangesAsync();

        return MapToDto(workSession);
    }

    public async Task<WorkSessionDto> UpdateWorkSessionAsync(Guid id, UpdateWorkSessionRequest request, string userId)
    {
        var workSession = await _context.WorkSessions
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

        if (workSession == null)
            throw new InvalidOperationException($"WorkSession with id {id} not found");

        if (request.Date.HasValue)
            workSession.Date = request.Date.Value;

        if (request.StartTime.HasValue)
            workSession.StartTime = request.StartTime.Value.ToUniversalTime();

        if (request.StopTime.HasValue)
            workSession.StopTime = request.StopTime.Value.ToUniversalTime();

        workSession.UpdatedAt = DateTime.UtcNow;
        workSession.SyncedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(workSession);
    }

    public async Task<bool> DeleteWorkSessionAsync(Guid id, string userId)
    {
        var workSession = await _context.WorkSessions
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

        if (workSession == null)
            return false;

        _context.WorkSessions.Remove(workSession);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<WorkSessionDto>> SyncWorkSessionsAsync(List<CreateWorkSessionRequest> workSessions, string userId)
    {
        var syncedSessions = new List<WorkSessionDto>();

        foreach (var request in workSessions)
        {
            var workSession = new WorkSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = request.Date,
                StartTime = request.StartTime.ToUniversalTime(),
                StopTime = request.StopTime?.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedAt = DateTime.UtcNow
            };

            _context.WorkSessions.Add(workSession);
            syncedSessions.Add(MapToDto(workSession));
        }

        await _context.SaveChangesAsync();

        return syncedSessions;
    }

    private static WorkSessionDto MapToDto(WorkSession workSession)
    {
        return new WorkSessionDto
        {
            Id = workSession.Id,
            UserId = workSession.UserId,
            Date = workSession.Date,
            StartTime = workSession.StartTime,
            StopTime = workSession.StopTime,
            CreatedAt = workSession.CreatedAt,
            UpdatedAt = workSession.UpdatedAt,
            SyncedAt = workSession.SyncedAt
        };
    }
}


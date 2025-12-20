using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class WorkSessionService(ApplicationDbContext context) : IWorkSessionService
{
    public async Task<List<WorkSessionDto>> GetWorkSessionsByUserIdAsync(string userId)
    {
        var workSessions = await context.WorkSessions
            .Where(ws => ws.UserId == userId)
            .OrderByDescending(ws => ws.Date)
            .ThenByDescending(ws => ws.StartTime)
            .ToListAsync();

        return workSessions.Select(MapToDto).ToList();
    }

    public async Task<WorkSessionDto?> GetWorkSessionByIdAsync(Guid id, string userId)
    {
        var workSession = await context.WorkSessions
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

        return workSession == null ? null : MapToDto(workSession);
    }

    public async Task<WorkSessionDto> CreateWorkSessionAsync(CreateWorkSessionRequest request, string userId)
    {
        // Check for duplicate work session (same date and start time within 5 minutes)
        var existingSession = await FindDuplicateWorkSessionAsync(
            userId,
            request.Date,
            request.StartTime.ToUniversalTime());

        if (existingSession != null)
        {
            // Update existing session instead of creating new one
            existingSession.StopTime = request.StopTime?.ToUniversalTime();
            existingSession.UpdatedAt = DateTime.UtcNow;
            existingSession.SyncedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return MapToDto(existingSession);
        }

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

        context.WorkSessions.Add(workSession);
        await context.SaveChangesAsync();

        return MapToDto(workSession);
    }

    public async Task<WorkSessionDto> UpdateWorkSessionAsync(Guid id, UpdateWorkSessionRequest request, string userId)
    {
        var workSession = await context.WorkSessions
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

        await context.SaveChangesAsync();

        return MapToDto(workSession);
    }

    public async Task<bool> DeleteWorkSessionAsync(Guid id, string userId)
    {
        var workSession = await context.WorkSessions
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);

        if (workSession == null)
            return false;

        context.WorkSessions.Remove(workSession);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<List<WorkSessionDto>> SyncWorkSessionsAsync(List<CreateWorkSessionRequest> workSessions,
        string userId)
    {
        // Process each work session from client (upsert logic)
        foreach (var request in workSessions)
        {
            // Check if session with this UUID already exists
            var existingSession = await context.WorkSessions
                .FirstOrDefaultAsync(ws => ws.Id == request.Id && ws.UserId == userId);

            if (existingSession != null)
            {
                // Update existing session
                existingSession.Date = request.Date;
                existingSession.StartTime = request.StartTime.ToUniversalTime();
                existingSession.StopTime = request.StopTime?.ToUniversalTime();
                existingSession.UpdatedAt = DateTime.UtcNow;
                existingSession.SyncedAt = DateTime.UtcNow;
            }
            else
            {
                // Check for duplicate by date/time before creating
                var duplicateSession = await FindDuplicateWorkSessionAsync(
                    userId,
                    request.Date,
                    request.StartTime.ToUniversalTime());

                if (duplicateSession != null)
                {
                    // Update the duplicate instead of creating new
                    duplicateSession.StopTime = request.StopTime?.ToUniversalTime();
                    duplicateSession.UpdatedAt = DateTime.UtcNow;
                    duplicateSession.SyncedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new session with client-provided UUID
                    var workSession = new WorkSession
                    {
                        Id = request.Id, // Use client-generated UUID
                        UserId = userId,
                        Date = request.Date,
                        StartTime = request.StartTime.ToUniversalTime(),
                        StopTime = request.StopTime?.ToUniversalTime(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        SyncedAt = DateTime.UtcNow
                    };

                    context.WorkSessions.Add(workSession);
                }
            }
        }

        await context.SaveChangesAsync();

        // Return ALL user's work sessions (backend is source of truth)
        return await GetWorkSessionsByUserIdAsync(userId);
    }

    private async Task<WorkSession?> FindDuplicateWorkSessionAsync(string userId, DateOnly date, DateTime startTime)
    {
        // Find work session with same date and start time within ±5 minutes
        var toleranceMinutes = 5;
        var startTimeMin = startTime.AddMinutes(-toleranceMinutes);
        var startTimeMax = startTime.AddMinutes(toleranceMinutes);

        return await context.WorkSessions
            .FirstOrDefaultAsync(ws =>
                ws.UserId == userId &&
                ws.Date == date &&
                ws.StartTime >= startTimeMin &&
                ws.StartTime <= startTimeMax);
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
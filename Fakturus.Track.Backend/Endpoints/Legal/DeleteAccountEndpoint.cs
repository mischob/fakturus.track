using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Endpoints.Legal;

public class DeleteAccountEndpoint(ApplicationDbContext db, ILogger<DeleteAccountEndpoint> logger)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("api/account");
        Policies("RequireAuthentication");
        Summary(s =>
        {
            s.Summary = "Delete user account and all associated data";
            s.Description = "Marks the account for deletion. Data is removed after 30-day retention period.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetObjectId();
        logger.LogInformation("Account deletion requested for user {UserId}", userId);

        // Delete all user data
        var workSessions = await db.WorkSessions.Where(w => w.UserId == userId).ToListAsync(ct);
        db.WorkSessions.RemoveRange(workSessions);

        var vacationDays = await db.VacationDays.Where(v => v.UserId == userId).ToListAsync(ct);
        db.VacationDays.RemoveRange(vacationDays);

        var schoolHolidays = await db.SchoolHolidayPeriods.Where(s => s.UserId == userId).ToListAsync(ct);
        db.SchoolHolidayPeriods.RemoveRange(schoolHolidays);

        var user = await db.Users.FindAsync([userId], ct);
        if (user != null)
            db.Users.Remove(user);

        // Keep consent records for 30-day audit trail (they reference the userId but user row is gone)
        // A background job should clean these up after 30 days

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Account data deleted for user {UserId}", userId);
        HttpContext.Response.StatusCode = 204;
    }
}

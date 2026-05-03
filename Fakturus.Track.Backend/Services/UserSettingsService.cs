using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Backend.Services;

public class UserSettingsService(ApplicationDbContext context) : IUserSettingsService
{
    public async Task<UserSettingsDto> GetUserSettingsAsync(string userId)
    {
        var user = await context.Users.FindAsync(userId);

        if (user == null)
        {
            user = new User
            {
                Id = userId,
                VacationDaysPerYear = 30,
                WorkHoursPerWeek = 40,
                WorkDays = 31,
                Bundesland = "NW",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await EnsureInitialHistoryRowAsync(user);
            await context.SaveChangesAsync();
        }

        return new UserSettingsDto(
            user.VacationDaysPerYear,
            user.WorkHoursPerWeek,
            user.WorkDays,
            user.Bundesland,
            user.UpdatedAt
        );
    }

    public async Task<UserSettingsDto> UpdateUserSettingsAsync(UpdateUserSettingsRequest request, string userId)
    {
        var user = await context.Users.FindAsync(userId);
        var isNewUser = user == null;

        if (user == null)
        {
            user = new User
            {
                Id = userId,
                VacationDaysPerYear = request.VacationDaysPerYear,
                WorkHoursPerWeek = request.WorkHoursPerWeek,
                WorkDays = request.WorkDays,
                Bundesland = request.Bundesland,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
        }
        else
        {
            user.VacationDaysPerYear = request.VacationDaysPerYear;
            user.WorkHoursPerWeek = request.WorkHoursPerWeek;
            user.WorkDays = request.WorkDays;
            user.Bundesland = request.Bundesland;
            user.UpdatedAt = DateTime.UtcNow;
        }

        // Stage 2: write a history row so future overtime calculations know
        // when the WorkDays / WorkHoursPerWeek values became effective.
        var effectiveDate = request.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        await UpsertHistoryAsync(userId, effectiveDate, request.WorkDays, request.WorkHoursPerWeek, isNewUser);

        await context.SaveChangesAsync();

        return new UserSettingsDto(
            user.VacationDaysPerYear,
            user.WorkHoursPerWeek,
            user.WorkDays,
            user.Bundesland,
            user.UpdatedAt
        );
    }

    public async Task<List<UserSettingsHistoryEntryDto>> GetUserSettingsHistoryAsync(string userId)
    {
        var rows = await context.UserSettingsHistory
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ValidFrom)
            .ToListAsync();

        return rows
            .Select(h => new UserSettingsHistoryEntryDto(h.Id, h.ValidFrom, h.ValidTo, h.WorkDays, h.WorkHoursPerWeek))
            .ToList();
    }

    // ---- private helpers ----------------------------------------------------

    /// <summary>
    /// Inserts a fresh history row for a brand-new user. For existing users,
    /// the migration backfill provides the initial row.
    /// </summary>
    private async Task EnsureInitialHistoryRowAsync(User user)
    {
        var hasAny = await context.UserSettingsHistory.AnyAsync(h => h.UserId == user.Id);
        if (hasAny) return;

        context.UserSettingsHistory.Add(new UserSettingsHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ValidFrom = new DateOnly(2000, 1, 1),
            ValidTo = null,
            WorkDays = user.WorkDays,
            WorkHoursPerWeek = user.WorkHoursPerWeek,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Inserts a new history row that takes effect on <paramref name="effectiveDate"/>.
    /// Any earlier open-ended row is sealed (ValidTo = effectiveDate - 1).
    /// Any rows beginning on/after <paramref name="effectiveDate"/> are removed
    /// — backdating an effective date "rewrites" forward history, which keeps
    /// the timeline contiguous and well-ordered.
    ///
    /// If the requested values match the row currently active on
    /// <paramref name="effectiveDate"/>, this is a no-op (avoids piling up
    /// history rows when the user opens settings and saves without changes).
    /// </summary>
    private async Task UpsertHistoryAsync(string userId, DateOnly effectiveDate, int workDays, decimal workHoursPerWeek, bool isNewUser)
    {
        // Ensure the timeline exists (legacy users without backfill).
        var existing = await context.UserSettingsHistory
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.ValidFrom)
            .ToListAsync();

        if (existing.Count == 0 && !isNewUser)
        {
            // Legacy account without a history row. Seed one valid from the
            // distant past with the values that were active before this
            // update; we'll then split it normally below.
            var seed = new UserSettingsHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ValidFrom = new DateOnly(2000, 1, 1),
                ValidTo = null,
                WorkDays = workDays,
                WorkHoursPerWeek = workHoursPerWeek,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserSettingsHistory.Add(seed);
            return;
        }

        // No-op short circuit
        var active = existing.LastOrDefault(h => h.ValidFrom <= effectiveDate && (h.ValidTo == null || h.ValidTo >= effectiveDate));
        if (active != null && active.WorkDays == workDays && active.WorkHoursPerWeek == workHoursPerWeek)
        {
            // Drop any future-dated rows that contradict this no-op (defensive
            // cleanup; they would only exist if a previous future-effective
            // change was scheduled and now no longer applies).
            var futureRows = existing.Where(h => h.ValidFrom > effectiveDate).ToList();
            foreach (var f in futureRows) context.UserSettingsHistory.Remove(f);
            if (futureRows.Count > 0 && active.ValidTo != null)
            {
                active.ValidTo = null;
                active.UpdatedAt = DateTime.UtcNow;
            }
            return;
        }

        // Remove any rows starting on/after effectiveDate (backdating rewrites
        // forward history). Anything strictly later than effectiveDate's
        // currently active row gets dropped — including the active row itself
        // if its ValidFrom is on/after effectiveDate.
        var toRemove = existing.Where(h => h.ValidFrom >= effectiveDate).ToList();
        foreach (var r in toRemove) context.UserSettingsHistory.Remove(r);

        // Seal the still-active row (the one whose ValidFrom < effectiveDate).
        var sealRow = existing.LastOrDefault(h => h.ValidFrom < effectiveDate && !toRemove.Contains(h));
        if (sealRow != null)
        {
            sealRow.ValidTo = effectiveDate.AddDays(-1);
            sealRow.UpdatedAt = DateTime.UtcNow;
        }

        // Insert the new row.
        context.UserSettingsHistory.Add(new UserSettingsHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ValidFrom = effectiveDate,
            ValidTo = null,
            WorkDays = workDays,
            WorkHoursPerWeek = workHoursPerWeek,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}

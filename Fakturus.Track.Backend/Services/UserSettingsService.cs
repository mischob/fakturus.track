using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.Data.Entities;
using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public class UserSettingsService(ApplicationDbContext context) : IUserSettingsService
{
    public async Task<UserSettingsDto> GetUserSettingsAsync(string userId)
    {
        var user = await context.Users.FindAsync(userId);

        if (user == null)
        {
            // Create user with default settings if not exists
            user = new User
            {
                Id = userId,
                VacationDaysPerYear = 30,
                WorkHoursPerWeek = 40,
                WorkDays = 31, // Mo-Fr
                Bundesland = "NW",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        return new UserSettingsDto(
            user.VacationDaysPerYear,
            user.WorkHoursPerWeek,
            user.WorkDays,
            user.Bundesland
        );
    }

    public async Task<UserSettingsDto> UpdateUserSettingsAsync(UpdateUserSettingsRequest request, string userId)
    {
        var user = await context.Users.FindAsync(userId);

        if (user == null)
        {
            // Create user if not exists
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

        await context.SaveChangesAsync();

        return new UserSettingsDto(
            user.VacationDaysPerYear,
            user.WorkHoursPerWeek,
            user.WorkDays,
            user.Bundesland
        );
    }
}
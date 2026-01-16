using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fakturus.Track.Mobile.Services.Offline;

public class UserSettingsService : OfflineDataService<UserSettingsEntity>, IUserSettingsService
{
    public UserSettingsService(MobileDbContext context) : base(context)
    {
    }

    public async Task<UserSettingsEntity?> GetByUserIdAsync(string userId)
    {
        return await Context.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId);
    }

    public async Task<UserSettingsEntity> GetOrCreateAsync(string userId)
    {
        var settings = await GetByUserIdAsync(userId);
        if (settings != null)
            return settings;

        settings = new UserSettingsEntity
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await AddAsync(settings);
    }
}

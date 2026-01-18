using Fakturus.Track.Mobile.Data;
using Fakturus.Track.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fakturus.Track.Mobile.Services.Offline;

public class UserSettingsService : OfflineDataService<UserSettingsEntity>, IUserSettingsService
{
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(MobileDbContext context, ILogger<UserSettingsService> logger) 
        : base(context, logger)
    {
        _logger = logger;
    }

    public async Task<UserSettingsEntity?> GetByUserIdAsync(string userId)
    {
        _logger.LogDebug("[Database] [UserSettings] GetByUserIdAsync - UserId: {UserId}", userId);
        try
        {
            var result = await Context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId);
            _logger.LogDebug("[Database] [UserSettings] GetByUserIdAsync completed - Found: {Found}", result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [UserSettings] Error in GetByUserIdAsync - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSettingsEntity> GetOrCreateAsync(string userId)
    {
        _logger.LogDebug("[Database] [UserSettings] GetOrCreateAsync - UserId: {UserId}", userId);
        try
        {
            var settings = await GetByUserIdAsync(userId);
            if (settings != null)
            {
                _logger.LogDebug("[Database] [UserSettings] GetOrCreateAsync - Existing settings found");
                return settings;
            }

            _logger.LogDebug("[Database] [UserSettings] GetOrCreateAsync - Creating new settings");
            settings = new UserSettingsEntity
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await AddAsync(settings);
            _logger.LogInformation("[Database] [UserSettings] GetOrCreateAsync - New settings created");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Database] [UserSettings] Error in GetOrCreateAsync - UserId: {UserId}", userId);
            throw;
        }
    }
}
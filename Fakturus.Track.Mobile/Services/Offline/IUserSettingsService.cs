using Fakturus.Track.Mobile.Data.Entities;

namespace Fakturus.Track.Mobile.Services.Offline;

public interface IUserSettingsService : IOfflineDataService<UserSettingsEntity>
{
    Task<UserSettingsEntity?> GetByUserIdAsync(string userId);
    Task<UserSettingsEntity> GetOrCreateAsync(string userId);
}

using Fakturus.Track.Backend.DTOs;

namespace Fakturus.Track.Backend.Services;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetUserSettingsAsync(string userId);
    Task<UserSettingsDto> UpdateUserSettingsAsync(UpdateUserSettingsRequest request, string userId);
}
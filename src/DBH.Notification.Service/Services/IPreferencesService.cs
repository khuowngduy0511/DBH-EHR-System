using DBH.Notification.Service.DTOs;

namespace DBH.Notification.Service.Services;

public interface IPreferencesService
{
    Task<PreferencesResponse> GetPreferencesAsync(string userDid);
    Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request);
}

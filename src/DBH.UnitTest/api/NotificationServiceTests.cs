using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DBH.UnitTest.Api;

/// <summary>
/// API integration tests for DBH.Notification.Service
/// Covers: NotificationsController, DeviceTokensController, PreferencesController
/// </summary>
public class NotificationServiceTests : ApiTestBase
{
    // =========================================================================
    // NOTIFICATIONS
    // =========================================================================

    [Fact]
    public async Task SendNotification_ToSeedUser_ShouldReturnMessage()
    {
        var request = new { recipientDid = $"did:dbh:user:{TestSeedData.PatientUserId}", title = "Test Notification", body = "This is a test", type = "System" };
        var response = await NotificationClient.PostAsJsonAsync(ApiEndpoints.Notifications.Send, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task BroadcastNotification_AsAdmin_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var request = new { title = "System Maintenance", body = "Scheduled downtime at midnight", type = "System" };
        var response = await NotificationClient.PostAsJsonAsync(ApiEndpoints.Notifications.Broadcast, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task GetNotificationsByUser_ShouldReturnPagedResult()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var userDid = $"did:dbh:user:{TestSeedData.PatientUserId}";
        var response = await NotificationClient.GetAsync($"{ApiEndpoints.Notifications.ByUser(userDid)}?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnNumericValue()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
        var response = await NotificationClient.GetAsync(ApiEndpoints.Notifications.UnreadCount(userDid));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task MarkAsRead_WithFakeIds_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
        var request = new { notificationIds = new[] { Guid.NewGuid() } };
        var response = await NotificationClient.PostAsJsonAsync(ApiEndpoints.Notifications.MarkRead(userDid), request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task DeleteNotification_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var response = await NotificationClient.DeleteAsync(ApiEndpoints.Notifications.Delete(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // DEVICE TOKENS
    // =========================================================================

    [Fact]
    public async Task RegisterDevice_ForSeedUser_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var request = new { userDid = $"did:dbh:user:{TestSeedData.AdminUserId}", token = $"test-fcm-token-{Guid.NewGuid():N}", platform = "Android", deviceName = "Test Phone" };
        var response = await NotificationClient.PostAsJsonAsync(ApiEndpoints.DeviceTokens.Register, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task GetUserDevices_ForSeedUser_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
        var response = await NotificationClient.GetAsync(ApiEndpoints.DeviceTokens.ByUser(userDid));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateDevice_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var response = await NotificationClient.DeleteAsync(ApiEndpoints.DeviceTokens.Deactivate(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PREFERENCES
    // =========================================================================

    [Fact]
    public async Task GetPreferences_ForSeedUser_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
        var response = await NotificationClient.GetAsync(ApiEndpoints.Preferences.Get(userDid));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task UpdatePreferences_ForSeedUser_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(NotificationClient);
        var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
        var request = new { emailEnabled = true, pushEnabled = true, smsEnabled = false };
        var response = await NotificationClient.PutAsJsonAsync(ApiEndpoints.Preferences.Update(userDid), request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}

using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DBH.Notification.Service.Services;
using DBH.Notification.Service.DbContext;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Enums;
using DBH.Notification.Service.Models.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBH.UnitTest.UnitTests;

public class NotificationServiceDirectTests
{
    private readonly DbContextOptions<NotificationDbContext> _dbContextOptions;
    private readonly Mock<ILogger<NotificationService>> _loggerMock = new();
    private readonly ITestOutputHelper _output;

    public NotificationServiceDirectTests(ITestOutputHelper output)
    {
        _output = output;
        _dbContextOptions = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private NotificationService CreateService(NotificationDbContext context)
    {
        return new NotificationService(context, _loggerMock.Object);
    }

    // #region SendNotificationAsync Tests

    // [Fact(DisplayName = "SendNotificationAsync::SendNotificationAsync-01")]
    // public async Task SendNotificationAsync_SendNotificationAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid request provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var request = new SendNotificationRequest
    //     {
    //         RecipientDid = "did:example:test-user-123",
    //         RecipientUserId = Guid.NewGuid(),
    //         Title = "Test Notification",
    //         Body = "This is a test notification body",
    //         Priority = NotificationPriority.Normal,
    //         Channel = NotificationChannel.InApp
    //     };

    //     // Act
    //     var result = await service.SendNotificationAsync(request);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    //     Assert.NotNull(result.Data);
    //     Assert.Equal(request.Title, result.Data.Title);
    //     Assert.Equal(request.RecipientDid, result.Data.RecipientDid);
    // }

    // [Fact(DisplayName = "SendNotificationAsync::SendNotificationAsync-02")]
    // public async Task SendNotificationAsync_SendNotificationAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: request with missing required fields
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var request = new SendNotificationRequest
    //     {
    //         // Missing required fields: RecipientDid and Title
    //         Body = "Test body"
    //     };

    //     // Act
    //     var result = await service.SendNotificationAsync(request);

    //     // Assert
    //     // Expected Return: Returns validation error (400 or 422) or equivalent domain error
    //     Assert.NotNull(result);
    //     Assert.False(result.Success);
    // }

    // #endregion

    // #region BroadcastNotificationAsync Tests

    // [Fact(DisplayName = "BroadcastNotificationAsync::BroadcastNotificationAsync-01")]
    // public async Task BroadcastNotificationAsync_BroadcastNotificationAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid request provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var request = new BroadcastNotificationRequest
    //     {
    //         RecipientDids = new List<string> { "did:example:user1", "did:example:user2", "did:example:user3" },
    //         Title = "Broadcast Test",
    //         Body = "Broadcast body",
    //         Priority = NotificationPriority.Normal
    //     };

    //     // Act
    //     var result = await service.BroadcastNotificationAsync(request);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    //     Assert.Equal(3, result.Data);
    // }

    // [Fact(DisplayName = "BroadcastNotificationAsync::BroadcastNotificationAsync-02")]
    // public async Task BroadcastNotificationAsync_BroadcastNotificationAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: request with missing required fields
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var request = new BroadcastNotificationRequest
    //     {
    //         // Missing Title and Body
    //         RecipientDids = new List<string> { "did:example:user1" }
    //     };

    //     // Act
    //     var result = await service.BroadcastNotificationAsync(request);

    //     // Assert
    //     // Expected Return: Returns validation error (400 or 422) or equivalent domain error
    //     Assert.NotNull(result);
    //     Assert.False(result.Success);
    // }

    // #endregion

    #region GetNotificationsByUserAsync Tests

    [Fact(DisplayName = "GetNotificationsByUserAsync::GetNotificationsByUserAsync-01")]
    public async Task GetNotificationsByUserAsync_GetNotificationsByUserAsync_01_1()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid userDid, page, pageSize provided
        using var context = new NotificationDbContext(_dbContextOptions);
        var service = CreateService(context);
        
        var userDid = "did:example:test-user";
        
        // Create test notifications
        for (int i = 0; i < 5; i++)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                RecipientDid = userDid,
                Title = $"Notification {i}",
                Body = $"Body {i}",
                Priority = NotificationPriority.Normal,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);
        }
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetNotificationsByUserAsync(userDid, 1, 10);
        Console.WriteLine("GetNotificationsByUserAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.Equal(5, result.Data.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact(DisplayName = "GetNotificationsByUserAsync::GetNotificationsByUserAsync-02")]
    public async Task GetNotificationsByUserAsync_GetNotificationsByUserAsync_02_2()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: userDid = null/empty OR page <= 0 OR pageSize <= 0
        using var context = new NotificationDbContext(_dbContextOptions);
        var service = CreateService(context);

        // Act & Assert - Invalid userDid
        var resultEmptyDid = await service.GetNotificationsByUserAsync("", 1, 10);
        Console.WriteLine("GetNotificationsByUserAsync response: " + JsonSerializer.Serialize(resultEmptyDid, new JsonSerializerOptions { WriteIndented = true }));
        Assert.NotNull(resultEmptyDid);
        Assert.Empty(resultEmptyDid.Data);

        var resultNegativePage = await service.GetNotificationsByUserAsync("did:example:user", -1, 10);

        Console.WriteLine("GetNotificationsByUserAsync response: " + JsonSerializer.Serialize(resultNegativePage, new JsonSerializerOptions { WriteIndented = true }));
        Assert.NotNull(resultNegativePage);
        Assert.Empty(resultNegativePage.Data);

        var resultNegativePageSize = await service.GetNotificationsByUserAsync("did:example:user", 1, -1);

        Console.WriteLine("GetNotificationsByUserAsync response: " + JsonSerializer.Serialize(resultNegativePageSize, new JsonSerializerOptions { WriteIndented = true }));
        Assert.NotNull(resultNegativePageSize);
        Assert.Empty(resultNegativePageSize.Data);
    }

    [Fact(DisplayName = "GetNotificationsByUserAsync::GetNotificationsByUserAsync-03")]
    public async Task GetNotificationsByUserAsync_GetNotificationsByUserAsync_03_3()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: Requested resource not found
        using var context = new NotificationDbContext(_dbContextOptions);
        var service = CreateService(context);
        
        var nonExistentUserDid = "did:example:non-existent-user";

        // Act
        var result = await service.GetNotificationsByUserAsync(nonExistentUserDid, 1, 10);
        Console.WriteLine("GetNotificationsByUserAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.Count);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact(DisplayName = "GetNotificationsByUserAsync::GetNotificationsByUserAsync-04")]
    public async Task GetNotificationsByUserAsync_GetNotificationsByUserAsync_04_4()
    {
        // Arrange
        // Condition: PagingBoundary
        // Input: page = 9999, pageSize = 10 (out of range)
        using var context = new NotificationDbContext(_dbContextOptions);
        var service = CreateService(context);
        
        var userDid = "did:example:test-user";
        
        // Create test notifications
        for (int i = 0; i < 5; i++)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                RecipientDid = userDid,
                Title = $"Notification {i}",
                Body = $"Body {i}",
                Priority = NotificationPriority.Normal,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);
        }
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetNotificationsByUserAsync(userDid, 9999, 10);
        Console.WriteLine("GetNotificationsByUserAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        // Assert
        // Expected Return: Returns valid paging metadata; out-of-range page returns empty item set
        // Assert
        // Expected Return: Returns valid paging metadata; out-of-range page returns empty item set
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.Count); // Empty item set for out-of-range page
        Assert.Equal(5, result.TotalCount); // Total count still valid
    }

    #endregion

    // #region GetUnreadNotificationsAsync Tests

    // [Fact(DisplayName = "GetUnreadNotificationsAsync::GetUnreadNotificationsAsync-01")]
    // public async Task GetUnreadNotificationsAsync_GetUnreadNotificationsAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid userDid, page, pageSize provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var userDid = "did:example:test-user";
        
    //     // Create mixed read/unread notifications
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var notification = new NotificationEntity
    //         {
    //             Id = Guid.NewGuid(),
    //             RecipientDid = userDid,
    //             Title = $"Unread Notification {i}",
    //             Body = $"Body {i}",
    //             Priority = NotificationPriority.Normal,
    //             Channel = NotificationChannel.InApp,
    //             Status = NotificationStatus.Pending, // Unread
    //             CreatedAt = DateTime.UtcNow
    //         };
    //         context.Notifications.Add(notification);
    //     }
    //     // Add one read notification
    //     var readNotification = new NotificationEntity
    //     {
    //         Id = Guid.NewGuid(),
    //         RecipientDid = userDid,
    //         Title = "Read Notification",
    //         Body = "Body",
    //         Priority = NotificationPriority.Normal,
    //         Channel = NotificationChannel.InApp,
    //         Status = NotificationStatus.Read, // Read
    //         ReadAt = DateTime.UtcNow,
    //         CreatedAt = DateTime.UtcNow
    //     };
    //     context.Notifications.Add(readNotification);
    //     await context.SaveChangesAsync();

    //     // Act
    //     var result = await service.GetUnreadNotificationsAsync(userDid, 1, 10);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.Equal(3, result.Data.Items.Count);
    // }

    // [Fact(DisplayName = "GetUnreadNotificationsAsync::GetUnreadNotificationsAsync-02")]
    // public async Task GetUnreadNotificationsAsync_GetUnreadNotificationsAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: userDid = null/empty OR page <= 0 OR pageSize <= 0
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act & Assert
    //     var resultEmptyDid = await service.GetUnreadNotificationsAsync("", 1, 10);
    //     Assert.NotNull(resultEmptyDid);

    //     var resultNegativePage = await service.GetUnreadNotificationsAsync("did:example:user", -1, 10);
    //     Assert.NotNull(resultNegativePage);
    // }

    // #endregion

    // #region MarkAsReadAsync Tests

    // [Fact(DisplayName = "MarkAsReadAsync::MarkAsReadAsync-01")]
    // public async Task MarkAsReadAsync_MarkAsReadAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid userDid, request provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var userDid = "did:example:test-user";
    //     var notificationId = Guid.NewGuid();
        
    //     var notification = new NotificationEntity
    //     {
    //         Id = notificationId,
    //         RecipientDid = userDid,
    //         Title = "Test Notification",
    //         Body = "Body",
    //         Priority = NotificationPriority.Normal,
    //         Channel = NotificationChannel.InApp,
    //         Status = NotificationStatus.Pending,
    //         CreatedAt = DateTime.UtcNow
    //     };
    //     context.Notifications.Add(notification);
    //     await context.SaveChangesAsync();

    //     var request = new MarkReadRequest { NotificationIds = new List<Guid> { notificationId } };

    //     // Act
    //     var result = await service.MarkAsReadAsync(userDid, request);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    //     Assert.Equal(1, result.Data);
    // }

    // [Fact(DisplayName = "MarkAsReadAsync::MarkAsReadAsync-02")]
    // public async Task MarkAsReadAsync_MarkAsReadAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: userDid = null/empty OR request with missing required fields
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act & Assert
    //     var resultEmptyDid = await service.MarkAsReadAsync("", new MarkReadRequest { NotificationIds = new List<Guid> { Guid.NewGuid() } });
    //     Assert.NotNull(resultEmptyDid);
    //     Assert.False(resultEmptyDid.Success);

    //     var resultEmptyList = await service.MarkAsReadAsync("did:example:user", new MarkReadRequest { NotificationIds = new List<Guid>() });
    //     Assert.NotNull(resultEmptyList);
    //     Assert.False(resultEmptyList.Success);
    // }

    // #endregion

    // #region MarkAllAsReadAsync Tests

    // [Fact(DisplayName = "MarkAllAsReadAsync::MarkAllAsReadAsync-01")]
    // public async Task MarkAllAsReadAsync_MarkAllAsReadAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid userDid provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var userDid = "did:example:test-user";
        
    //     // Create multiple unread notifications
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var notification = new NotificationEntity
    //         {
    //             Id = Guid.NewGuid(),
    //             RecipientDid = userDid,
    //             Title = $"Notification {i}",
    //             Body = $"Body {i}",
    //             Priority = NotificationPriority.Normal,
    //             Channel = NotificationChannel.InApp,
    //             Status = NotificationStatus.Pending,
    //             CreatedAt = DateTime.UtcNow
    //         };
    //         context.Notifications.Add(notification);
    //     }
    //     await context.SaveChangesAsync();

    //     // Act
    //     var result = await service.MarkAllAsReadAsync(userDid);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    //     Assert.Equal(3, result.Data);
    // }

    // [Fact(DisplayName = "MarkAllAsReadAsync::MarkAllAsReadAsync-02")]
    // public async Task MarkAllAsReadAsync_MarkAllAsReadAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: userDid = null/empty
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act
    //     var result = await service.MarkAllAsReadAsync("");

    //     // Assert
    //     // Expected Return: Returns validation error (400 or 422) or equivalent domain error
    //     Assert.NotNull(result);
    //     Assert.False(result.Success);
    // }

    // #endregion

    // #region DeleteNotificationAsync Tests

    // [Fact(DisplayName = "DeleteNotificationAsync::DeleteNotificationAsync-01")]
    // public async Task DeleteNotificationAsync_DeleteNotificationAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid notificationId provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var notificationId = Guid.NewGuid();
        
    //     var notification = new NotificationEntity
    //     {
    //         Id = notificationId,
    //         RecipientDid = "did:example:test-user",
    //         Title = "Test Notification",
    //         Body = "Body",
    //         Priority = NotificationPriority.Normal,
    //         Channel = NotificationChannel.InApp,
    //         Status = NotificationStatus.Pending,
    //         CreatedAt = DateTime.UtcNow
    //     };
    //     context.Notifications.Add(notification);
    //     await context.SaveChangesAsync();

    //     // Act
    //     var result = await service.DeleteNotificationAsync(notificationId);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    //     Assert.True(result.Data);
    // }

    // [Fact(DisplayName = "DeleteNotificationAsync::DeleteNotificationAsync-02")]
    // public async Task DeleteNotificationAsync_DeleteNotificationAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: notificationId = Guid.Empty
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act
    //     var result = await service.DeleteNotificationAsync(Guid.Empty);

    //     // Assert
    //     // Expected Return: Returns validation error (400 or 422) or equivalent domain error
    //     Assert.NotNull(result);
    //     Assert.False(result.Success);
    // }

    // #endregion

    // #region GetUnreadCountAsync Tests

    // [Fact(DisplayName = "GetUnreadCountAsync::GetUnreadCountAsync-01")]
    // public async Task GetUnreadCountAsync_GetUnreadCountAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid userDid provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var userDid = "did:example:test-user";
        
    //     // Create unread notifications
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var notification = new NotificationEntity
    //         {
    //             Id = Guid.NewGuid(),
    //             RecipientDid = userDid,
    //             Title = $"Notification {i}",
    //             Body = $"Body {i}",
    //             Priority = NotificationPriority.Normal,
    //             Channel = NotificationChannel.InApp,
    //             Status = NotificationStatus.Pending,
    //             CreatedAt = DateTime.UtcNow
    //         };
    //         context.Notifications.Add(notification);
    //     }
    //     // Add one read notification
    //     var readNotification = new NotificationEntity
    //     {
    //         Id = Guid.NewGuid(),
    //         RecipientDid = userDid,
    //         Title = "Read Notification",
    //         Body = "Body",
    //         Priority = NotificationPriority.Normal,
    //         Channel = NotificationChannel.InApp,
    //         Status = NotificationStatus.Read,
    //         ReadAt = DateTime.UtcNow,
    //         CreatedAt = DateTime.UtcNow
    //     };
    //     context.Notifications.Add(readNotification);
    //     await context.SaveChangesAsync();

    //     // Act
    //     var result = await service.GetUnreadCountAsync(userDid);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.Equal(3, result);
    // }

    // [Fact(DisplayName = "GetUnreadCountAsync::GetUnreadCountAsync-02")]
    // public async Task GetUnreadCountAsync_GetUnreadCountAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: userDid = null/empty
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act
    //     var result = await service.GetUnreadCountAsync("");

    //     // Assert
    //     // Expected Return: Returns 0 for empty userDid
    //     Assert.Equal(0, result);
    // }

    // #endregion

    // #region RegisterDeviceAsync Tests

    // [Fact(DisplayName = "RegisterDeviceAsync::RegisterDeviceAsync-01")]
    // public async Task RegisterDeviceAsync_RegisterDeviceAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid request provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var request = new RegisterDeviceRequest
    //     {
    //         UserDid = "did:example:test-user",
    //         UserId = Guid.NewGuid(),
    //         FcmToken = "fcm-token-12345",
    //         DeviceName = "Samsung Galaxy S21"
    //     };

    //     // Act
    //     var result = await service.RegisterDeviceAsync(request);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    // }

    // [Fact(DisplayName = "RegisterDeviceAsync::RegisterDeviceAsync-02")]
    // public async Task RegisterDeviceAsync_RegisterDeviceAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: request with missing required fields
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var request = new RegisterDeviceRequest
    //     {
    //         // Missing UserDid and FcmToken
    //     };

    //     // Act
    //     var result = await service.RegisterDeviceAsync(request);

    //     // Assert
    //     // Expected Return: Returns validation error (400 or 422) or equivalent domain error
    //     Assert.NotNull(result);
    //     Assert.False(result.Success);
    // }

    // #endregion

    // #region DeactivateDeviceAsync Tests

    // [Fact(DisplayName = "DeactivateDeviceAsync::DeactivateDeviceAsync-01")]
    // public async Task DeactivateDeviceAsync_DeactivateDeviceAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid userDid, deviceTokenId provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var userDid = "did:example:test-user";
    //     var deviceTokenId = Guid.NewGuid();
        
    //     var deviceToken = new DeviceTokenEntity
    //     {
    //         Id = deviceTokenId,
    //         UserDid = userDid,
    //         FcmToken = "fcm-token-12345",
    //         DeviceName = "Samsung Galaxy S21",
    //         IsActive = true,
    //         CreatedAt = DateTime.UtcNow
    //     };
    //     context.DeviceTokens.Add(deviceToken);
    //     await context.SaveChangesAsync();

    //     // Act
    //     var result = await service.DeactivateDeviceAsync(userDid, deviceTokenId);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    // }

    // [Fact(DisplayName = "DeactivateDeviceAsync::DeactivateDeviceAsync-02")]
    // public async Task DeactivateDeviceAsync_DeactivateDeviceAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: userDid = null/empty OR deviceTokenId = Guid.Empty
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act
    //     var resultEmptyDid = await service.DeactivateDeviceAsync("", Guid.NewGuid());
    //     Assert.NotNull(resultEmptyDid);
    //     Assert.False(resultEmptyDid.Success);

    //     var resultEmptyGuid = await service.DeactivateDeviceAsync("did:example:user", Guid.Empty);
    //     Assert.NotNull(resultEmptyGuid);
    //     Assert.False(resultEmptyGuid.Success);
    // }

    // #endregion

    // #region GetUserDevicesAsync Tests

    // [Fact(DisplayName = "GetUserDevicesAsync::GetUserDevicesAsync-01")]
    // public async Task GetUserDevicesAsync_GetUserDevicesAsync_01_1()
    // {
    //     // Arrange
    //     // Condition: HappyPath
    //     // Input: Valid userDid provided
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var userDid = "did:example:test-user";
        
    //     // Create multiple device tokens
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var deviceToken = new DeviceTokenEntity
    //         {
    //             Id = Guid.NewGuid(),
    //             UserDid = userDid,
    //             FcmToken = $"fcm-token-{i}",
    //             DeviceName = $"Device {i}",
    //             IsActive = true,
    //             CreatedAt = DateTime.UtcNow
    //         };
    //         context.DeviceTokens.Add(deviceToken);
    //     }
    //     await context.SaveChangesAsync();

    //     // Act
    //     var result = await service.GetUserDevicesAsync(userDid);

    //     // Assert
    //     // Expected Return: Returns success payload matching declared return type
    //     Assert.NotNull(result);
    //     Assert.Equal(3, result.Data.Count(d => d.IsActive));
    // }

    // [Fact(DisplayName = "GetUserDevicesAsync::GetUserDevicesAsync-02")]
    // public async Task GetUserDevicesAsync_GetUserDevicesAsync_02_2()
    // {
    //     // Arrange
    //     // Condition: InvalidInput
    //     // Input: userDid = null/empty
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);

    //     // Act
    //     var result = await service.GetUserDevicesAsync("");

    //     // Assert
    //     // Expected Return: Returns validation error (400 or 422) or equivalent domain error
    //     Assert.NotNull(result);
    //     Assert.False(result.Success);
    // }

    // [Fact(DisplayName = "GetUserDevicesAsync::GetUserDevicesAsync-03")]
    // public async Task GetUserDevicesAsync_GetUserDevicesAsync_03_3()
    // {
    //     // Arrange
    //     // Condition: NotFoundOrNoData
    //     // Input: Requested resource not found
    //     using var context = new NotificationDbContext(_dbContextOptions);
    //     var service = CreateService(context);
        
    //     var nonExistentUserDid = "did:example:non-existent-user";

    //     // Act
    //     var result = await service.GetUserDevicesAsync(nonExistentUserDid);

    //     // Assert
    //     // Expected Return: Returns null, empty, false, or not-found response according to contract
    //     Assert.NotNull(result);
    //     Assert.True(result.Success);
    //     Assert.Empty(result.Data);
    // }

    // #endregion
}


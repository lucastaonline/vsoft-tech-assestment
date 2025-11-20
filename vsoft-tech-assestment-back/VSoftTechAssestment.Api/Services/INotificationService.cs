using VSoftTechAssestment.Api.Models.DTOs.Notification;

namespace VSoftTechAssestment.Api.Services;

public interface INotificationService
{
    System.Threading.Tasks.Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(string userId);
    System.Threading.Tasks.Task<int> GetUnreadCountAsync(string userId);
    System.Threading.Tasks.Task<NotificationResponse?> MarkAsReadAsync(Guid notificationId, string userId);
    System.Threading.Tasks.Task MarkAllAsReadAsync(string userId);
    System.Threading.Tasks.Task<NotificationResponse> CreateNotificationAsync(string userId, string message);
}


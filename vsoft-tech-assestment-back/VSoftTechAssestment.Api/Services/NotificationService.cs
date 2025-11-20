using Microsoft.EntityFrameworkCore;
using VSoftTechAssestment.Api.Data;
using VSoftTechAssestment.Api.Models.DTOs.Notification;
using VSoftTechAssestment.Api.Models.Entities;

namespace VSoftTechAssestment.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            })
            .ToListAsync();

        return notifications;
    }

    public async System.Threading.Tasks.Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async System.Threading.Tasks.Task<NotificationResponse?> MarkAsReadAsync(Guid notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return null;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new NotificationResponse
        {
            Id = notification.Id,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }

    public async System.Threading.Tasks.Task MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task<NotificationResponse> CreateNotificationAsync(string userId, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return new NotificationResponse
        {
            Id = notification.Id,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}


namespace VSoftTechAssestment.Api.Models.DTOs.Notification;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? TaskId { get; set; }
}


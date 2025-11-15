namespace VSoftTechAssestment.Api.Services;

public interface IRabbitMQService
{
    void PublishTaskNotification(string userId, string taskId, string taskTitle);
}


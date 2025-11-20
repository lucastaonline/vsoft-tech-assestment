using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VSoftTechAssestment.Api.Data;
using VSoftTechAssestment.Api.Hubs;
using VSoftTechAssestment.Api.Models.DTOs.Notification;

namespace VSoftTechAssestment.Api.Services;

public class RabbitMQNotificationConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQNotificationConsumer> _logger;
    private const string ExchangeName = "task_notifications";
    private const string QueueName = "task_notifications_queue";
    private const string RoutingKey = "task.assigned";

    public RabbitMQNotificationConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQNotificationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var rabbitMqHost = configuration["RabbitMQ:HostName"] ?? "localhost";
        var rabbitMqPort = configuration.GetValue<int>("RabbitMQ:Port", 5672);
        var rabbitMqUserName = configuration["RabbitMQ:UserName"] ?? "guest";
        var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = rabbitMqHost,
            Port = rabbitMqPort,
            UserName = rabbitMqUserName,
            Password = rabbitMqPassword
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange (mesmo do publisher)
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            // Set QoS to process one message at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ consumer connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ consumer connection");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var notificationData = JsonSerializer.Deserialize<NotificationMessage>(message);
                
                if (notificationData != null)
                {
                    await ProcessNotificationAsync(notificationData);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Notification processed successfully for user {UserId}", notificationData.UserId);
                }
                else
                {
                    _logger.LogWarning("Received invalid notification message: {Message}", message);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification message: {Message}", message);
                // Reject and don't requeue to avoid infinite loop
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started, waiting for messages...");

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessNotificationAsync(NotificationMessage notificationData)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

        try
        {
            // Salvar notificação no banco
            var notification = await notificationService.CreateNotificationAsync(
                notificationData.UserId,
                notificationData.Message);

            // Enviar notificação em tempo real via SignalR
            var notificationResponse = new NotificationResponse
            {
                Id = notification.Id,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                TaskId = !string.IsNullOrEmpty(notificationData.TaskId) && Guid.TryParse(notificationData.TaskId, out var taskId) 
                    ? taskId 
                    : null
            };

            await hubContext.Clients.Group($"user_{notificationData.UserId}")
                .SendAsync("ReceiveNotification", notificationResponse);

            _logger.LogInformation("Notification sent via SignalR to user {UserId}", notificationData.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification for user {UserId}", notificationData.UserId);
            throw;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }

    private class NotificationMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


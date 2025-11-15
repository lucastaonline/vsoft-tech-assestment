using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace VSoftTechAssestment.Api.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private const string ExchangeName = "task_notifications";
    private const string QueueName = "task_notifications_queue";
    private const string RoutingKey = "task.assigned";

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
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

            // Declare exchange
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

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public void PublishTaskNotification(string userId, string taskId, string taskTitle)
    {
        try
        {
            var message = new
            {
                UserId = userId,
                TaskId = taskId,
                TaskTitle = taskTitle,
                Timestamp = DateTime.UtcNow,
                Message = $"Nova tarefa atribuída: {taskTitle}"
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Task notification published for user {UserId}, task {TaskId}", userId, taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish task notification for user {UserId}, task {TaskId}", userId, taskId);
            // Não lançar exceção para não quebrar o fluxo de criação de tarefa
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}


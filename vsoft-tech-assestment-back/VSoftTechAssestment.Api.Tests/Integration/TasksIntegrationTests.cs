using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using VSoftTechAssestment.Api.Data;
using VSoftTechAssestment.Api.Models.DTOs.Auth;
using VSoftTechAssestment.Api.Models.DTOs.Task;
using VSoftTechAssestment.Api.Models.Entities;
using TaskEntity = VSoftTechAssestment.Api.Models.Entities.Task;

namespace VSoftTechAssestment.Api.Tests.Integration;

public class TasksIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly string _databaseName;
    private string? _authToken;
    private string? _userId;

    public TasksIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Use a shared database name so both test and API use the same in-memory database
        _databaseName = "TestDb_" + Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add JWT configuration for tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Secret", "YourSuperSecretKeyThatMustBeAtLeast32CharactersLong!" },
                    { "Jwt:Issuer", "VSoftTechAssestment" },
                    { "Jwt:Audience", "VSoftTechAssestment" },
                    { "Jwt:ExpirationMinutes", "60" }
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database with shared name
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                // Remove RabbitMQ service
                var rabbitMQDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(VSoftTechAssestment.Api.Services.IRabbitMQService));
                if (rabbitMQDescriptor != null)
                {
                    services.Remove(rabbitMQDescriptor);
                }

                // Remove RabbitMQ background consumer
                var hostedServiceDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(VSoftTechAssestment.Api.Services.RabbitMQNotificationConsumer));
                if (hostedServiceDescriptor != null)
                {
                    services.Remove(hostedServiceDescriptor);
                }

                services.AddSingleton<VSoftTechAssestment.Api.Services.IRabbitMQService>(
                    new MockRabbitMQService());
            });
        });

        _client = _factory.CreateClient();

        // Create scope and get services that use the same in-memory database
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_WithAuthentication_ShouldReturnCreated()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateTaskRequest
        {
            Title = "Integration Test Task",
            Description = "This is a test task",
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = Models.Entities.TaskStatus.Pending
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskResponse = await response.Content.ReadFromJsonAsync<TaskResponse>();
        taskResponse.Should().NotBeNull();
        taskResponse!.Title.Should().Be(request.Title);
        taskResponse.UserId.Should().Be(_userId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_WithAuthentication_ShouldReturnUserTasks()
    {
        // Arrange
        await AuthenticateAsync();

        // Create a task for the authenticated user
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "User Task",
            Description = "Description",
            DueDate = DateTime.UtcNow,
            Status = Models.Entities.TaskStatus.Pending,
            UserId = _userId!,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        tasks!.Should().HaveCount(1);
        tasks.First().Title.Should().Be("User Task");
    }

    private async System.Threading.Tasks.Task AuthenticateAsync()
    {
        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // Register user via API endpoint (ensures it's in the same context)
        var registerRequest = new RegisterRequest
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed with status {registerResponse.StatusCode}: {errorContent}");
        }

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        
        if (registerResult == null || string.IsNullOrEmpty(registerResult.UserId))
        {
            throw new Exception("Failed to register test user");
        }

        _userId = registerResult.UserId;

        // Small delay to ensure registration is fully persisted
        await System.Threading.Tasks.Task.Delay(200);

        // Login to get token
        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = "testuser",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with status {loginResponse.StatusCode}: {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        
        if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
        {
            throw new Exception("Failed to get authentication token");
        }

        _authToken = loginResult.Token;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _client.Dispose();
    }
}

// Mock RabbitMQ Service for integration tests
public class MockRabbitMQService : VSoftTechAssestment.Api.Services.IRabbitMQService
{
    public void PublishTaskNotification(string userId, string taskId, string taskTitle)
    {
        // Mock implementation - do nothing
    }
}


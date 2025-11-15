using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    private string? _authToken;
    private string? _userId;

    public TasksIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });

                // Remove RabbitMQ service
                var rabbitMQDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(VSoftTechAssestment.Api.Services.IRabbitMQService));
                if (rabbitMQDescriptor != null)
                {
                    services.Remove(rabbitMQDescriptor);
                }

                services.AddSingleton<VSoftTechAssestment.Api.Services.IRabbitMQService>(
                    new MockRabbitMQService());
            });
        });

        _client = _factory.CreateClient();

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
        // Create a test user
        var user = new IdentityUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, "Password123!");
        if (result.Succeeded)
        {
            _userId = user.Id;

            // Login to get token
            var loginRequest = new LoginRequest
            {
                UserNameOrEmail = "testuser",
                Password = "Password123!"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            _authToken = loginResult?.Token;
        }
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


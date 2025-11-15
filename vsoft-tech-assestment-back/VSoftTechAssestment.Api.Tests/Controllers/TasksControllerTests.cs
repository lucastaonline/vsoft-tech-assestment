using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using VSoftTechAssestment.Api.Controllers;
using VSoftTechAssestment.Api.Data;
using VSoftTechAssestment.Api.Models.DTOs.Task;
using VSoftTechAssestment.Api.Models.Entities;
using VSoftTechAssestment.Api.Services;
using TaskEntity = VSoftTechAssestment.Api.Models.Entities.Task;
using System.Threading.Tasks;

namespace VSoftTechAssestment.Api.Tests.Controllers;

public class TasksControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<IRabbitMQService> _rabbitMQServiceMock;
    private readonly TasksController _controller;
    private readonly string _testUserId = "test-user-id";

    public TasksControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _rabbitMQServiceMock = new Mock<IRabbitMQService>();

        _controller = new TasksController(_context, _userManagerMock.Object, _rabbitMQServiceMock.Object);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = Models.Entities.TaskStatus.Pending
        };

        var user = new IdentityUser { Id = _testUserId, UserName = "testuser" };
        _userManagerMock.Setup(x => x.FindByIdAsync(_testUserId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<TaskResponse>().Subject;
        response.Title.Should().Be(request.Title);
        response.Description.Should().Be(request.Description);
        response.UserId.Should().Be(_testUserId);
        _rabbitMQServiceMock.Verify(x => x.PublishTaskNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var otherUserId = "other-user-id";
        
        var userTask = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "User Task",
            Description = "Description",
            DueDate = DateTime.UtcNow,
            Status = Models.Entities.TaskStatus.Pending,
            UserId = _testUserId
        };

        var otherTask = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Other Task",
            Description = "Description",
            DueDate = DateTime.UtcNow,
            Status = Models.Entities.TaskStatus.Pending,
            UserId = otherUserId
        };

        _context.Tasks.AddRange(userTask, otherTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTasks();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskResponse>>().Subject;
        tasks.Should().HaveCount(1);
        tasks.First().Title.Should().Be("User Task");
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_WithValidData_ShouldReturnNoContent()
    {
        // Arrange
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            DueDate = DateTime.UtcNow,
            Status = Models.Entities.TaskStatus.Pending,
            UserId = _testUserId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            Status = Models.Entities.TaskStatus.InProgress
        };

        // Act
        var result = await _controller.UpdateTask(task.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedTask = await _context.Tasks.FindAsync(task.Id);
        updatedTask.Should().NotBeNull();
        updatedTask!.Title.Should().Be("Updated Title");
        updatedTask.Status.Should().Be(Models.Entities.TaskStatus.InProgress);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteTask_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = "Task to Delete",
            Description = "Description",
            DueDate = DateTime.UtcNow,
            Status = Models.Entities.TaskStatus.Pending,
            UserId = _testUserId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteTask(task.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var deletedTask = await _context.Tasks.FindAsync(task.Id);
        deletedTask.Should().BeNull();
    }
}


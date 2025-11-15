using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using VSoftTechAssestment.Api.Controllers;
using VSoftTechAssestment.Api.Models.DTOs.Task;
using VSoftTechAssestment.Api.Services;

namespace VSoftTechAssestment.Api.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly TasksController _controller;
    private readonly string _testUserId = "test-user-id";

    public TasksControllerTests()
    {
        _taskServiceMock = new Mock<ITaskService>();
        _controller = new TasksController(_taskServiceMock.Object);

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
    public async Task CreateTask_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = Models.Entities.TaskStatus.Pending
        };

        var expectedResponse = new TaskResponse
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Status = request.Status,
            UserId = _testUserId,
            UserName = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        _taskServiceMock.Setup(x => x.CreateTaskAsync(request, _testUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateTask(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<TaskResponse>().Subject;
        response.Title.Should().Be(request.Title);
        response.Description.Should().Be(request.Description);
        response.UserId.Should().Be(_testUserId);
        _taskServiceMock.Verify(x => x.CreateTaskAsync(request, _testUserId), Times.Once);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var expectedTasks = new List<TaskResponse>
        {
            new TaskResponse
            {
                Id = Guid.NewGuid(),
                Title = "User Task",
                Description = "Description",
                DueDate = DateTime.UtcNow,
                Status = Models.Entities.TaskStatus.Pending,
                UserId = _testUserId,
                CreatedAt = DateTime.UtcNow
            }
        };

        _taskServiceMock.Setup(x => x.GetUserTasksAsync(_testUserId))
            .ReturnsAsync(expectedTasks);

        // Act
        var result = await _controller.GetTasks();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskResponse>>().Subject;
        tasks.Should().HaveCount(1);
        tasks.First().Title.Should().Be("User Task");
        _taskServiceMock.Verify(x => x.GetUserTasksAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task UpdateTask_WithValidData_ShouldReturnNoContent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            Status = Models.Entities.TaskStatus.InProgress
        };

        _taskServiceMock.Setup(x => x.UpdateTaskAsync(taskId, request, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _taskServiceMock.Verify(x => x.UpdateTaskAsync(taskId, request, _testUserId), Times.Once);
    }

    [Fact]
    public async Task UpdateTask_WithNonExistentTask_ShouldReturnNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            Status = Models.Entities.TaskStatus.InProgress
        };

        _taskServiceMock.Setup(x => x.UpdateTaskAsync(taskId, request, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _taskServiceMock.Verify(x => x.UpdateTaskAsync(taskId, request, _testUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteTask_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskServiceMock.Setup(x => x.DeleteTaskAsync(taskId, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteTask(taskId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _taskServiceMock.Verify(x => x.DeleteTaskAsync(taskId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteTask_WithNonExistentTask_ShouldReturnNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskServiceMock.Setup(x => x.DeleteTaskAsync(taskId, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTask(taskId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _taskServiceMock.Verify(x => x.DeleteTaskAsync(taskId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task GetTask_WithValidId_ShouldReturnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var expectedTask = new TaskResponse
        {
            Id = taskId,
            Title = "Test Task",
            Description = "Description",
            DueDate = DateTime.UtcNow,
            Status = Models.Entities.TaskStatus.Pending,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId, _testUserId))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var task = okResult.Value.Should().BeOfType<TaskResponse>().Subject;
        task.Id.Should().Be(taskId);
        task.Title.Should().Be("Test Task");
        _taskServiceMock.Verify(x => x.GetTaskByIdAsync(taskId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task GetTask_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId, _testUserId))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _taskServiceMock.Verify(x => x.GetTaskByIdAsync(taskId, _testUserId), Times.Once);
    }
}


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
    public async Task GetTasks_WithoutPagination_ShouldReturnAllTasks()
    {
        // Arrange
        var expectedTasks = new List<TaskResponse>
        {
            new TaskResponse
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Description",
                DueDate = DateTime.UtcNow,
                Status = Models.Entities.TaskStatus.Pending,
                UserId = _testUserId,
                CreatedAt = DateTime.UtcNow
            }
        };

        _taskServiceMock.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(expectedTasks);

        // Act
        var result = await _controller.GetTasks();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var tasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskResponse>>().Subject;
        tasks.Should().HaveCount(1);
        _taskServiceMock.Verify(x => x.GetAllTasksAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTasks_WithPagination_ShouldReturnPaginatedResult()
    {
        // Arrange
        var cursor = Guid.NewGuid();
        var paginatedResponse = new PaginatedTasksResponse
        {
            Tasks = new List<TaskResponse>
            {
                new TaskResponse
                {
                    Id = Guid.NewGuid(),
                    Title = "Task 2",
                    Status = Models.Entities.TaskStatus.Pending,
                    UserId = _testUserId
                }
            },
            NextCursor = Guid.NewGuid(),
            HasMore = true
        };

        _taskServiceMock.Setup(x => x.GetAllTasksPaginatedAsync(cursor, 10))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetTasks(cursor, 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedTasksResponse>().Subject;
        response.Tasks.Should().HaveCount(1);
        response.HasMore.Should().BeTrue();
        _taskServiceMock.Verify(x => x.GetAllTasksPaginatedAsync(cursor, 10), Times.Once);
    }

    [Fact]
    public async Task UpdateTask_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            Status = Models.Entities.TaskStatus.InProgress,
            UserId = _testUserId
        };

        var existingTask = new TaskResponse
        {
            Id = taskId,
            UserId = _testUserId
        };

        var updatedTask = new TaskResponse
        {
            Id = taskId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            UserId = request.UserId
        };

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _taskServiceMock.Setup(x => x.UpdateTaskAsync(taskId, request, _testUserId, request.UserId))
            .ReturnsAsync(updatedTask);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TaskResponse>().Subject;
        response.Title.Should().Be(request.Title);
        _taskServiceMock.Verify(x => x.GetTaskByIdAsync(taskId), Times.Once);
        _taskServiceMock.Verify(x => x.UpdateTaskAsync(taskId, request, _testUserId, request.UserId), Times.Once);
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
            Status = Models.Entities.TaskStatus.InProgress,
            UserId = _testUserId
        };

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _taskServiceMock.Verify(x => x.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskRequest>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTask_WhenUserIsNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            Status = Models.Entities.TaskStatus.InProgress,
            UserId = _testUserId
        };

        var existingTask = new TaskResponse
        {
            Id = taskId,
            UserId = "another-user"
        };

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _taskServiceMock.Verify(x => x.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskRequest>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTask_WithoutUserId_ShouldReturnBadRequest()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            Status = Models.Entities.TaskStatus.InProgress,
            UserId = string.Empty
        };

        // Act
        var result = await _controller.UpdateTask(taskId, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _taskServiceMock.Verify(x => x.GetTaskByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTask_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var existingTask = new TaskResponse
        {
            Id = taskId,
            UserId = _testUserId
        };

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync(existingTask);

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

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.DeleteTask(taskId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _taskServiceMock.Verify(x => x.DeleteTaskAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
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

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var task = okResult.Value.Should().BeOfType<TaskResponse>().Subject;
        task.Id.Should().Be(taskId);
        task.Title.Should().Be("Test Task");
        _taskServiceMock.Verify(x => x.GetTaskByIdAsync(taskId), Times.Once);
    }

    [Fact]
    public async Task GetTask_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _taskServiceMock.Setup(x => x.GetTaskByIdAsync(taskId))
            .ReturnsAsync((TaskResponse?)null);

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _taskServiceMock.Verify(x => x.GetTaskByIdAsync(taskId), Times.Once);
    }
}


using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VSoftTechAssestment.Api.Data;
using VSoftTechAssestment.Api.Models.DTOs.Task;
using VSoftTechAssestment.Api.Models.Entities;
using TaskEntity = VSoftTechAssestment.Api.Models.Entities.Task;

namespace VSoftTechAssestment.Api.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IRabbitMQService _rabbitMQService;

    public TaskService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IRabbitMQService rabbitMQService)
    {
        _context = context;
        _userManager = userManager;
        _rabbitMQService = rabbitMQService;
    }

    public async Task<IEnumerable<TaskResponse>> GetUserTasksAsync(string userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Status = t.Status,
                UserId = t.UserId,
                UserName = t.User != null ? t.User.UserName : null,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid id, string userId)
    {
        var task = await _context.Tasks
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return null;
        }

        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Status = task.Status,
            UserId = task.UserId,
            UserName = task.User?.UserName,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId)
    {
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Status = request.Status,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Send notification via RabbitMQ
        _rabbitMQService.PublishTaskNotification(userId, task.Id.ToString(), task.Title);

        var user = await _userManager.FindByIdAsync(userId);
        
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Status = task.Status,
            UserId = task.UserId,
            UserName = user?.UserName,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    public async Task<bool> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return false;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> DeleteTaskAsync(Guid id, string userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        
        return true;
    }
}


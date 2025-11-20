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

    public async Task<IEnumerable<TaskResponse>> GetAllTasksAsync()
    {
        return await _context.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.User)
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

    public async Task<PaginatedTasksResponse> GetAllTasksPaginatedAsync(Guid? cursor, int pageSize)
    {
        var query = _context.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        // Se há cursor, buscar tasks criadas antes da data do cursor
        if (cursor.HasValue)
        {
            var cursorTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == cursor.Value);
            
            if (cursorTask != null)
            {
                query = query.Where(t => t.CreatedAt < cursorTask.CreatedAt || 
                    (t.CreatedAt == cursorTask.CreatedAt && t.Id != cursor.Value));
            }
        }

        // Buscar uma página a mais para verificar se há mais resultados
        var tasks = await query
            .Take(pageSize + 1)
            .Include(t => t.User)
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

        var hasMore = tasks.Count > pageSize;
        var resultTasks = hasMore ? tasks.Take(pageSize).ToList() : tasks;

        var nextCursor = resultTasks.Any() ? resultTasks.Last().Id : (Guid?)null;

        return new PaginatedTasksResponse
        {
            Tasks = resultTasks,
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid id)
    {
        var task = await _context.Tasks
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

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

    public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string authenticatedUserId, string newUserId)
    {
        // Verificar se a tarefa pertence ao usuário autenticado
        var task = await _context.Tasks
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == authenticatedUserId);

        if (task == null)
        {
            return null;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.Status = request.Status;
        
        // Atualizar o responsável (userId é obrigatório no UpdateTaskRequest)
        if (newUserId != authenticatedUserId)
        {
            task.UserId = newUserId;
            // Enviar notificação para o novo responsável
            _rabbitMQService.PublishTaskNotification(newUserId, task.Id.ToString(), task.Title);
        }
        
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        // Recarregar a task com o usuário atualizado se necessário
        if (newUserId != authenticatedUserId)
        {
            await _context.Entry(task).Reference(t => t.User).LoadAsync();
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

    public async Task<bool> DeleteTaskAsync(Guid id, string authenticatedUserId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            return false;
        }

        // Verificar se o usuário autenticado é o dono da tarefa
        if (task.UserId != authenticatedUserId)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        
        return true;
    }
}


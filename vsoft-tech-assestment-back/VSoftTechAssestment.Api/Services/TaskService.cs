using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VSoftTechAssestment.Api.Data;
using VSoftTechAssestment.Api.Models.DTOs.Task;
using VSoftTechAssestment.Api.Models.Entities;
using TaskEntity = VSoftTechAssestment.Api.Models.Entities.Task;
using TaskStatusEnum = VSoftTechAssestment.Api.Models.Entities.TaskStatus;

namespace VSoftTechAssestment.Api.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IRabbitMQService _rabbitMQService;
    private static readonly string[] TitlePrefixes = { "Melhoria", "Bugfix", "Pesquisa", "Spike", "Entrega" };
    private static readonly string[] TitleSubjects = {
        "Dashboard",
        "API de Tasks",
        "Workflow",
        "Integração",
        "Notificações",
        "Auth",
        "Relatórios",
        "Performance"
    };
    private static readonly string[] ScenarioTemplates = {
        "Precisamos validar o comportamento do módulo em cenários reais para garantir que as mudanças não afetem os usuários finais.",
        "O time recebeu feedback do produto solicitando ajustes rápidos antes do próximo release.",
        "Há inconsistências entre o que está no design system e o que entregamos na interface atual.",
        "Estamos preparando uma demo pública e precisamos de exemplos convincentes para apresentar."
    };
    private static readonly string[] Objectives = {
        "Documentar os aprendizados e registrar métricas para futuras iterações.",
        "Garantir que a experiência seja consistente em mobile e desktop.",
        "Reduzir o tempo de resposta médio em pelo menos 20%.",
        "Trabalhar próximos ao time de produto para validar hipóteses."
    };
    private static readonly string[] ChecklistItems = {
        "Criar cenários de teste com dados sintéticos",
        "Validar critérios de aceitação com o squad",
        "Atualizar documentação no Notion",
        "Sincronizar com o capítulo de QA",
        "Revisar métricas no dashboard",
        "Publicar release notes em Markdown",
        "Ajustar notificações em tempo real"
    };
    private static readonly string[] Notes = {
        "Alinhar com o time de SRE antes de subir para produção.",
        "Registrar aprendizados no retro da sprint.",
        "Priorizar a clareza do texto para facilitar revisões futuras.",
        "Evitar dependências externas que possam quebrar o pipeline."
    };

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

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string assignedUserId, string createdByUserId)
    {
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Status = request.Status,
            UserId = assignedUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        if (!string.Equals(assignedUserId, createdByUserId, StringComparison.OrdinalIgnoreCase))
        {
            _rabbitMQService.PublishTaskNotification(assignedUserId, task.Id.ToString(), task.Title);
        }

        var user = await _userManager.FindByIdAsync(assignedUserId);
        
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

    public async Task<IReadOnlyList<TaskResponse>> CreateMockTasksAsync(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A quantidade precisa ser maior que zero.");
        }

        var users = await _userManager.Users.ToListAsync();
        if (users.Count == 0)
        {
            throw new InvalidOperationException("Não há usuários cadastrados para receber tarefas.");
        }

        var lookupUserNames = users.ToDictionary(u => u.Id, u => u.UserName);
        var random = Random.Shared;
        var statuses = Enum.GetValues<TaskStatusEnum>();
        var tasks = new List<TaskEntity>(amount);

        for (var i = 0; i < amount; i++)
        {
            var user = users[random.Next(users.Count)];
            var title = GenerateTitle(random);
            var description = GenerateMarkdownDescription(title, random);

            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                DueDate = DateTime.UtcNow.AddDays(random.Next(2, 30)),
                Status = statuses[random.Next(statuses.Length)],
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-random.Next(0, 240))
            };

            tasks.Add(task);
        }

        await _context.Tasks.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();

        foreach (var task in tasks)
        {
            _rabbitMQService.PublishTaskNotification(task.UserId, task.Id.ToString(), task.Title);
        }

        return tasks
            .Select(task => new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status,
                UserId = task.UserId,
                UserName = lookupUserNames.GetValueOrDefault(task.UserId),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            })
            .ToList();
    }

    private static string GenerateTitle(Random random)
    {
        var prefix = TitlePrefixes[random.Next(TitlePrefixes.Length)];
        var subject = TitleSubjects[random.Next(TitleSubjects.Length)];
        var code = random.Next(100, 999);
        return $"{prefix} {subject} #{code}";
    }

    private static string GenerateMarkdownDescription(string title, Random random)
    {
        var scenario = ScenarioTemplates[random.Next(ScenarioTemplates.Length)];
        var objective = Objectives[random.Next(Objectives.Length)];
        var checklist = ChecklistItems
            .OrderBy(_ => random.Next())
            .Take(3)
            .Select(item => $"- [ ] {item}");
        var note = Notes[random.Next(Notes.Length)];

        return
            $"## Contexto\n\n{scenario}\n\n" +
            $"### Objetivo\n\n{objective}\n\n" +
            $"### Checklist\n{string.Join("\n", checklist)}\n\n" +
            $"> Nota: {note}\n\n" +
            $"_Gerado automaticamente para **{title}**._";
    }
}


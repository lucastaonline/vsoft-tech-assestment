using VSoftTechAssestment.Api.Models.DTOs.Task;

namespace VSoftTechAssestment.Api.Services;

public interface ITaskService
{
    /// <summary>
    /// Obtém todas as tarefas (de todos os usuários)
    /// </summary>
    Task<IEnumerable<TaskResponse>> GetAllTasksAsync();

    /// <summary>
    /// Obtém uma tarefa específica por ID
    /// </summary>
    Task<TaskResponse?> GetTaskByIdAsync(Guid id);

    /// <summary>
    /// Cria uma nova tarefa para o usuário especificado
    /// </summary>
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string assignedUserId, string createdByUserId);

    /// <summary>
    /// Atualiza uma tarefa existente (apenas se o usuário autenticado for o dono)
    /// </summary>
    Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string authenticatedUserId, string newUserId);

    /// <summary>
    /// Exclui uma tarefa (apenas se o usuário autenticado for o dono)
    /// </summary>
    Task<bool> DeleteTaskAsync(Guid id, string authenticatedUserId);

    /// <summary>
    /// Gera tarefas mockadas para usuários existentes
    /// </summary>
    Task<IReadOnlyList<TaskResponse>> CreateMockTasksAsync(int amount);
}


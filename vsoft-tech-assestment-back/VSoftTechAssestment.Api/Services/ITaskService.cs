using VSoftTechAssestment.Api.Models.DTOs.Task;

namespace VSoftTechAssestment.Api.Services;

public interface ITaskService
{
    /// <summary>
    /// Obtém todas as tarefas (de todos os usuários)
    /// </summary>
    Task<IEnumerable<TaskResponse>> GetAllTasksAsync();

    /// <summary>
    /// Obtém todas as tarefas com paginação baseada em cursor
    /// </summary>
    Task<PaginatedTasksResponse> GetAllTasksPaginatedAsync(Guid? cursor, int pageSize);

    /// <summary>
    /// Obtém uma tarefa específica por ID
    /// </summary>
    Task<TaskResponse?> GetTaskByIdAsync(Guid id);

    /// <summary>
    /// Cria uma nova tarefa para o usuário especificado
    /// </summary>
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId);

    /// <summary>
    /// Atualiza uma tarefa existente (apenas se o usuário autenticado for o dono)
    /// </summary>
    Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string authenticatedUserId, string newUserId);

    /// <summary>
    /// Exclui uma tarefa (apenas se o usuário autenticado for o dono)
    /// </summary>
    Task<bool> DeleteTaskAsync(Guid id, string authenticatedUserId);
}


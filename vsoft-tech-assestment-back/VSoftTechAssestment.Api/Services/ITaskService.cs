using VSoftTechAssestment.Api.Models.DTOs.Task;

namespace VSoftTechAssestment.Api.Services;

public interface ITaskService
{
    /// <summary>
    /// Obtém todas as tarefas de um usuário
    /// </summary>
    Task<IEnumerable<TaskResponse>> GetUserTasksAsync(string userId);

    /// <summary>
    /// Obtém tarefas de um usuário com paginação baseada em cursor
    /// </summary>
    Task<PaginatedTasksResponse> GetUserTasksPaginatedAsync(string userId, Guid? cursor, int pageSize);

    /// <summary>
    /// Obtém uma tarefa específica por ID (verificando se pertence ao usuário)
    /// </summary>
    Task<TaskResponse?> GetTaskByIdAsync(Guid id, string userId);

    /// <summary>
    /// Cria uma nova tarefa para o usuário especificado
    /// </summary>
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId);

    /// <summary>
    /// Atualiza uma tarefa existente (verificando se pertence ao usuário autenticado)
    /// </summary>
    Task<bool> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string authenticatedUserId, string newUserId);

    /// <summary>
    /// Exclui uma tarefa (verificando se pertence ao usuário)
    /// </summary>
    Task<bool> DeleteTaskAsync(Guid id, string userId);
}


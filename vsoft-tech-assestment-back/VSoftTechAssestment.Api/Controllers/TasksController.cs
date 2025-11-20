using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSoftTechAssestment.Api.Models.DTOs.Task;
using VSoftTechAssestment.Api.Services;

namespace VSoftTechAssestment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Lista todas as tarefas com suporte a paginação
    /// </summary>
    /// <param name="cursor">Cursor para paginação (opcional)</param>
    /// <param name="pageSize">Tamanho da página (padrão: 20)</param>
    /// <returns>Lista de todas as tarefas (paginada ou completa)</returns>
    /// <response code="200">Lista de tarefas retornada com sucesso</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedTasksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetTasks([FromQuery] Guid? cursor = null, [FromQuery] int? pageSize = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Se não há parâmetros de paginação, retornar lista completa (compatibilidade)
        if (!cursor.HasValue && !pageSize.HasValue)
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }

        // Retornar paginado
        var paginatedResult = await _taskService.GetAllTasksPaginatedAsync(cursor, pageSize ?? 20);
        return Ok(paginatedResult);
    }

    /// <summary>
    /// Obtém uma tarefa específica por ID
    /// </summary>
    /// <param name="id">ID da tarefa (Guid)</param>
    /// <returns>Tarefa encontrada</returns>
    /// <response code="200">Tarefa encontrada</response>
    /// <response code="404">Tarefa não encontrada</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var task = await _taskService.GetTaskByIdAsync(id);
        
        if (task == null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    /// <summary>
    /// Cria uma nova tarefa atribuída ao usuário autenticado
    /// </summary>
    /// <param name="request">Dados da tarefa a ser criada</param>
    /// <returns>Tarefa criada com ID gerado</returns>
    /// <response code="201">Tarefa criada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Não autenticado</response>
    /// <remarks>
    /// A tarefa será automaticamente atribuída ao usuário autenticado e uma notificação será enviada via RabbitMQ.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var authenticatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authenticatedUserId))
        {
            return Unauthorized();
        }

        // Se userId não foi fornecido no request, usa o usuário autenticado
        var userId = !string.IsNullOrEmpty(request.UserId) ? request.UserId : authenticatedUserId;

        var task = await _taskService.CreateTaskAsync(request, userId);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    /// <summary>
    /// Atualiza uma tarefa existente (apenas se o usuário autenticado for o dono)
    /// </summary>
    /// <param name="id">ID da tarefa a ser atualizada</param>
    /// <param name="request">Dados atualizados da tarefa</param>
    /// <returns>Tarefa atualizada</returns>
    /// <response code="200">Tarefa atualizada com sucesso</response>
    /// <response code="403">Usuário não é o dono da tarefa</response>
    /// <response code="404">Tarefa não encontrada</response>
    /// <response code="401">Não autenticado</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, UpdateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var authenticatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authenticatedUserId))
        {
            return Unauthorized();
        }

        // Validar que userId foi fornecido no request
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest(new { error = "UserId é obrigatório" });
        }

        // Verificar se a tarefa existe
        var existingTask = await _taskService.GetTaskByIdAsync(id);
        if (existingTask == null)
        {
            return NotFound();
        }

        // Verificar se o usuário autenticado é o dono da tarefa
        if (existingTask.UserId != authenticatedUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Você não tem permissão para editar esta tarefa" });
        }

        // Para update, userId é obrigatório e atualiza o responsável da tarefa
        var updatedTask = await _taskService.UpdateTaskAsync(id, request, authenticatedUserId, request.UserId);
        
        if (updatedTask == null)
        {
            return NotFound();
        }

        return Ok(updatedTask);
    }

    /// <summary>
    /// Exclui uma tarefa (apenas se o usuário autenticado for o dono)
    /// </summary>
    /// <param name="id">ID da tarefa a ser excluída</param>
    /// <returns>Sem conteúdo (204)</returns>
    /// <response code="204">Tarefa excluída com sucesso</response>
    /// <response code="403">Usuário não é o dono da tarefa</response>
    /// <response code="404">Tarefa não encontrada</response>
    /// <response code="401">Não autenticado</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var authenticatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authenticatedUserId))
        {
            return Unauthorized();
        }

        // Verificar se a tarefa existe
        var existingTask = await _taskService.GetTaskByIdAsync(id);
        if (existingTask == null)
        {
            return NotFound();
        }

        // Verificar se o usuário autenticado é o dono da tarefa
        if (existingTask.UserId != authenticatedUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Você não tem permissão para deletar esta tarefa" });
        }

        var deleted = await _taskService.DeleteTaskAsync(id, authenticatedUserId);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}


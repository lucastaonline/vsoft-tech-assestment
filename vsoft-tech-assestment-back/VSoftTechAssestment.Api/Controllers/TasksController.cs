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
    /// Lista todas as tarefas do usuário autenticado
    /// </summary>
    /// <returns>Lista de tarefas do usuário</returns>
    /// <response code="200">Lista de tarefas retornada com sucesso</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetTasks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var tasks = await _taskService.GetUserTasksAsync(userId);
        return Ok(tasks);
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

        var task = await _taskService.GetTaskByIdAsync(id, userId);
        
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var task = await _taskService.CreateTaskAsync(request, userId);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    /// <summary>
    /// Atualiza uma tarefa existente
    /// </summary>
    /// <param name="id">ID da tarefa a ser atualizada</param>
    /// <param name="request">Dados atualizados da tarefa</param>
    /// <returns>Sem conteúdo (204)</returns>
    /// <response code="204">Tarefa atualizada com sucesso</response>
    /// <response code="404">Tarefa não encontrada</response>
    /// <response code="401">Não autenticado</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTask(Guid id, UpdateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var updated = await _taskService.UpdateTaskAsync(id, request, userId);
        
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Exclui uma tarefa
    /// </summary>
    /// <param name="id">ID da tarefa a ser excluída</param>
    /// <returns>Sem conteúdo (204)</returns>
    /// <response code="204">Tarefa excluída com sucesso</response>
    /// <response code="404">Tarefa não encontrada</response>
    /// <response code="401">Não autenticado</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var deleted = await _taskService.DeleteTaskAsync(id, userId);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}


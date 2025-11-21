using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
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
    private readonly IDataProtector _calendarProtector;

    public TasksController(ITaskService taskService, IDataProtectionProvider dataProtectionProvider)
    {
        _taskService = taskService;
        _calendarProtector = dataProtectionProvider.CreateProtector("tasks-calendar-link");
    }

    /// <summary>
    /// Lista todas as tarefas
    /// </summary>
    /// <returns>Lista de todas as tarefas</returns>
    /// <response code="200">Lista de tarefas retornada com sucesso</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetTasks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var tasks = await _taskService.GetAllTasksAsync();
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
        var assignedUserId = !string.IsNullOrEmpty(request.UserId) ? request.UserId : authenticatedUserId;

        var task = await _taskService.CreateTaskAsync(request, assignedUserId, authenticatedUserId);
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

    /// <summary>
    /// Gera tarefas mockadas distribuídas entre usuários existentes
    /// </summary>
    /// <param name="request">Quantidade de tarefas a serem criadas</param>
    /// <returns>Lista de tarefas geradas</returns>
    /// <response code="200">Tarefas geradas com sucesso</response>
    /// <response code="400">Entrada inválida ou ausência de usuários</response>
    /// <response code="401">Não autenticado</response>
    [HttpPost("mock")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> CreateMockTasks([FromBody] GenerateMockTasksRequest request)
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

        try
        {
            var tasks = await _taskService.CreateMockTasksAsync(request.Amount);
            return Ok(tasks);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gera um token de calendário pessoal e retorna o link iCal correspondente.
    /// </summary>
    /// <param name="scope">Escopo do calendário: "user" (padrão) ou "all".</param>
    /// <returns>Token protegido e URL do feed iCal.</returns>
    [HttpGet("calendar/token")]
    [ProducesResponseType(typeof(CalendarLinkResponse), StatusCodes.Status200OK)]
    public ActionResult<CalendarLinkResponse> GetCalendarLink([FromQuery] string scope = "user")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var normalizedScope = string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase)
            ? "all"
            : "user";
        var issuedAt = DateTime.UtcNow;
        var rawToken = $"{userId}|{normalizedScope}|{issuedAt:O}";
        var token = _calendarProtector.Protect(rawToken);
        var host = Request.Host.HasValue ? Request.Host.Value : "localhost";
        var baseUrl = $"{Request.Scheme}://{host}";
        var url = $"{baseUrl}/api/tasks/calendar.ics?token={Uri.EscapeDataString(token)}";

        return Ok(new CalendarLinkResponse
        {
            Token = token,
            Url = url,
            IssuedAt = issuedAt,
            Scope = normalizedScope
        });
    }

    /// <summary>
    /// Retorna o feed iCal de tarefas para o token informado.
    /// </summary>
    /// <param name="token">Token protegido emitido para o usuário.</param>
    /// <returns>Arquivo ICS com os eventos das tarefas.</returns>
    [AllowAnonymous]
    [HttpGet("calendar.ics")]
    [Produces("text/calendar")]
    public async Task<IActionResult> GetCalendarFeed([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { error = "Token é obrigatório." });
        }

        string payload;
        try
        {
            payload = _calendarProtector.Unprotect(token);
        }
        catch
        {
            return Unauthorized(new { error = "Token inválido." });
        }

        var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return Unauthorized(new { error = "Token inválido." });
        }

        var ownerUserId = parts[0];
        var scope = parts.Length > 1 ? parts[1] : "user";
        IEnumerable<TaskResponse> tasks;

        if (string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            tasks = await _taskService.GetAllTasksAsync();
        }
        else
        {
            scope = "user";
            tasks = await _taskService.GetTasksByUserAsync(ownerUserId);
        }

        var calendar = BuildICalendar(
            tasks,
            string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase) ? "VSoftTech Board" : ownerUserId,
            scope);

        return Content(calendar, "text/calendar", Encoding.UTF8);
    }

    private static string BuildICalendar(IEnumerable<TaskResponse> tasks, string organizerName, string scope)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//VSoftTech//Tasks//PT-BR");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");

        foreach (var task in tasks)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{task.Id}@vsofttech");

            var referenceDate = task.DueDate != default
                ? task.DueDate
                : (task.CreatedAt != default ? task.CreatedAt : DateTime.UtcNow);
            var start = referenceDate.ToUniversalTime();
            var end = start.AddHours(1);

            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}");
            sb.AppendLine($"DTSTART:{start:yyyyMMdd'T'HHmmss'Z'}");
            sb.AppendLine($"DTEND:{end:yyyyMMdd'T'HHmmss'Z'}");
            sb.AppendLine($"SUMMARY:{EscapeICalText(task.Title)}");

            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                sb.AppendLine($"DESCRIPTION:{EscapeICalText(task.Description)}");
            }

            sb.AppendLine($"CATEGORIES:{task.Status}");
            sb.AppendLine($"STATUS:CONFIRMED");
            sb.AppendLine($"ORGANIZER;CN={EscapeICalText(organizerName)}:MAILTO:no-reply@vsofttech.local");
            sb.AppendLine($"X-SCOPE:{scope.ToUpperInvariant()}");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static string EscapeICalText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace(@"\", @"\\")
            .Replace(";", @"\;")
            .Replace(",", @"\,")
            .Replace("\r\n", @"\n")
            .Replace("\n", @"\n");
    }
}


using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSoftTechAssestment.Api.Models.DTOs.Notification;
using VSoftTechAssestment.Api.Services;

namespace VSoftTechAssestment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Obtém todas as notificações do usuário autenticado
    /// </summary>
    /// <returns>Lista de notificações</returns>
    /// <response code="200">Lista de notificações retornada com sucesso</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// Obtém a quantidade de notificações não lidas do usuário autenticado
    /// </summary>
    /// <returns>Quantidade de notificações não lidas</returns>
    /// <response code="200">Quantidade retornada com sucesso</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    /// <summary>
    /// Marca uma notificação como lida
    /// </summary>
    /// <param name="id">ID da notificação</param>
    /// <returns>Notificação atualizada</returns>
    /// <response code="200">Notificação marcada como lida</response>
    /// <response code="404">Notificação não encontrada</response>
    /// <response code="401">Não autenticado</response>
    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notification = await _notificationService.MarkAsReadAsync(id, userId);
        if (notification == null)
        {
            return NotFound();
        }

        return Ok(notification);
    }

    /// <summary>
    /// Marca todas as notificações do usuário como lidas
    /// </summary>
    /// <returns>Sem conteúdo (204)</returns>
    /// <response code="204">Todas as notificações foram marcadas como lidas</response>
    /// <response code="401">Não autenticado</response>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }
}


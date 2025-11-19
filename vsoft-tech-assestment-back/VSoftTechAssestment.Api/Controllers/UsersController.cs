using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSoftTechAssestment.Api.Models.DTOs.User;
using VSoftTechAssestment.Api.Services;

namespace VSoftTechAssestment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lista todos os usuários do sistema
    /// </summary>
    /// <returns>Lista de usuários (id, userName, email)</returns>
    /// <response code="200">Lista de usuários retornada com sucesso</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserListItemResponse>>> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Cria múltiplos usuários aleatórios em massa
    /// </summary>
    /// <param name="request">Quantidade de usuários e máscara de nome de usuário</param>
    /// <returns>Estatísticas de criação (sucessos e falhas)</returns>
    /// <response code="200">Processo de criação concluído</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Não autenticado</response>
    /// <remarks>
    /// O campo userNameMask deve conter {{random}} que será substituído por um sufixo aleatório.
    /// Exemplo: "user_{{random}}" gerará "user_123456", "user_789012", etc.
    /// </remarks>
    [HttpPost("createRandom")]
    [ProducesResponseType(typeof(CreateRandomUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateRandomUsersResponse>> CreateRandomUsers(
        [FromBody] CreateRandomUsersRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _userService.CreateRandomUsersAsync(request);
        return Ok(response);
    }
}


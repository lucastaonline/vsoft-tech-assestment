using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSoftTechAssestment.Api.Models.DTOs.Auth;
using VSoftTechAssestment.Api.Services;

namespace VSoftTechAssestment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    /// <param name="request">Dados do usuário para registro (email, username, password)</param>
    /// <returns>Resposta com status do registro e ID do usuário criado</returns>
    /// <response code="200">Usuário registrado com sucesso</response>
    /// <response code="400">Dados inválidos ou usuário já existe</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errorResponse = new RegisterResponse
            {
                Success = false,
                Message = "Dados inválidos",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            };
            return BadRequest(errorResponse);
        }

        var response = await _authService.RegisterAsync(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Realiza login e retorna token JWT para autenticação
    /// </summary>
    /// <param name="request">Credenciais de login (username/email e password)</param>
    /// <returns>Token JWT e informações do usuário</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="401">Credenciais inválidas</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errorResponse = new LoginResponse
            {
                Success = false,
                Message = "Dados inválidos"
            };
            return BadRequest(errorResponse);
        }

        var response = await _authService.LoginAsync(request);

        if (!response.Success)
        {
            return Unauthorized(response);
        }

        // Configurar cookies HttpOnly para segurança (opcional, para navegadores)
        // Também retornamos o token no body para suportar clientes não-navegador
        if (response.Success && response.Token != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Protege contra XSS
                Secure = !Request.Host.Host.Contains("localhost"), // HTTPS em produção, HTTP em localhost
                SameSite = SameSiteMode.Strict, // Protege contra CSRF
                Expires = response.ExpiresAt.HasValue 
                    ? new DateTimeOffset(response.ExpiresAt.Value)
                    : DateTimeOffset.UtcNow.AddDays(7), // Default 7 dias
                Path = "/"
            };

            Response.Cookies.Append("auth_token", response.Token, cookieOptions);

            if (response.RefreshToken != null)
            {
                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !Request.Host.Host.Contains("localhost"),
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30), // Refresh token dura 30 dias
                    Path = "/"
                };

                Response.Cookies.Append("refresh_token", response.RefreshToken, refreshCookieOptions);
            }
        }

        // Retornar resposta completa com token no body (para clientes não-navegador)
        // O cookie também é setado para navegadores que suportam cookies
        return Ok(response);
    }

    /// <summary>
    /// Renova o token de acesso usando o refresh token
    /// O refresh token pode ser enviado no body ou será lido automaticamente do cookie
    /// </summary>
    /// <param name="request">Refresh token para renovação (opcional, pode vir do cookie)</param>
    /// <returns>Novo token JWT e novo refresh token</returns>
    /// <response code="200">Token renovado com sucesso</response>
    /// <response code="401">Refresh token inválido ou expirado</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest? request = null)
    {
        // Tentar obter refresh token do body ou do cookie
        // Para navegadores, o cookie HttpOnly é mais seguro
        // Para clientes não-navegador, enviamos no body
        var refreshToken = request?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = Request.Cookies["refresh_token"];
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "Refresh token não fornecido"
            });
        }

        var response = await _authService.RefreshTokenAsync(refreshToken);

        if (!response.Success)
        {
            return Unauthorized(response);
        }

        // Configurar cookies HttpOnly com os novos tokens
        if (response.Success && response.Token != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !Request.Host.Host.Contains("localhost"),
                SameSite = SameSiteMode.Strict,
                Expires = response.ExpiresAt.HasValue 
                    ? new DateTimeOffset(response.ExpiresAt.Value)
                    : DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            };

            Response.Cookies.Append("auth_token", response.Token, cookieOptions);

            if (response.RefreshToken != null)
            {
                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !Request.Host.Host.Contains("localhost"),
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    Path = "/"
                };

                Response.Cookies.Append("refresh_token", response.RefreshToken, refreshCookieOptions);
            }
        }

        return Ok(response);
    }

    /// <summary>
    /// Realiza logout e remove cookies de autenticação
    /// </summary>
    /// <returns>Status do logout</returns>
    /// <response code="200">Logout realizado com sucesso</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // Remover cookies de autenticação
        Response.Cookies.Delete("auth_token");
        Response.Cookies.Delete("refresh_token");

        return Ok(new { success = true, message = "Logout realizado com sucesso" });
    }
}


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using VSoftTechAssestment.Api.Models.DTOs.Auth;

namespace VSoftTechAssestment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
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
        var response = new RegisterResponse();

        if (!ModelState.IsValid)
        {
            response.Success = false;
            response.Message = "Dados inválidos";
            response.Errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(response);
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            response.Success = false;
            response.Message = "Usuário com este email já existe";
            response.Errors.Add("Email já está em uso");
            return BadRequest(response);
        }

        existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
        {
            response.Success = false;
            response.Message = "Nome de usuário já existe";
            response.Errors.Add("Nome de usuário já está em uso");
            return BadRequest(response);
        }

        var user = new IdentityUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            response.Success = false;
            response.Message = "Erro ao criar usuário";
            response.Errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(response);
        }

        response.Success = true;
        response.Message = "Usuário criado com sucesso";
        response.UserId = user.Id;

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
        var response = new LoginResponse();

        if (!ModelState.IsValid)
        {
            response.Success = false;
            response.Message = "Dados inválidos";
            return BadRequest(response);
        }

        var user = await _userManager.FindByEmailAsync(request.UserNameOrEmail) 
                   ?? await _userManager.FindByNameAsync(request.UserNameOrEmail);

        if (user == null)
        {
            response.Success = false;
            response.Message = "Usuário ou senha inválidos";
            return Unauthorized(response);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            response.Success = false;
            response.Message = "Usuário ou senha inválidos";
            return Unauthorized(response);
        }

        var token = GenerateJwtToken(user);
        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

        response.Success = true;
        response.Message = "Login realizado com sucesso";
        response.Token = token;
        response.ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        response.UserId = user.Id;
        response.UserName = user.UserName;
        response.Email = user.Email;

        return Ok(response);
    }

    private string GenerateJwtToken(IdentityUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");
        var expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 60);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}


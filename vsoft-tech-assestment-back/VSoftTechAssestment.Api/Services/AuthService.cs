using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using VSoftTechAssestment.Api.Models.DTOs.Auth;

namespace VSoftTechAssestment.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var response = new RegisterResponse();

        // Check if user with email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            response.Success = false;
            response.Message = "Usuário com este email já existe";
            response.Errors.Add("Email já está em uso");
            return response;
        }

        // Check if user with username already exists
        existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
        {
            response.Success = false;
            response.Message = "Nome de usuário já existe";
            response.Errors.Add("Nome de usuário já está em uso");
            return response;
        }

        // Create new user
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
            return response;
        }

        response.Success = true;
        response.Message = "Usuário criado com sucesso";
        response.UserId = user.Id;

        return response;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = new LoginResponse();

        // Try to find user by email or username
        var user = await _userManager.FindByEmailAsync(request.UserNameOrEmail) 
                   ?? await _userManager.FindByNameAsync(request.UserNameOrEmail);

        if (user == null)
        {
            response.Success = false;
            response.Message = "Usuário ou senha inválidos";
            return response;
        }

        // Verify password
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            response.Success = false;
            response.Message = "Usuário ou senha inválidos";
            return response;
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);
        var refreshToken = GenerateRefreshToken(user);

        response.Success = true;
        response.Message = "Login realizado com sucesso";
        response.Token = token;
        response.RefreshToken = refreshToken;
        response.ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        response.UserId = user.Id;
        response.UserName = user.UserName;
        response.Email = user.Email;

        return response;
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var response = new LoginResponse();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            response.Success = false;
            response.Message = "Refresh token inválido";
            return response;
        }

        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true, // Validar expiração do refresh token
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out SecurityToken validatedToken);
            
            // Verificar se é um refresh token
            var tokenType = principal.FindFirst("type")?.Value;
            if (tokenType != "refresh")
            {
                response.Success = false;
                response.Message = "Token inválido";
                return response;
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                response.Success = false;
                response.Message = "Refresh token inválido";
                return response;
            }

            // Buscar usuário
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "Usuário não encontrado";
                return response;
            }

            // Gerar novo token e novo refresh token
            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user);
            var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

            response.Success = true;
            response.Message = "Token renovado com sucesso";
            response.Token = newToken;
            response.RefreshToken = newRefreshToken;
            response.ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
            response.UserId = user.Id;
            response.UserName = user.UserName;
            response.Email = user.Email;

            return response;
        }
        catch
        {
            response.Success = false;
            response.Message = "Refresh token inválido ou expirado";
            return response;
        }
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

    private string GenerateRefreshToken(IdentityUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        // Refresh token com expiração de 30 dias e inclui userId
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("type", "refresh")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(30),
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


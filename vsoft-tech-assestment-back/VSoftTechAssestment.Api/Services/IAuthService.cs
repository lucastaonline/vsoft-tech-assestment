using VSoftTechAssestment.Api.Models.DTOs.Auth;

namespace VSoftTechAssestment.Api.Services;

public interface IAuthService
{
    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Realiza login e retorna token JWT
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);
}


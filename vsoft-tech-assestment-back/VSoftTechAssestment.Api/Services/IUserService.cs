using VSoftTechAssestment.Api.Models.DTOs.User;

namespace VSoftTechAssestment.Api.Services;

public interface IUserService
{
    /// <summary>
    /// Cria múltiplos usuários aleatórios em massa
    /// </summary>
    Task<CreateRandomUsersResponse> CreateRandomUsersAsync(CreateRandomUsersRequest request);

    /// <summary>
    /// Obtém todos os usuários do sistema
    /// </summary>
    Task<IEnumerable<UserListItemResponse>> GetAllUsersAsync();
}


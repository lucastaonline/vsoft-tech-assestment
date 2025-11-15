using VSoftTechAssestment.Api.Models.DTOs.User;

namespace VSoftTechAssestment.Api.Services;

public interface IUserService
{
    /// <summary>
    /// Cria múltiplos usuários aleatórios em massa
    /// </summary>
    Task<CreateRandomUsersResponse> CreateRandomUsersAsync(CreateRandomUsersRequest request);
}


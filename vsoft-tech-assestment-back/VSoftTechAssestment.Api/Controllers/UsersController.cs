using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VSoftTechAssestment.Api.Models.DTOs.User;

namespace VSoftTechAssestment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<IdentityUser> userManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _logger = logger;
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

        var response = new CreateRandomUsersResponse();
        var errors = new List<string>();
        int createdCount = 0;
        int failedCount = 0;

        var random = new Random();

        for (int i = 0; i < request.Amount; i++)
        {
            try
            {
                // Generate random suffix
                var randomSuffix = random.Next(100000, 999999).ToString();
                
                // Replace {{random}} with the random suffix
                var userName = request.UserNameMask.Replace("{{random}}", randomSuffix);
                
                // Generate email based on username
                var email = $"{userName}@example.com";

                // Check if user already exists
                var existingUser = await _userManager.FindByNameAsync(userName);
                if (existingUser != null)
                {
                    failedCount++;
                    errors.Add($"User '{userName}' already exists");
                    continue;
                }

                existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    failedCount++;
                    errors.Add($"Email '{email}' already exists");
                    continue;
                }

                // Create user with random password
                var user = new IdentityUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true
                };

                // Generate a random password (8-16 characters)
                var passwordLength = random.Next(8, 17);
                var password = GenerateRandomPassword(passwordLength);

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    createdCount++;
                }
                else
                {
                    failedCount++;
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                    errors.Add($"Failed to create user '{userName}': {errorMessages}");
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"Error creating user: {ex.Message}");
                _logger.LogError(ex, "Error creating random user");
            }
        }

        response.Success = createdCount > 0;
        response.Message = $"Created {createdCount} users. {failedCount} failed.";
        response.CreatedCount = createdCount;
        response.FailedCount = failedCount;
        response.Errors = errors.Take(100).ToList(); // Limit errors to first 100

        return Ok(response);
    }

    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}


using Microsoft.AspNetCore.Identity;
using VSoftTechAssestment.Api.Models.DTOs.User;

namespace VSoftTechAssestment.Api.Services;

public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<IdentityUser> userManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<CreateRandomUsersResponse> CreateRandomUsersAsync(CreateRandomUsersRequest request)
    {
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

        return response;
    }

    public async Task<IEnumerable<UserListItemResponse>> GetAllUsersAsync()
    {
        var users = _userManager.Users
            .OrderBy(u => u.UserName)
            .Select(u => new UserListItemResponse
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            })
            .ToList();

        return await Task.FromResult(users);
    }

    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}


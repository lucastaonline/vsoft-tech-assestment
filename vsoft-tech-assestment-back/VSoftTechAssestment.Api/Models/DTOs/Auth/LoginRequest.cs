using System.ComponentModel.DataAnnotations;

namespace VSoftTechAssestment.Api.Models.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}


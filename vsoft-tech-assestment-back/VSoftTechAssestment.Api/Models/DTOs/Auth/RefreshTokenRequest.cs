using System.ComponentModel.DataAnnotations;

namespace VSoftTechAssestment.Api.Models.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}


using System.ComponentModel.DataAnnotations;

namespace VSoftTechAssestment.Api.Models.DTOs.User;

public class CreateRandomUsersRequest
{
    [Required]
    [Range(1, 10000, ErrorMessage = "Amount must be between 1 and 10000")]
    public int Amount { get; set; }

    [Required]
    public string UserNameMask { get; set; } = string.Empty;
}


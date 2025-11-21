using System.ComponentModel.DataAnnotations;

namespace VSoftTechAssestment.Api.Models.DTOs.Task;

public class GenerateMockTasksRequest
{
    [Required]
    [Range(1, 50, ErrorMessage = "A quantidade deve estar entre 1 e 50.")]
    public int Amount { get; set; }
}


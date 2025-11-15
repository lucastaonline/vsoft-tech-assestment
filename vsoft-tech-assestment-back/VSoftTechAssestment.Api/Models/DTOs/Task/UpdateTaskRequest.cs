using System.ComponentModel.DataAnnotations;
using VSoftTechAssestment.Api.Models.Entities;

namespace VSoftTechAssestment.Api.Models.DTOs.Task;

public class UpdateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    public Models.Entities.TaskStatus Status { get; set; }
}


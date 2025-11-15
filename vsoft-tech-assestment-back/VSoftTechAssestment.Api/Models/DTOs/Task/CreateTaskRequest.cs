using System.ComponentModel.DataAnnotations;
using VSoftTechAssestment.Api.Models.Entities;

namespace VSoftTechAssestment.Api.Models.DTOs.Task;

public class CreateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime DueDate { get; set; }

    public Models.Entities.TaskStatus Status { get; set; } = Models.Entities.TaskStatus.Pending;
}


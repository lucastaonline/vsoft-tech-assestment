using VSoftTechAssestment.Api.Models.Entities;

namespace VSoftTechAssestment.Api.Models.DTOs.Task;

public class PaginatedTasksResponse
{
    public IEnumerable<TaskResponse> Tasks { get; set; } = new List<TaskResponse>();
    public Guid? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

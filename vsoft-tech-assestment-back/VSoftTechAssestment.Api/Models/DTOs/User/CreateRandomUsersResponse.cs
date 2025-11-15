namespace VSoftTechAssestment.Api.Models.DTOs.User;

public class CreateRandomUsersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}


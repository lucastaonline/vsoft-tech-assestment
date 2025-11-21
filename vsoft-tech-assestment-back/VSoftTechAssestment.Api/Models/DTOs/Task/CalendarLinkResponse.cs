namespace VSoftTechAssestment.Api.Models.DTOs.Task;

public class CalendarLinkResponse
{
    public string Token { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string Scope { get; set; } = "user";
}



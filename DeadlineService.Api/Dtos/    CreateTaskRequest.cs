namespace DeadlineService.Api.Dtos;

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DeadlineAt { get; set; }
    public string Priority { get; set; } = "Medium";
}
namespace DeadlineService.Api.Dtos;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DeadlineAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string Priority { get; set; } = "Medium";
}
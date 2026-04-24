namespace DeadlineService.Api.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DeadlineAt { get; set; }

    public string Status { get; set; } = "Pending";
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
namespace DeadlineService.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsBlocked { get; set; } = false;

    public List<TaskItem> Tasks { get; set; } = new();
}
namespace DeadlineService.Api.Models;

public class NotificationSetting
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }

    public int RemindBeforeValue { get; set; }
    public string RemindBeforeUnit { get; set; } = "Hours";
    public string Channel { get; set; } = "email";
}
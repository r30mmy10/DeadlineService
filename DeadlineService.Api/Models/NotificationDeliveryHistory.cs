namespace DeadlineService.Api.Models;

public class NotificationDeliveryHistory
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    public int AttemptNumber { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
namespace DeadlineService.Api.Models;

public static class NotificationUnitTests
{
    public static void RunAll()
    {
        Notification_ShouldHaveEmptyMessageByDefault();
        Notification_ShouldHaveDefaultChannelEmail();
        Notification_ShouldHaveDefaultDeliveryStatusPending();
        Notification_ShouldHaveCreatedAtByDefault();
        Notification_ShouldHaveNullSentAtByDefault();
        Notification_ShouldAllowSettingMainProperties();
    }

    private static void Notification_ShouldHaveEmptyMessageByDefault()
    {
        var notification = new Notification();

        if (notification.Message != string.Empty)
            throw new Exception("Notification test failed: Message should be empty by default.");
    }

    private static void Notification_ShouldHaveDefaultChannelEmail()
    {
        var notification = new Notification();

        if (notification.Channel != "email")
            throw new Exception("Notification test failed: default Channel should be email.");
    }

    private static void Notification_ShouldHaveDefaultDeliveryStatusPending()
    {
        var notification = new Notification();

        if (notification.DeliveryStatus != "Pending")
            throw new Exception("Notification test failed: default DeliveryStatus should be Pending.");
    }

    private static void Notification_ShouldHaveCreatedAtByDefault()
    {
        var beforeCreate = DateTime.UtcNow;

        var notification = new Notification();

        var afterCreate = DateTime.UtcNow;

        if (notification.CreatedAt < beforeCreate || notification.CreatedAt > afterCreate)
            throw new Exception("Notification test failed: CreatedAt should be set to current UTC time by default.");
    }

    private static void Notification_ShouldHaveNullSentAtByDefault()
    {
        var notification = new Notification();

        if (notification.SentAt != null)
            throw new Exception("Notification test failed: SentAt should be null by default.");
    }

    private static void Notification_ShouldAllowSettingMainProperties()
    {
        var notificationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@example.com"
        };

        var task = new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "Task for notification",
            User = user
        };

        var createdAt = DateTime.UtcNow;
        var sentAt = DateTime.UtcNow.AddMinutes(5);

        var notification = new Notification
        {
            Id = notificationId,
            TaskId = taskId,
            Task = task,
            UserId = userId,
            User = user,
            Message = "Deadline reminder",
            Channel = "telegram",
            DeliveryStatus = "Sent",
            CreatedAt = createdAt,
            SentAt = sentAt
        };

        if (notification.Id != notificationId)
            throw new Exception("Notification test failed: Id was set incorrectly.");

        if (notification.TaskId != taskId)
            throw new Exception("Notification test failed: TaskId was set incorrectly.");

        if (notification.Task != task)
            throw new Exception("Notification test failed: Task navigation property was set incorrectly.");

        if (notification.UserId != userId)
            throw new Exception("Notification test failed: UserId was set incorrectly.");

        if (notification.User != user)
            throw new Exception("Notification test failed: User navigation property was set incorrectly.");

        if (notification.Message != "Deadline reminder")
            throw new Exception("Notification test failed: Message was set incorrectly.");

        if (notification.Channel != "telegram")
            throw new Exception("Notification test failed: Channel was set incorrectly.");

        if (notification.DeliveryStatus != "Sent")
            throw new Exception("Notification test failed: DeliveryStatus was set incorrectly.");

        if (notification.CreatedAt != createdAt)
            throw new Exception("Notification test failed: CreatedAt was set incorrectly.");

        if (notification.SentAt != sentAt)
            throw new Exception("Notification test failed: SentAt was set incorrectly.");
    }
}
namespace DeadlineService.Api.Models;

public static class NotificationSettingUnitTests
{
    public static void RunAll()
    {
        NotificationSetting_ShouldHaveDefaultRemindBeforeUnitHours();
        NotificationSetting_ShouldHaveDefaultChannelEmail();
        NotificationSetting_ShouldHaveDefaultRemindBeforeValueZero();
        NotificationSetting_ShouldAllowSettingMainProperties();
    }

    private static void NotificationSetting_ShouldHaveDefaultRemindBeforeUnitHours()
    {
        var setting = new NotificationSetting();

        if (setting.RemindBeforeUnit != "Hours")
            throw new Exception("NotificationSetting test failed: default RemindBeforeUnit should be Hours.");
    }

    private static void NotificationSetting_ShouldHaveDefaultChannelEmail()
    {
        var setting = new NotificationSetting();

        if (setting.Channel != "email")
            throw new Exception("NotificationSetting test failed: default Channel should be email.");
    }

    private static void NotificationSetting_ShouldHaveDefaultRemindBeforeValueZero()
    {
        var setting = new NotificationSetting();

        if (setting.RemindBeforeValue != 0)
            throw new Exception("NotificationSetting test failed: RemindBeforeValue should be 0 by default.");
    }

    private static void NotificationSetting_ShouldAllowSettingMainProperties()
    {
        var id = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var setting = new NotificationSetting
        {
            Id = id,
            TaskId = taskId,
            RemindBeforeValue = 2,
            RemindBeforeUnit = "Days",
            Channel = "telegram"
        };

        if (setting.Id != id)
            throw new Exception("NotificationSetting test failed: Id was set incorrectly.");

        if (setting.TaskId != taskId)
            throw new Exception("NotificationSetting test failed: TaskId was set incorrectly.");

        if (setting.RemindBeforeValue != 2)
            throw new Exception("NotificationSetting test failed: RemindBeforeValue was set incorrectly.");

        if (setting.RemindBeforeUnit != "Days")
            throw new Exception("NotificationSetting test failed: RemindBeforeUnit was set incorrectly.");

        if (setting.Channel != "telegram")
            throw new Exception("NotificationSetting test failed: Channel was set incorrectly.");
    }
}
namespace DeadlineService.Api.Models;

public static class TaskItemUnitTests
{
    public static void RunAll()
    {
        TaskItem_ShouldHaveEmptyTitleByDefault();
        TaskItem_ShouldHaveNullDescriptionByDefault();
        TaskItem_ShouldHaveDefaultStatusPending();
        TaskItem_ShouldHaveDefaultPriorityMedium();
        TaskItem_ShouldHaveCreatedAtByDefault();
        TaskItem_ShouldAllowSettingMainProperties();
    }

    private static void TaskItem_ShouldHaveEmptyTitleByDefault()
    {
        var task = new TaskItem();

        if (task.Title != string.Empty)
            throw new Exception("TaskItem test failed: Title should be empty by default.");
    }

    private static void TaskItem_ShouldHaveNullDescriptionByDefault()
    {
        var task = new TaskItem();

        if (task.Description != null)
            throw new Exception("TaskItem test failed: Description should be null by default.");
    }

    private static void TaskItem_ShouldHaveDefaultStatusPending()
    {
        var task = new TaskItem();

        if (task.Status != "Pending")
            throw new Exception("TaskItem test failed: default Status should be Pending.");
    }

    private static void TaskItem_ShouldHaveDefaultPriorityMedium()
    {
        var task = new TaskItem();

        if (task.Priority != "Medium")
            throw new Exception("TaskItem test failed: default Priority should be Medium.");
    }

    private static void TaskItem_ShouldHaveCreatedAtByDefault()
    {
        var beforeCreate = DateTime.UtcNow;

        var task = new TaskItem();

        var afterCreate = DateTime.UtcNow;

        if (task.CreatedAt < beforeCreate || task.CreatedAt > afterCreate)
            throw new Exception("TaskItem test failed: CreatedAt should be set to current UTC time by default.");
    }

    private static void TaskItem_ShouldAllowSettingMainProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deadline = DateTime.UtcNow.AddDays(3);
        var createdAt = DateTime.UtcNow;
        var user = new User
        {
            Id = userId,
            Email = "user@example.com"
        };

        var task = new TaskItem
        {
            Id = id,
            UserId = userId,
            Title = "Test task",
            Description = "Test description",
            DeadlineAt = deadline,
            Status = "Done",
            Priority = "High",
            CreatedAt = createdAt,
            User = user
        };

        if (task.Id != id)
            throw new Exception("TaskItem test failed: Id was set incorrectly.");

        if (task.UserId != userId)
            throw new Exception("TaskItem test failed: UserId was set incorrectly.");

        if (task.Title != "Test task")
            throw new Exception("TaskItem test failed: Title was set incorrectly.");

        if (task.Description != "Test description")
            throw new Exception("TaskItem test failed: Description was set incorrectly.");

        if (task.DeadlineAt != deadline)
            throw new Exception("TaskItem test failed: DeadlineAt was set incorrectly.");

        if (task.Status != "Done")
            throw new Exception("TaskItem test failed: Status was set incorrectly.");

        if (task.Priority != "High")
            throw new Exception("TaskItem test failed: Priority was set incorrectly.");

        if (task.CreatedAt != createdAt)
            throw new Exception("TaskItem test failed: CreatedAt was set incorrectly.");

        if (task.User != user)
            throw new Exception("TaskItem test failed: User navigation property was set incorrectly.");
    }
}
using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Services;

public static class NotificationSettingsServiceUnitTests
{
    public static async Task RunAllAsync()
    {
        await CreateDefaultSettingsAsync_ShouldCreateEmailAndInAppSettings_WhenReminderIsNull();
        await CreateDefaultSettingsAsync_ShouldCreateOnlyEmailSetting();
        await CreateDefaultSettingsAsync_ShouldCreateOnlyInAppSetting();
        await CreateDefaultSettingsAsync_ShouldCreateDefaultEmailSetting_WhenChannelsDisabled();
        await ReplaceSettingsAsync_ShouldRemoveOldSettingsAndCreateNewSettings();
        await GetSettingsAsync_ShouldReturnNull_WhenSettingsDoNotExist();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task CreateDefaultSettingsAsync_ShouldCreateEmailAndInAppSettings_WhenReminderIsNull()
    {
        await using var dbContext = CreateDbContext();
        var service = new NotificationSettingsService(dbContext);
        var taskId = Guid.NewGuid();

        await service.CreateDefaultSettingsAsync(taskId, null);

        var settings = await dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .ToListAsync();

        if (settings.Count != 2)
            throw new Exception("NotificationSettingsService test failed: null reminder should create 2 settings.");

        if (!settings.Any(x => x.Channel == "email"))
            throw new Exception("NotificationSettingsService test failed: email setting was not created.");

        if (!settings.Any(x => x.Channel == "in_app"))
            throw new Exception("NotificationSettingsService test failed: in_app setting was not created.");
    }

    private static async Task CreateDefaultSettingsAsync_ShouldCreateOnlyEmailSetting()
    {
        await using var dbContext = CreateDbContext();
        var service = new NotificationSettingsService(dbContext);
        var taskId = Guid.NewGuid();

        var reminder = new TaskReminderDto
        {
            RemindBeforeValue = 3,
            RemindBeforeUnit = "Hours",
            NotifyByEmail = true,
            NotifyInApp = false
        };

        await service.CreateDefaultSettingsAsync(taskId, reminder);

        var settings = await dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .ToListAsync();

        if (settings.Count != 1)
            throw new Exception("NotificationSettingsService test failed: only one setting should be created.");

        var setting = settings[0];

        if (setting.Channel != "email")
            throw new Exception("NotificationSettingsService test failed: created setting should have email channel.");

        if (setting.RemindBeforeValue != 3)
            throw new Exception("NotificationSettingsService test failed: RemindBeforeValue was saved incorrectly.");

        if (setting.RemindBeforeUnit != "Hours")
            throw new Exception("NotificationSettingsService test failed: RemindBeforeUnit was saved incorrectly.");
    }

    private static async Task CreateDefaultSettingsAsync_ShouldCreateOnlyInAppSetting()
    {
        await using var dbContext = CreateDbContext();
        var service = new NotificationSettingsService(dbContext);
        var taskId = Guid.NewGuid();

        var reminder = new TaskReminderDto
        {
            RemindBeforeValue = 1,
            RemindBeforeUnit = "Days",
            NotifyByEmail = false,
            NotifyInApp = true
        };

        await service.CreateDefaultSettingsAsync(taskId, reminder);

        var settings = await dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .ToListAsync();

        if (settings.Count != 1)
            throw new Exception("NotificationSettingsService test failed: only one in_app setting should be created.");

        if (settings[0].Channel != "in_app")
            throw new Exception("NotificationSettingsService test failed: created setting should have in_app channel.");
    }

    private static async Task CreateDefaultSettingsAsync_ShouldCreateDefaultEmailSetting_WhenChannelsDisabled()
    {
        await using var dbContext = CreateDbContext();
        var service = new NotificationSettingsService(dbContext);
        var taskId = Guid.NewGuid();

        var reminder = new TaskReminderDto
        {
            RemindBeforeValue = 5,
            RemindBeforeUnit = "Minutes",
            NotifyByEmail = false,
            NotifyInApp = false
        };

        await service.CreateDefaultSettingsAsync(taskId, reminder);

        var settings = await dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .ToListAsync();

        if (settings.Count != 1)
            throw new Exception("NotificationSettingsService test failed: default email setting should be created.");

        if (settings[0].Channel != "email")
            throw new Exception("NotificationSettingsService test failed: default channel should be email.");
    }

    private static async Task ReplaceSettingsAsync_ShouldRemoveOldSettingsAndCreateNewSettings()
    {
        await using var dbContext = CreateDbContext();
        var service = new NotificationSettingsService(dbContext);
        var taskId = Guid.NewGuid();

        await service.CreateDefaultSettingsAsync(taskId, new TaskReminderDto
        {
            NotifyByEmail = true,
            NotifyInApp = true
        });

        await service.ReplaceSettingsAsync(taskId, new UpdateNotificationSettingsRequest
        {
            RemindBeforeValue = 2,
            RemindBeforeUnit = "Days",
            NotifyByEmail = true,
            NotifyInApp = false
        });

        var settings = await dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .ToListAsync();

        if (settings.Count != 1)
            throw new Exception("NotificationSettingsService test failed: old settings were not replaced correctly.");

        if (settings[0].Channel != "email")
            throw new Exception("NotificationSettingsService test failed: new setting should have email channel.");

        if (settings[0].RemindBeforeValue != 2)
            throw new Exception("NotificationSettingsService test failed: new RemindBeforeValue is incorrect.");

        if (settings[0].RemindBeforeUnit != "Days")
            throw new Exception("NotificationSettingsService test failed: new RemindBeforeUnit is incorrect.");
    }

    private static async Task GetSettingsAsync_ShouldReturnNull_WhenSettingsDoNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new NotificationSettingsService(dbContext);

        var result = await service.GetSettingsAsync(Guid.NewGuid());

        if (result != null)
            throw new Exception("NotificationSettingsService test failed: GetSettingsAsync should return null when settings do not exist.");
    }
}
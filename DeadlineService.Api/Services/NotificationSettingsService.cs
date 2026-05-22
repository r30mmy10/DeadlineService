using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Services;

public class NotificationSettingsService
{
    private readonly AppDbContext _dbContext;

    public NotificationSettingsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateDefaultSettingsAsync(Guid taskId, TaskReminderDto? reminder)
    {
        reminder ??= new TaskReminderDto();

        var settings = new List<NotificationSetting>();

        if (reminder.NotifyByEmail)
        {
            settings.Add(new NotificationSetting
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                RemindBeforeValue = reminder.RemindBeforeValue,
                RemindBeforeUnit = reminder.RemindBeforeUnit,
                Channel = "email"
            });
        }

        if (reminder.NotifyInApp)
        {
            settings.Add(new NotificationSetting
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                RemindBeforeValue = reminder.RemindBeforeValue,
                RemindBeforeUnit = reminder.RemindBeforeUnit,
                Channel = "in_app"
            });
        }

        if (settings.Count == 0)
        {
            settings.Add(new NotificationSetting
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                RemindBeforeValue = reminder.RemindBeforeValue,
                RemindBeforeUnit = reminder.RemindBeforeUnit,
                Channel = "email"
            });
        }

        _dbContext.NotificationSettings.AddRange(settings);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ReplaceSettingsAsync(Guid taskId, UpdateNotificationSettingsRequest request)
    {
        var existing = await _dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .ToListAsync();

        _dbContext.NotificationSettings.RemoveRange(existing);

        await CreateDefaultSettingsAsync(taskId, new TaskReminderDto
        {
            RemindBeforeValue = request.RemindBeforeValue,
            RemindBeforeUnit = request.RemindBeforeUnit,
            NotifyByEmail = request.NotifyByEmail,
            NotifyInApp = request.NotifyInApp
        });
    }

    public async Task<object?> GetSettingsAsync(Guid taskId)
    {
        var settings = await _dbContext.NotificationSettings
            .Where(x => x.TaskId == taskId)
            .Select(x => new
            {
                x.Id,
                x.RemindBeforeValue,
                x.RemindBeforeUnit,
                x.Channel
            })
            .ToListAsync();

        if (settings.Count == 0)
            return null;

        return new
        {
            taskId,
            remindBeforeValue = settings[0].RemindBeforeValue,
            remindBeforeUnit = settings[0].RemindBeforeUnit,
            notifyByEmail = settings.Any(x => x.Channel == "email"),
            notifyInApp = settings.Any(x => x.Channel == "in_app"),
            channels = settings
        };
    }
}

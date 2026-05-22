using System.Security.Claims;
using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
using DeadlineService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly NotificationSettingsService _notificationSettings;

    public TasksController(AppDbContext dbContext, NotificationSettingsService notificationSettings)
    {
        _dbContext = dbContext;
        _notificationSettings = notificationSettings;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] string? status,
        [FromQuery] DateTime? deadlineFrom,
        [FromQuery] DateTime? deadlineTo,
        [FromQuery] bool? overdueOnly)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var query = _dbContext.Tasks.Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        if (deadlineFrom.HasValue)
            query = query.Where(x => x.DeadlineAt >= deadlineFrom.Value);

        if (deadlineTo.HasValue)
            query = query.Where(x => x.DeadlineAt <= deadlineTo.Value);

        if (overdueOnly == true)
            query = query.Where(x => x.DeadlineAt < now && x.Status != "Done");

        var tasks = await query
            .OrderBy(x => x.DeadlineAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.Title,
                x.Description,
                x.DeadlineAt,
                x.Status,
                x.Priority,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Название задачи обязательно.");

        var userId = GetCurrentUserId();

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description,
            DeadlineAt = request.DeadlineAt,
            Priority = request.Priority,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        await _notificationSettings.CreateDefaultSettingsAsync(task.Id, request.Reminder);

        var settings = await _notificationSettings.GetSettingsAsync(task.Id);

        return Ok(new { task, notificationSettings = settings });
    }

    [HttpGet("{id:guid}/notification-settings")]
    public async Task<IActionResult> GetNotificationSettings(Guid id)
    {
        var userId = GetCurrentUserId();
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (task is null)
            return NotFound("Задача не найдена.");

        var settings = await _notificationSettings.GetSettingsAsync(id);
        if (settings is null)
            return NotFound("Настройки напоминаний не найдены.");

        return Ok(settings);
    }

    [HttpPut("{id:guid}/notification-settings")]
    public async Task<IActionResult> UpdateNotificationSettings(Guid id, UpdateNotificationSettingsRequest request)
    {
        var userId = GetCurrentUserId();
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (task is null)
            return NotFound("Задача не найдена.");

        if (request.RemindBeforeValue <= 0)
            return BadRequest("Значение напоминания должно быть больше 0.");

        await _notificationSettings.ReplaceSettingsAsync(id, request);
        var settings = await _notificationSettings.GetSettingsAsync(id);
        return Ok(settings);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, UpdateTaskRequest request)
    {
        var userId = GetCurrentUserId();

        var task = await _dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (task is null)
            return NotFound("Задача не найдена.");

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.DeadlineAt = request.DeadlineAt;
        task.Status = request.Status;
        task.Priority = request.Priority;

        await _dbContext.SaveChangesAsync();

        return Ok(task);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var userId = GetCurrentUserId();

        var task = await _dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (task is null)
            return NotFound("Задача не найдена.");

        var settings = await _dbContext.NotificationSettings.Where(x => x.TaskId == id).ToListAsync();
        _dbContext.NotificationSettings.RemoveRange(settings);
        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Задача удалена." });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}

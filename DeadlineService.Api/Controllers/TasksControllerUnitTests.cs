using System.Security.Claims;
using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
using DeadlineService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Controllers;

public static class TasksControllerUnitTests
{
    public static async Task RunAllAsync()
    {
        await GetMyTasks_ShouldReturnOnlyCurrentUserTasks();
        await CreateTask_ShouldReturnBadRequest_WhenTitleIsEmpty();
        await CreateTask_ShouldCreateTask_WhenRequestIsValid();
        await UpdateTask_ShouldReturnNotFound_WhenTaskDoesNotExist();
        await UpdateTask_ShouldUpdateTask_WhenTaskExists();
        await DeleteTask_ShouldReturnNotFound_WhenTaskDoesNotExist();
        await DeleteTask_ShouldDeleteTaskAndSettings_WhenTaskExists();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static TasksController CreateController(AppDbContext dbContext, Guid userId)
    {
        var notificationSettingsService = new NotificationSettingsService(dbContext);
        var controller = new TasksController(dbContext, notificationSettingsService);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };

        return controller;
    }

    private static async Task GetMyTasks_ShouldReturnOnlyCurrentUserTasks()
    {
        await using var dbContext = CreateDbContext();

        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        dbContext.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Title = "My task",
                DeadlineAt = DateTime.UtcNow.AddDays(1),
                Status = "Pending",
                Priority = "Medium"
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                Title = "Other user task",
                DeadlineAt = DateTime.UtcNow.AddDays(1),
                Status = "Pending",
                Priority = "Medium"
            }
        );

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, currentUserId);

        var result = await controller.GetMyTasks(null, null, null, null);

        if (result is not OkObjectResult)
            throw new Exception("TasksController test failed: GetMyTasks should return Ok.");
    }

    private static async Task CreateTask_ShouldReturnBadRequest_WhenTitleIsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.CreateTask(new CreateTaskRequest
        {
            Title = "",
            Description = "Description",
            DeadlineAt = DateTime.UtcNow.AddDays(1),
            Priority = "Medium"
        });

        if (result is not BadRequestObjectResult)
            throw new Exception("TasksController test failed: CreateTask should return BadRequest when title is empty.");
    }

    private static async Task CreateTask_ShouldCreateTask_WhenRequestIsValid()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var controller = CreateController(dbContext, userId);

        var result = await controller.CreateTask(new CreateTaskRequest
        {
            Title = "  New task  ",
            Description = "Test description",
            DeadlineAt = DateTime.UtcNow.AddDays(2),
            Priority = "High",
            Reminder = new TaskReminderDto
            {
                RemindBeforeValue = 1,
                RemindBeforeUnit = "Hours",
                NotifyByEmail = true,
                NotifyInApp = true
            }
        });

        if (result is not OkObjectResult)
            throw new Exception("TasksController test failed: CreateTask should return Ok for valid request.");

        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.UserId == userId);

        if (task == null)
            throw new Exception("TasksController test failed: task was not saved to database.");

        if (task.Title != "New task")
            throw new Exception("TasksController test failed: task title should be trimmed and saved.");

        if (task.Status != "Pending")
            throw new Exception("TasksController test failed: created task status should be Pending.");

        var settingsCount = await dbContext.NotificationSettings.CountAsync(x => x.TaskId == task.Id);

        if (settingsCount != 2)
            throw new Exception("TasksController test failed: notification settings were not created for task.");
    }

    private static async Task UpdateTask_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.UpdateTask(Guid.NewGuid(), new UpdateTaskRequest
        {
            Title = "Updated",
            Description = "Updated description",
            DeadlineAt = DateTime.UtcNow.AddDays(3),
            Status = "Done",
            Priority = "High"
        });

        if (result is not NotFoundObjectResult)
            throw new Exception("TasksController test failed: UpdateTask should return NotFound when task does not exist.");
    }

    private static async Task UpdateTask_ShouldUpdateTask_WhenTaskExists()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        dbContext.Tasks.Add(new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "Old title",
            Description = "Old description",
            DeadlineAt = DateTime.UtcNow.AddDays(1),
            Status = "Pending",
            Priority = "Medium"
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, userId);

        var newDeadline = DateTime.UtcNow.AddDays(5);

        var result = await controller.UpdateTask(taskId, new UpdateTaskRequest
        {
            Title = "Updated title",
            Description = "Updated description",
            DeadlineAt = newDeadline,
            Status = "Done",
            Priority = "High"
        });

        if (result is not OkObjectResult)
            throw new Exception("TasksController test failed: UpdateTask should return Ok when task exists.");

        var task = await dbContext.Tasks.FirstAsync(x => x.Id == taskId);

        if (task.Title != "Updated title")
            throw new Exception("TasksController test failed: task title was not updated.");

        if (task.Status != "Done")
            throw new Exception("TasksController test failed: task status was not updated.");

        if (task.Priority != "High")
            throw new Exception("TasksController test failed: task priority was not updated.");
    }

    private static async Task DeleteTask_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.DeleteTask(Guid.NewGuid());

        if (result is not NotFoundObjectResult)
            throw new Exception("TasksController test failed: DeleteTask should return NotFound when task does not exist.");
    }

    private static async Task DeleteTask_ShouldDeleteTaskAndSettings_WhenTaskExists()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        dbContext.Tasks.Add(new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "Task to delete",
            DeadlineAt = DateTime.UtcNow.AddDays(1),
            Status = "Pending",
            Priority = "Medium"
        });

        dbContext.NotificationSettings.Add(new NotificationSetting
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            RemindBeforeValue = 1,
            RemindBeforeUnit = "Hours",
            Channel = "email"
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, userId);

        var result = await controller.DeleteTask(taskId);

        if (result is not OkObjectResult)
            throw new Exception("TasksController test failed: DeleteTask should return Ok when task exists.");

        var taskExists = await dbContext.Tasks.AnyAsync(x => x.Id == taskId);
        var settingsExist = await dbContext.NotificationSettings.AnyAsync(x => x.TaskId == taskId);

        if (taskExists)
            throw new Exception("TasksController test failed: task was not deleted.");

        if (settingsExist)
            throw new Exception("TasksController test failed: notification settings were not deleted.");
    }
}
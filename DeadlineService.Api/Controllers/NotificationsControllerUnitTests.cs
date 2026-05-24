using System.Security.Claims;
using DeadlineService.Api.Data;
using DeadlineService.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Controllers;

public static class NotificationsControllerUnitTests
{
    public static async Task RunAllAsync()
    {
        await GetMyNotifications_ShouldReturnOk_ForCurrentUser();
        await GetAllNotifications_ShouldReturnOk_WithAllNotifications();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static NotificationsController CreateController(AppDbContext dbContext, Guid userId, string role = "User")
    {
        var controller = new NotificationsController(dbContext);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
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

    private static async Task GetMyNotifications_ShouldReturnOk_ForCurrentUser()
    {
        await using var dbContext = CreateDbContext();

        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = currentUserId,
                Email = "current@example.com",
                PasswordHash = "hash",
                Role = "User"
            },
            new User
            {
                Id = otherUserId,
                Email = "other@example.com",
                PasswordHash = "hash",
                Role = "User"
            }
        );

        dbContext.Tasks.Add(new TaskItem
        {
            Id = taskId,
            UserId = currentUserId,
            Title = "Task",
            DeadlineAt = DateTime.UtcNow.AddDays(1),
            Status = "Pending",
            Priority = "Medium"
        });

        dbContext.Notifications.AddRange(
            new Notification
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = currentUserId,
                Message = "Current user notification",
                Channel = "email",
                DeliveryStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = otherUserId,
                Message = "Other user notification",
                Channel = "email",
                DeliveryStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            }
        );

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, currentUserId);

        var result = await controller.GetMyNotifications();

        if (result is not OkObjectResult okResult)
            throw new Exception("NotificationsController test failed: GetMyNotifications should return Ok.");

        if (okResult.Value == null)
            throw new Exception("NotificationsController test failed: GetMyNotifications should return response body.");
    }

    private static async Task GetAllNotifications_ShouldReturnOk_WithAllNotifications()
    {
        await using var dbContext = CreateDbContext();

        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "hash",
            Role = "User"
        };

        dbContext.Users.AddRange(
            new User
            {
                Id = adminId,
                Email = "admin@example.com",
                PasswordHash = "hash",
                Role = "Admin"
            },
            user
        );

        dbContext.Tasks.Add(new TaskItem
        {
            Id = taskId,
            UserId = userId,
            Title = "Task",
            DeadlineAt = DateTime.UtcNow.AddDays(1),
            Status = "Pending",
            Priority = "Medium"
        });

        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            User = user,
            Message = "Admin visible notification",
            Channel = "email",
            DeliveryStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, adminId, "Admin");

        var result = await controller.GetAllNotifications();

        if (result is not OkObjectResult okResult)
            throw new Exception("NotificationsController test failed: GetAllNotifications should return Ok.");

        if (okResult.Value == null)
            throw new Exception("NotificationsController test failed: GetAllNotifications should return response body.");
    }
}
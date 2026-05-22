using System.Security.Claims;
using DeadlineService.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public NotificationsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCurrentUserId();

        var notifications = await _dbContext.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TaskId,
                x.Message,
                x.Channel,
                x.DeliveryStatus,
                x.CreatedAt,
                x.SentAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllNotifications()
    {
        var notifications = await _dbContext.Notifications
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TaskId,
                x.UserId,
                UserEmail = x.User.Email,
                x.Message,
                x.Channel,
                x.DeliveryStatus,
                x.CreatedAt,
                x.SentAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}

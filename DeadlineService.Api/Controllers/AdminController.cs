using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _dbContext.Users
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.Role,
                x.IsBlocked
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("users/{id:guid}/block")]
    public async Task<IActionResult> UpdateUserBlock(Guid id, UpdateUserBlockRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
            return NotFound("Пользователь не найден.");

        user.IsBlocked = request.IsBlocked;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.IsBlocked,
            message = request.IsBlocked ? "Пользователь заблокирован." : "Пользователь разблокирован."
        });
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetAllTasks()
    {
        var tasks = await _dbContext.Tasks
            .Include(x => x.User)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.DeadlineAt,
                x.Status,
                x.Priority,
                x.UserId,
                UserEmail = x.User.Email
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPut("tasks/{id:guid}")]
    public async Task<IActionResult> UpdateAnyTask(Guid id, UpdateTaskRequest request)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == id);
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
}

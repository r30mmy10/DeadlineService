using System.Security.Claims;
using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
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

    public TasksController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = GetCurrentUserId();

        var tasks = await _dbContext.Tasks
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DeadlineAt)
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

        return Ok(task);
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
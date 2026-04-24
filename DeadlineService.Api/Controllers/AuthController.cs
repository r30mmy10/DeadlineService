using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
using DeadlineService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext dbContext, JwtService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email и пароль обязательны.");

        var exists = await _dbContext.Users.AnyAsync(x => x.Email == request.Email);
        if (exists)
            return BadRequest("Пользователь с таким email уже существует.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Пользователь зарегистрирован." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if (user is null)
            return Unauthorized("Неверный email или пароль.");

        if (user.IsBlocked)
            return Forbid();

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
            return Unauthorized("Неверный email или пароль.");

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Email,
                user.Role
            }
        });
    }
}
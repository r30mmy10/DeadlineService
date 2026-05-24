using DeadlineService.Api.Data;
using DeadlineService.Api.Dtos;
using DeadlineService.Api.Models;
using DeadlineService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DeadlineService.Api.Controllers;

public static class AuthControllerUnitTests
{
    public static async Task RunAllAsync()
    {
        await Register_ShouldReturnBadRequest_WhenEmailOrPasswordIsEmpty();
        await Register_ShouldReturnBadRequest_WhenUserAlreadyExists();
        await Register_ShouldCreateUser_WhenRequestIsValid();

        await Login_ShouldReturnUnauthorized_WhenUserDoesNotExist();
        await Login_ShouldReturnForbid_WhenUserIsBlocked();
        await Login_ShouldReturnUnauthorized_WhenPasswordIsWrong();
        await Login_ShouldReturnOk_WhenCredentialsAreValid();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static JwtService CreateJwtService()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "this_is_a_test_secret_key_for_jwt_token_12345" },
            { "Jwt:Issuer", "DeadlineServiceTestIssuer" },
            { "Jwt:Audience", "DeadlineServiceTestAudience" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new JwtService(configuration);
    }

    private static AuthController CreateController(AppDbContext dbContext)
    {
        return new AuthController(dbContext, CreateJwtService());
    }

    private static async Task Register_ShouldReturnBadRequest_WhenEmailOrPasswordIsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.Register(new RegisterRequest
        {
            Email = "",
            Password = ""
        });

        if (result is not BadRequestObjectResult)
            throw new Exception("AuthController test failed: Register should return BadRequest when email or password is empty.");
    }

    private static async Task Register_ShouldReturnBadRequest_WhenUserAlreadyExists()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User"
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.Register(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "123456"
        });

        if (result is not BadRequestObjectResult)
            throw new Exception("AuthController test failed: Register should return BadRequest when user already exists.");
    }

    private static async Task Register_ShouldCreateUser_WhenRequestIsValid()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.Register(new RegisterRequest
        {
            Email = "new@example.com",
            Password = "123456"
        });

        if (result is not OkObjectResult)
            throw new Exception("AuthController test failed: Register should return Ok for valid request.");

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == "new@example.com");

        if (user == null)
            throw new Exception("AuthController test failed: user was not saved to database.");

        if (user.Role != "User")
            throw new Exception("AuthController test failed: new user role should be User.");

        if (!BCrypt.Net.BCrypt.Verify("123456", user.PasswordHash))
            throw new Exception("AuthController test failed: password hash is incorrect.");
    }

    private static async Task Login_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.Login(new LoginRequest
        {
            Email = "missing@example.com",
            Password = "123456"
        });

        if (result is not UnauthorizedObjectResult)
            throw new Exception("AuthController test failed: Login should return Unauthorized when user does not exist.");
    }

    private static async Task Login_ShouldReturnForbid_WhenUserIsBlocked()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "blocked@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User",
            IsBlocked = true
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.Login(new LoginRequest
        {
            Email = "blocked@example.com",
            Password = "123456"
        });

        if (result is not ForbidResult)
            throw new Exception("AuthController test failed: Login should return Forbid when user is blocked.");
    }

    private static async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            Role = "User"
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.Login(new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrong-password"
        });

        if (result is not UnauthorizedObjectResult)
            throw new Exception("AuthController test failed: Login should return Unauthorized when password is wrong.");
    }

    private static async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User"
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.Login(new LoginRequest
        {
            Email = "test@example.com",
            Password = "123456"
        });

        if (result is not OkObjectResult okResult)
            throw new Exception("AuthController test failed: Login should return Ok when credentials are valid.");

        if (okResult.Value == null)
            throw new Exception("AuthController test failed: Login Ok result should contain response body.");
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeadlineService.Api.Models;
using Microsoft.Extensions.Configuration;

namespace DeadlineService.Api.Services;

public static class JwtServiceUnitTests
{
    public static void RunAll()
    {
        GenerateToken_ShouldReturnNotEmptyToken();
        GenerateToken_ShouldContainUserIdClaim();
        GenerateToken_ShouldContainEmailClaim();
        GenerateToken_ShouldContainRoleClaim();
        GenerateToken_ShouldContainCorrectIssuerAndAudience();
        GenerateToken_ShouldHaveExpirationTime();
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

    private static User CreateTestUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Role = "Admin"
        };
    }

    private static JwtSecurityToken GenerateAndReadToken(User user)
    {
        var jwtService = CreateJwtService();

        var tokenString = jwtService.GenerateToken(user);

        if (string.IsNullOrWhiteSpace(tokenString))
            throw new Exception("JwtService test failed: generated token should not be empty.");

        var handler = new JwtSecurityTokenHandler();

        return handler.ReadJwtToken(tokenString);
    }

    private static void GenerateToken_ShouldReturnNotEmptyToken()
    {
        var user = CreateTestUser();
        var jwtService = CreateJwtService();

        var token = jwtService.GenerateToken(user);

        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("JwtService test failed: token should not be empty.");
    }

    private static void GenerateToken_ShouldContainUserIdClaim()
    {
        var user = CreateTestUser();

        var token = GenerateAndReadToken(user);

        var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            throw new Exception("JwtService test failed: token should contain user id claim.");

        if (userIdClaim.Value != user.Id.ToString())
            throw new Exception("JwtService test failed: user id claim value is incorrect.");
    }

    private static void GenerateToken_ShouldContainEmailClaim()
    {
        var user = CreateTestUser();

        var token = GenerateAndReadToken(user);

        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        if (emailClaim == null)
            throw new Exception("JwtService test failed: token should contain email claim.");

        if (emailClaim.Value != user.Email)
            throw new Exception("JwtService test failed: email claim value is incorrect.");
    }

    private static void GenerateToken_ShouldContainRoleClaim()
    {
        var user = CreateTestUser();

        var token = GenerateAndReadToken(user);

        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        if (roleClaim == null)
            throw new Exception("JwtService test failed: token should contain role claim.");

        if (roleClaim.Value != user.Role)
            throw new Exception("JwtService test failed: role claim value is incorrect.");
    }

    private static void GenerateToken_ShouldContainCorrectIssuerAndAudience()
    {
        var user = CreateTestUser();

        var token = GenerateAndReadToken(user);

        if (token.Issuer != "DeadlineServiceTestIssuer")
            throw new Exception("JwtService test failed: token issuer is incorrect.");

        if (!token.Audiences.Contains("DeadlineServiceTestAudience"))
            throw new Exception("JwtService test failed: token audience is incorrect.");
    }

    private static void GenerateToken_ShouldHaveExpirationTime()
    {
        var user = CreateTestUser();

        var beforeCreate = DateTime.UtcNow;
        var token = GenerateAndReadToken(user);
        var afterCreate = DateTime.UtcNow;

        var expectedMinExpiration = beforeCreate.AddHours(12).AddMinutes(-1);
        var expectedMaxExpiration = afterCreate.AddHours(12).AddMinutes(1);

        if (token.ValidTo < expectedMinExpiration || token.ValidTo > expectedMaxExpiration)
            throw new Exception("JwtService test failed: token expiration time is incorrect.");
    }
}
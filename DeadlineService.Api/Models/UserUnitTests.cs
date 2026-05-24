namespace DeadlineService.Api.Models;

public static class UserUnitTests
{
    public static void RunAll()
    {
        User_ShouldHaveDefaultRoleUser();
        User_ShouldNotBeBlockedByDefault();
        User_ShouldHaveEmptyEmailByDefault();
        User_ShouldHaveEmptyPasswordHashByDefault();
        User_ShouldHaveEmptyTasksListByDefault();
        User_ShouldAllowSettingMainProperties();
    }

    private static void User_ShouldHaveDefaultRoleUser()
    {
        var user = new User();

        if (user.Role != "User")
            throw new Exception("User test failed: default Role should be User.");
    }

    private static void User_ShouldNotBeBlockedByDefault()
    {
        var user = new User();

        if (user.IsBlocked)
            throw new Exception("User test failed: IsBlocked should be false by default.");
    }

    private static void User_ShouldHaveEmptyEmailByDefault()
    {
        var user = new User();

        if (user.Email != string.Empty)
            throw new Exception("User test failed: Email should be empty by default.");
    }

    private static void User_ShouldHaveEmptyPasswordHashByDefault()
    {
        var user = new User();

        if (user.PasswordHash != string.Empty)
            throw new Exception("User test failed: PasswordHash should be empty by default.");
    }

    private static void User_ShouldHaveEmptyTasksListByDefault()
    {
        var user = new User();

        if (user.Tasks == null)
            throw new Exception("User test failed: Tasks list should not be null.");

        if (user.Tasks.Count != 0)
            throw new Exception("User test failed: Tasks list should be empty by default.");
    }

    private static void User_ShouldAllowSettingMainProperties()
    {
        var id = Guid.NewGuid();

        var user = new User
        {
            Id = id,
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            Role = "Admin",
            IsBlocked = true
        };

        if (user.Id != id)
            throw new Exception("User test failed: Id was set incorrectly.");

        if (user.Email != "test@example.com")
            throw new Exception("User test failed: Email was set incorrectly.");

        if (user.PasswordHash != "hashed_password")
            throw new Exception("User test failed: PasswordHash was set incorrectly.");

        if (user.Role != "Admin")
            throw new Exception("User test failed: Role was set incorrectly.");

        if (!user.IsBlocked)
            throw new Exception("User test failed: IsBlocked was set incorrectly.");
    }
}
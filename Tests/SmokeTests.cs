using TaskBoard.Models;

namespace TaskBoard.Tests;

public class SmokeTests
{
    [Fact]
    public void User_Can_Be_Instantiated_With_Required_Fields()
    {
        var user = new User
        {
            Username = "thomas",
            Email = "thomas@example.com",
            PasswordHash = "hash"
        };

        Assert.Equal("thomas", user.Username);
        Assert.Equal("thomas@example.com", user.Email);
        Assert.Equal(0, user.Id);
    }

    [Fact]
    public void BCrypt_Verifies_Hashed_Password()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("s3cret");

        Assert.True(BCrypt.Net.BCrypt.Verify("s3cret", hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("wrong", hash));
    }
}


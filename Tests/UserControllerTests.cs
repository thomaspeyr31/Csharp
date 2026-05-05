using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using TaskBoard.Controllers;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Tests;

public class UserControllerTests
{
    [Fact]
    public void UserDto_Json_Does_Not_Expose_PasswordHash()
    {
        var dto = new UserDto(1, "thomas", "thomas@demo.fr");

        var json = JsonSerializer.Serialize(dto);

        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void User_Maps_To_UserDto_Without_PasswordHash()
    {
        var user = new User
        {
            Id = 42,
            Username = "thomas",
            Email = "thomas@demo.fr",
            PasswordHash = "this-must-not-leak"
        };

        var dto = UserDto.From(user);
        var json = JsonSerializer.Serialize(dto);

        Assert.Equal(42, dto.Id);
        Assert.Equal("thomas", dto.Username);
        Assert.Equal("thomas@demo.fr", dto.Email);
        Assert.DoesNotContain("this-must-not-leak", json);
    }

    [Fact]
    public void UserController_Has_Authorize_Attribute()
    {
        var attr = typeof(UserController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attr);
    }

    [Fact]
    public void UserController_Does_Not_Expose_Raw_CreateUser_Endpoint()
    {
        var post = typeof(UserController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>().Any())
            .ToList();

        // Only the /register endpoint should remain (Template == "register")
        Assert.All(post, m =>
        {
            var http = m.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPostAttribute>()!;
            Assert.Equal("register", http.Template);
        });
    }

    [Fact]
    public void Register_Endpoint_Allows_Anonymous()
    {
        var register = typeof(UserController).GetMethod("Register");

        var allowAnon = register!.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.NotNull(allowAnon);
    }
}


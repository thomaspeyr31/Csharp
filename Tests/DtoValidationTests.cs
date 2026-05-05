using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Data;
using TaskBoard.Models;

namespace TaskBoard.Tests;

public class DtoValidationTests : IClassFixture<JwtTests.TaskBoardFactory>
{
    private readonly JwtTests.TaskBoardFactory _factory;

    public DtoValidationTests(JwtTests.TaskBoardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_With_Invalid_Email_Format_Returns_400()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "not-an-email", password = "secret123" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Login_With_Empty_Password_Returns_400()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "x@x.com", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateWorkspace_With_Empty_Name_Returns_400()
    {
        var client = await AuthenticateAs("dto-ws@demo.fr");

        var resp = await client.PostAsJsonAsync("/api/workspaces", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_With_Missing_WorkspaceId_Returns_400()
    {
        var client = await AuthenticateAs("dto-board@demo.fr");

        var resp = await client.PostAsJsonAsync("/api/boards", new { name = "Sprint 1" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private async Task<HttpClient> AuthenticateAs(string email)
    {
        const string password = "secret123";
        SeedUser(email, password);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var token = (await resp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private void SeedUser(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Users.Any(u => u.Email == email))
        {
            return;
        }
        db.Users.Add(new User
        {
            Username = email.Split('@')[0],
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User"
        });
        db.SaveChanges();
    }
}


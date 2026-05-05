using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Data;
using TaskBoard.Models;

namespace TaskBoard.Tests;

public class WorkspaceSharingTests : IClassFixture<JwtTests.TaskBoardFactory>
{
    private readonly JwtTests.TaskBoardFactory _factory;

    public WorkspaceSharingTests(JwtTests.TaskBoardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_With_MemberEmails_Adds_Members_Atomically()
    {
        SeedUser("share-bob@demo.fr", "secret123");
        SeedUser("share-charlie@demo.fr", "secret123");
        var alice = await AuthenticateAs("share-alice@demo.fr");

        var resp = await alice.PostAsJsonAsync("/api/workspaces", new
        {
            name = "Trio",
            memberEmails = new[] { "share-bob@demo.fr", "share-charlie@demo.fr" }
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var ws = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var wsId = ws.GetProperty("id").GetInt32();

        var members = await alice.GetFromJsonAsync<List<JsonElement>>($"/api/workspaces/{wsId}/members");
        Assert.NotNull(members);
        Assert.Equal(3, members!.Count);
        Assert.Contains(members, m => m.GetProperty("email").GetString() == "share-bob@demo.fr"
                                       && m.GetProperty("role").GetString() == "Member");
        Assert.Contains(members, m => m.GetProperty("email").GetString() == "share-alice@demo.fr"
                                       && m.GetProperty("role").GetString() == "Owner");
    }

    [Fact]
    public async Task Create_With_Unknown_Email_Returns_BadRequest_And_Does_Not_Persist()
    {
        var alice = await AuthenticateAs("share-strict-alice@demo.fr");

        var resp = await alice.PostAsJsonAsync("/api/workspaces", new
        {
            name = "Should fail",
            memberEmails = new[] { "ghost@nowhere.tld" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        // The workspace must not have been persisted (atomic rollback).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(db.Workspaces.Any(w => w.Name == "Should fail"));
    }

    [Fact]
    public async Task Owner_Can_Invite_After_Creation_And_Member_Sees_Workspace()
    {
        SeedUser("share-late-bob@demo.fr", "secret123");
        var alice = await AuthenticateAs("share-late-alice@demo.fr");
        var wsResp = await alice.PostAsJsonAsync("/api/workspaces", new { name = "Just-me-then" });
        var wsId = (await wsResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var invite = await alice.PostAsJsonAsync($"/api/workspaces/{wsId}/members",
            new { email = "share-late-bob@demo.fr" });
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);

        var bob = await AuthenticateAs("share-late-bob@demo.fr");
        var bobsList = await bob.GetFromJsonAsync<List<JsonElement>>("/api/workspaces");
        Assert.Contains(bobsList!, w => w.GetProperty("id").GetInt32() == wsId);
    }

    [Fact]
    public async Task Non_Owner_Cannot_Invite()
    {
        var bobId = SeedUser("share-perm-bob@demo.fr", "secret123");
        var alice = await AuthenticateAs("share-perm-alice@demo.fr");
        var wsResp = await alice.PostAsJsonAsync("/api/workspaces", new
        {
            name = "ws",
            memberEmails = new[] { "share-perm-bob@demo.fr" }
        });
        var wsId = (await wsResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var bob = await AuthenticateAs("share-perm-bob@demo.fr");
        SeedUser("share-perm-charlie@demo.fr", "secret123");

        var invite = await bob.PostAsJsonAsync($"/api/workspaces/{wsId}/members",
            new { email = "share-perm-charlie@demo.fr" });

        Assert.Equal(HttpStatusCode.Forbidden, invite.StatusCode);
    }

    [Fact]
    public async Task Owner_Can_Remove_A_Member()
    {
        SeedUser("share-kick-bob@demo.fr", "secret123");
        var alice = await AuthenticateAs("share-kick-alice@demo.fr");
        var wsResp = await alice.PostAsJsonAsync("/api/workspaces", new
        {
            name = "ws",
            memberEmails = new[] { "share-kick-bob@demo.fr" }
        });
        var wsId = (await wsResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var bobId = await GetUserIdByEmail("share-kick-bob@demo.fr");

        var remove = await alice.DeleteAsync($"/api/workspaces/{wsId}/members/{bobId}");
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);

        var bob = await AuthenticateAs("share-kick-bob@demo.fr");
        var bobsList = await bob.GetFromJsonAsync<List<JsonElement>>("/api/workspaces");
        Assert.DoesNotContain(bobsList!, w => w.GetProperty("id").GetInt32() == wsId);
    }

    [Fact]
    public async Task Workspace_List_Carries_Caller_Role()
    {
        SeedUser("role-bob@demo.fr", "secret123");
        var alice = await AuthenticateAs("role-alice@demo.fr");
        var resp = await alice.PostAsJsonAsync("/api/workspaces", new
        {
            name = "shared",
            memberEmails = new[] { "role-bob@demo.fr" }
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var aliceList = await alice.GetFromJsonAsync<List<JsonElement>>("/api/workspaces");
        var aliceShared = aliceList!.First(w => w.GetProperty("name").GetString() == "shared");
        Assert.Equal("Owner", aliceShared.GetProperty("myRole").GetString());

        var bob = await AuthenticateAs("role-bob@demo.fr");
        var bobList = await bob.GetFromJsonAsync<List<JsonElement>>("/api/workspaces");
        var bobShared = bobList!.First(w => w.GetProperty("name").GetString() == "shared");
        Assert.Equal("Member", bobShared.GetProperty("myRole").GetString());
    }

    [Fact]
    public async Task Owner_Cannot_Be_Removed()
    {
        var alice = await AuthenticateAs("share-self-alice@demo.fr");
        var wsResp = await alice.PostAsJsonAsync("/api/workspaces", new { name = "Solo" });
        var wsId = (await wsResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var aliceId = await GetUserIdByEmail("share-self-alice@demo.fr");

        var remove = await alice.DeleteAsync($"/api/workspaces/{wsId}/members/{aliceId}");
        Assert.Equal(HttpStatusCode.BadRequest, remove.StatusCode);
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

    private int SeedUser(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = db.Users.FirstOrDefault(u => u.Email == email);
        if (existing != null) return existing.Id;
        var user = new User
        {
            Username = email.Split('@')[0],
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User"
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    private Task<int> GetUserIdByEmail(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return Task.FromResult(db.Users.First(u => u.Email == email).Id);
    }
}


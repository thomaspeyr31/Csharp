using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Data;
using TaskBoard.Models;

namespace TaskBoard.Tests;

public class CardRichFieldsTests : IClassFixture<JwtTests.TaskBoardFactory>
{
    private readonly JwtTests.TaskBoardFactory _factory;

    public CardRichFieldsTests(JwtTests.TaskBoardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Card_Can_Carry_DueDate()
    {
        var (client, listId) = await SetUpKanbanFor("rich-due@demo.fr");
        var due = DateTime.UtcNow.AddDays(3);

        var resp = await client.PostAsJsonAsync("/api/cards", new
        {
            title = "with deadline",
            description = "x",
            position = 0,
            dueDate = due,
            listId
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var card = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(card.GetProperty("dueDate").ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public async Task Label_Can_Be_Created_And_Assigned_To_Card()
    {
        var (client, listId, workspaceId, _) = await SetUpKanbanWithIdsFor("rich-label@demo.fr");
        var card = await CreateCard(client, listId);

        var labelResp = await client.PostAsJsonAsync("/api/labels", new
        {
            name = "Urgent",
            color = "#ff0000",
            workspaceId
        });
        Assert.Equal(HttpStatusCode.OK, labelResp.StatusCode);
        var labelId = (await labelResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var assign = await client.PostAsJsonAsync($"/api/cards/{card}/labels", new { labelId });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/cards/{card}");
        var labels = detail.GetProperty("labels").EnumerateArray().ToList();
        Assert.Single(labels);
        Assert.Equal("Urgent", labels[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Comment_Can_Be_Posted_Edited_And_Deleted_By_Author()
    {
        var (client, listId, _, _) = await SetUpKanbanWithIdsFor("rich-comment@demo.fr");
        var card = await CreateCard(client, listId);

        var post = await client.PostAsJsonAsync($"/api/cards/{card}/comments", new { content = "first" });
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var commentId = (await post.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var put = await client.PutAsJsonAsync($"/api/comments/{commentId}", new { content = "edited" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("edited", (await put.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("content").GetString());

        var del = await client.DeleteAsync($"/api/comments/{commentId}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
    }

    [Fact]
    public async Task Comment_Of_Other_User_Cannot_Be_Edited()
    {
        var (alice, listId, workspaceId, _) = await SetUpKanbanWithIdsFor("alice-c@demo.fr");
        var card = await CreateCard(alice, listId);
        var post = await alice.PostAsJsonAsync($"/api/cards/{card}/comments", new { content = "alice's" });
        var commentId = (await post.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // Add Bob as a workspace member so he can SEE the card, but he is not the comment author.
        var bobId = SeedUser("bob-c@demo.fr", "secret123");
        AddMembership(workspaceId, bobId, MemberRoles.Member);
        var bob = await AuthenticateAs("bob-c@demo.fr");

        var put = await bob.PutAsJsonAsync($"/api/comments/{commentId}", new { content = "bob hijack" });
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
    }

    private async Task<(HttpClient client, int listId)> SetUpKanbanFor(string email)
    {
        var (client, listId, _, _) = await SetUpKanbanWithIdsFor(email);
        return (client, listId);
    }

    private async Task<(HttpClient client, int listId, int workspaceId, int boardId)> SetUpKanbanWithIdsFor(string email)
    {
        var client = await AuthenticateAs(email);

        var ws = await client.PostAsJsonAsync("/api/workspaces", new { name = $"ws-{email}" });
        var wsId = (await ws.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var board = await client.PostAsJsonAsync("/api/boards", new { name = "B", workspaceId = wsId });
        var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var list = await client.PostAsJsonAsync("/api/lists", new { name = "L", position = 0, boardId });
        var listId = (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        return (client, listId, wsId, boardId);
    }

    private static async Task<int> CreateCard(HttpClient client, int listId)
    {
        var resp = await client.PostAsJsonAsync("/api/cards", new
        {
            title = "C",
            description = (string?)null,
            position = 0,
            listId
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
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

    private void AddMembership(int workspaceId, int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.WorkspaceMembers.Any(m => m.WorkspaceId == workspaceId && m.UserId == userId))
        {
            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                Role = role
            });
            db.SaveChanges();
        }
    }
}


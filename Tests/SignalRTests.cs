using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Data;
using TaskBoard.Models;

namespace TaskBoard.Tests;

public class SignalRTests : IClassFixture<JwtTests.TaskBoardFactory>
{
    private readonly JwtTests.TaskBoardFactory _factory;

    public SignalRTests(JwtTests.TaskBoardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CardCreated_Is_Broadcast_To_Other_Connected_Clients()
    {
        // Two clients (alice = author, bob = listener), both members of the same workspace.
        var alice = await AuthenticateAs("sigr-alice@demo.fr");
        var (workspaceId, boardId, listId) = await CreateWorkspaceBoardList(alice);

        var bobId = SeedUser("sigr-bob@demo.fr", "secret123");
        AddMembership(workspaceId, bobId, MemberRoles.Member);
        var (bobToken, _) = await Login("sigr-bob@demo.fr", "secret123");

        // Bob opens a hub connection and joins the board group.
        await using var bobHub = BuildHubConnection(bobToken);
        var received = new TaskCompletionSource<JsonElement>();
        bobHub.On<JsonElement>("CardCreated", payload => received.TrySetResult(payload));

        await bobHub.StartAsync();
        await bobHub.InvokeAsync("JoinBoard", boardId);

        // Alice creates a card via the REST API.
        var resp = await alice.PostAsJsonAsync("/api/cards", new
        {
            title = "RT card",
            description = (string?)null,
            position = 0,
            listId
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);

        // Bob's listener should fire within a reasonable timeout.
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.True(completed == received.Task, "Bob did not receive the CardCreated event in time.");

        var payload = await received.Task;
        Assert.Equal("RT card", payload.GetProperty("title").GetString());
    }

    private HubConnection BuildHubConnection(string accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(_factory.Server.BaseAddress + "hubs/board", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .Build();
    }

    private async Task<HttpClient> AuthenticateAs(string email)
    {
        const string password = "secret123";
        SeedUser(email, password);
        var (token, _) = await Login(email, password);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(string Access, string Refresh)> Login(string email, string password)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("accessToken").GetString()!, json.GetProperty("refreshToken").GetString()!);
    }

    private async Task<(int wsId, int boardId, int listId)> CreateWorkspaceBoardList(HttpClient client)
    {
        var ws = await client.PostAsJsonAsync("/api/workspaces", new { name = $"ws-{Guid.NewGuid():N}" });
        var wsId = (await ws.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var b = await client.PostAsJsonAsync("/api/boards", new { name = "B", workspaceId = wsId });
        var boardId = (await b.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var l = await client.PostAsJsonAsync("/api/lists", new { name = "L", position = 0, boardId });
        var listId = (await l.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (wsId, boardId, listId);
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


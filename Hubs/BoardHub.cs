using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskBoard.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public Task JoinBoard(int boardId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));

    public Task LeaveBoard(int boardId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));

    public static string GroupName(int boardId) => $"board-{boardId}";
}

public static class BoardEvents
{
    public const string CardCreated = "CardCreated";
    public const string CardUpdated = "CardUpdated";
    public const string CardDeleted = "CardDeleted";
    public const string ListCreated = "ListCreated";
    public const string ListDeleted = "ListDeleted";
    public const string CommentCreated = "CommentCreated";
    public const string CommentUpdated = "CommentUpdated";
    public const string CommentDeleted = "CommentDeleted";
}


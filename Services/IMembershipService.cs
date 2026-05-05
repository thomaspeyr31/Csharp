namespace TaskBoard.Services;

public interface IMembershipService
{
    Task<bool> IsMemberOfWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default);

    Task<bool> IsMemberOfBoardAsync(int userId, int boardId, CancellationToken ct = default);

    Task<bool> IsMemberOfListAsync(int userId, int listId, CancellationToken ct = default);
}

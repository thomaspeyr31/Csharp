using Microsoft.EntityFrameworkCore;
using TaskBoard.Data;

namespace TaskBoard.Services;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _context;

    public MembershipService(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsMemberOfWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default) =>
        _context.WorkspaceMembers.AnyAsync(m => m.UserId == userId && m.WorkspaceId == workspaceId, ct);

    public Task<bool> IsMemberOfBoardAsync(int userId, int boardId, CancellationToken ct = default) =>
        _context.Boards
            .Where(b => b.Id == boardId)
            .AnyAsync(b => _context.WorkspaceMembers
                .Any(m => m.WorkspaceId == b.WorkspaceId && m.UserId == userId), ct);

    public Task<bool> IsMemberOfListAsync(int userId, int listId, CancellationToken ct = default) =>
        _context.Lists
            .Where(l => l.Id == listId)
            .Join(_context.Boards, l => l.BoardId, b => b.Id, (l, b) => b.WorkspaceId)
            .AnyAsync(workspaceId => _context.WorkspaceMembers
                .Any(m => m.WorkspaceId == workspaceId && m.UserId == userId), ct);
}

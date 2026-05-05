using Microsoft.EntityFrameworkCore;
using TaskBoard.Common;
using TaskBoard.Data;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class BoardService : IBoardService
{
    private readonly AppDbContext _context;
    private readonly IMembershipService _membership;

    public BoardService(AppDbContext context, IMembershipService membership)
    {
        _context = context;
        _membership = membership;
    }

    public async Task<List<BoardDto>> GetForUserAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Boards
            .Where(b => _context.WorkspaceMembers
                .Any(m => m.WorkspaceId == b.WorkspaceId && m.UserId == userId))
            .Select(b => new BoardDto(b.Id, b.Name, b.WorkspaceId))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<BoardDto>> CreateAsync(int userId, CreateBoardDto dto, CancellationToken ct = default)
    {
        var workspaceId = dto.WorkspaceId!.Value;

        if (!await _membership.IsMemberOfWorkspaceAsync(userId, workspaceId, ct))
        {
            return ServiceResult<BoardDto>.Forbidden("Not a member of the target workspace.");
        }

        var board = new Board { Name = dto.Name, WorkspaceId = workspaceId };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync(ct);

        return ServiceResult<BoardDto>.Ok(BoardDto.From(board));
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int boardId, CancellationToken ct = default)
    {
        var board = await _context.Boards.FindAsync(new object[] { boardId }, ct);
        if (board is null)
        {
            return ServiceResult.NotFound();
        }

        if (!await _membership.IsMemberOfWorkspaceAsync(userId, board.WorkspaceId, ct))
        {
            return ServiceResult.Forbidden();
        }

        _context.Boards.Remove(board);
        await _context.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }
}


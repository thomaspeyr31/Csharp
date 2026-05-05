using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Common;
using TaskBoard.Data;
using TaskBoard.Hubs;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class ListService : IListService
{
    private readonly AppDbContext _context;
    private readonly IMembershipService _membership;
    private readonly IHubContext<BoardHub> _hub;

    public ListService(AppDbContext context, IMembershipService membership, IHubContext<BoardHub> hub)
    {
        _context = context;
        _membership = membership;
        _hub = hub;
    }

    public async Task<ServiceResult<List<BoardListDto>>> GetByBoardAsync(int userId, int boardId, CancellationToken ct = default)
    {
        if (!await _membership.IsMemberOfBoardAsync(userId, boardId, ct))
        {
            return ServiceResult<List<BoardListDto>>.Forbidden();
        }

        var lists = await _context.Lists
            .Where(l => l.BoardId == boardId)
            .OrderBy(l => l.Position)
            .Select(l => new BoardListDto(l.Id, l.Name, l.Position, l.BoardId))
            .ToListAsync(ct);

        return ServiceResult<List<BoardListDto>>.Ok(lists);
    }

    public async Task<ServiceResult<BoardListDto>> CreateAsync(int userId, CreateBoardListDto dto, CancellationToken ct = default)
    {
        var boardId = dto.BoardId!.Value;

        if (!await _membership.IsMemberOfBoardAsync(userId, boardId, ct))
        {
            return ServiceResult<BoardListDto>.Forbidden();
        }

        var list = new BoardList { Name = dto.Name, Position = dto.Position, BoardId = boardId };
        _context.Lists.Add(list);
        await _context.SaveChangesAsync(ct);

        var listDto = BoardListDto.From(list);
        await _hub.Clients.Group(BoardHub.GroupName(boardId))
            .SendAsync(BoardEvents.ListCreated, listDto, ct);

        return ServiceResult<BoardListDto>.Ok(listDto);
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int listId, CancellationToken ct = default)
    {
        var list = await _context.Lists.FindAsync(new object[] { listId }, ct);
        if (list is null)
        {
            return ServiceResult.NotFound();
        }

        if (!await _membership.IsMemberOfBoardAsync(userId, list.BoardId, ct))
        {
            return ServiceResult.Forbidden();
        }

        var boardId = list.BoardId;
        _context.Lists.Remove(list);
        await _context.SaveChangesAsync(ct);

        await _hub.Clients.Group(BoardHub.GroupName(boardId))
            .SendAsync(BoardEvents.ListDeleted, new { id = listId }, ct);

        return ServiceResult.Ok();
    }
}


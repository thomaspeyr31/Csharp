using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Common;
using TaskBoard.Data;
using TaskBoard.Hubs;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class CommentService : ICommentService
{
    private readonly AppDbContext _context;
    private readonly IMembershipService _membership;
    private readonly IHubContext<BoardHub> _hub;
    private readonly IBoardLookup _boards;

    public CommentService(AppDbContext context, IMembershipService membership, IHubContext<BoardHub> hub, IBoardLookup boards)
    {
        _context = context;
        _membership = membership;
        _hub = hub;
        _boards = boards;
    }

    public async Task<ServiceResult<List<CommentDto>>> GetForCardAsync(int userId, int cardId, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult<List<CommentDto>>.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult<List<CommentDto>>.Forbidden();
        }

        var comments = await _context.Comments
            .Include(c => c.Author)
            .Where(c => c.CardId == cardId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return ServiceResult<List<CommentDto>>.Ok(comments.Select(CommentDto.From).ToList());
    }

    public async Task<ServiceResult<CommentDto>> CreateAsync(int userId, int cardId, CreateCommentDto dto, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult<CommentDto>.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult<CommentDto>.Forbidden();
        }

        var comment = new Comment
        {
            CardId = cardId,
            AuthorId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(ct);

        comment.Author = await _context.Users.FindAsync(new object[] { userId }, ct) ?? null!;

        var dtoOut = CommentDto.From(comment);
        await BroadcastAsync(card.ListId, BoardEvents.CommentCreated, dtoOut, ct);

        return ServiceResult<CommentDto>.Ok(dtoOut);
    }

    public async Task<ServiceResult<CommentDto>> UpdateAsync(int userId, int commentId, UpdateCommentDto dto, CancellationToken ct = default)
    {
        var comment = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Card)
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);

        if (comment is null)
        {
            return ServiceResult<CommentDto>.NotFound();
        }

        if (comment.AuthorId != userId)
        {
            return ServiceResult<CommentDto>.Forbidden("Only the author may edit this comment.");
        }

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var dtoOut = CommentDto.From(comment);
        await BroadcastAsync(comment.Card.ListId, BoardEvents.CommentUpdated, dtoOut, ct);

        return ServiceResult<CommentDto>.Ok(dtoOut);
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int commentId, CancellationToken ct = default)
    {
        var comment = await _context.Comments
            .Include(c => c.Card)
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null)
        {
            return ServiceResult.NotFound();
        }

        if (comment.AuthorId != userId)
        {
            return ServiceResult.Forbidden("Only the author may delete this comment.");
        }

        var listId = comment.Card.ListId;
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync(ct);

        await BroadcastAsync(listId, BoardEvents.CommentDeleted, new { id = commentId, cardId = comment.CardId }, ct);

        return ServiceResult.Ok();
    }

    private async Task BroadcastAsync(int listId, string @event, object payload, CancellationToken ct)
    {
        var boardId = await _boards.BoardIdForListAsync(listId, ct);
        if (boardId is null) return;
        await _hub.Clients.Group(BoardHub.GroupName(boardId.Value)).SendAsync(@event, payload, ct);
    }
}


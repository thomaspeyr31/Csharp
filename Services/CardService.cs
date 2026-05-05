using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Common;
using TaskBoard.Data;
using TaskBoard.Hubs;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class CardService : ICardService
{
    private readonly AppDbContext _context;
    private readonly IMembershipService _membership;
    private readonly IHubContext<BoardHub> _hub;
    private readonly IBoardLookup _boards;

    public CardService(AppDbContext context, IMembershipService membership, IHubContext<BoardHub> hub, IBoardLookup boards)
    {
        _context = context;
        _membership = membership;
        _hub = hub;
        _boards = boards;
    }

    private Task BroadcastAsync(int? boardId, string @event, object payload, CancellationToken ct)
    {
        if (boardId is null) return Task.CompletedTask;
        return _hub.Clients.Group(BoardHub.GroupName(boardId.Value)).SendAsync(@event, payload, ct);
    }

    public async Task<ServiceResult<List<CardDto>>> GetByListAsync(int userId, int listId, CancellationToken ct = default)
    {
        if (!await _membership.IsMemberOfListAsync(userId, listId, ct))
        {
            return ServiceResult<List<CardDto>>.Forbidden();
        }

        var cards = await _context.Cards
            .Where(c => c.ListId == listId)
            .OrderBy(c => c.Position)
            .Select(c => new CardDto(c.Id, c.Title, c.Description, c.Position, c.DueDate, c.ListId))
            .ToListAsync(ct);

        return ServiceResult<List<CardDto>>.Ok(cards);
    }

    public async Task<ServiceResult<CardDetailDto>> GetWithDetailsAsync(int userId, int cardId, CancellationToken ct = default)
    {
        var card = await _context.Cards
            .Include(c => c.Labels).ThenInclude(cl => cl.Label)
            .Include(c => c.Members).ThenInclude(cm => cm.User)
            .Include(c => c.Comments).ThenInclude(co => co.Author)
            .FirstOrDefaultAsync(c => c.Id == cardId, ct);

        if (card is null)
        {
            return ServiceResult<CardDetailDto>.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult<CardDetailDto>.Forbidden();
        }

        var detail = new CardDetailDto(
            card.Id, card.Title, card.Description, card.Position, card.DueDate, card.ListId,
            card.Labels.Select(cl => LabelDto.From(cl.Label)).ToList(),
            card.Members.Select(cm => UserDto.From(cm.User)).ToList(),
            card.Comments.OrderBy(c => c.CreatedAt).Select(CommentDto.From).ToList());

        return ServiceResult<CardDetailDto>.Ok(detail);
    }

    public async Task<ServiceResult<CardDto>> CreateAsync(int userId, CreateCardDto dto, CancellationToken ct = default)
    {
        var listId = dto.ListId!.Value;

        if (!await _membership.IsMemberOfListAsync(userId, listId, ct))
        {
            return ServiceResult<CardDto>.Forbidden();
        }

        var card = new Card
        {
            Title = dto.Title,
            Description = dto.Description,
            Position = dto.Position,
            DueDate = dto.DueDate,
            ListId = listId
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync(ct);

        var boardId = await _boards.BoardIdForListAsync(listId, ct);
        await BroadcastAsync(boardId, BoardEvents.CardCreated, CardDto.From(card), ct);

        return ServiceResult<CardDto>.Ok(CardDto.From(card));
    }

    public async Task<ServiceResult<CardDto>> UpdateAsync(int userId, int cardId, UpdateCardDto dto, CancellationToken ct = default)
    {
        var newListId = dto.ListId!.Value;

        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult<CardDto>.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult<CardDto>.Forbidden();
        }

        if (newListId != card.ListId
            && !await _membership.IsMemberOfListAsync(userId, newListId, ct))
        {
            return ServiceResult<CardDto>.Forbidden();
        }

        card.Title = dto.Title;
        card.Description = dto.Description;
        card.Position = dto.Position;
        card.DueDate = dto.DueDate;
        card.ListId = newListId;
        await _context.SaveChangesAsync(ct);

        var boardId = await _boards.BoardIdForListAsync(newListId, ct);
        await BroadcastAsync(boardId, BoardEvents.CardUpdated, CardDto.From(card), ct);

        return ServiceResult<CardDto>.Ok(CardDto.From(card));
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int cardId, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult.Forbidden();
        }

        var boardId = await _boards.BoardIdForListAsync(card.ListId, ct);
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync(ct);

        await BroadcastAsync(boardId, BoardEvents.CardDeleted, new { id = cardId }, ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddLabelAsync(int userId, int cardId, int labelId, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult.NotFound("Card not found.");
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult.Forbidden();
        }

        var label = await _context.Labels.FindAsync(new object[] { labelId }, ct);
        if (label is null)
        {
            return ServiceResult.NotFound("Label not found.");
        }

        // Label and Card must live in the same workspace.
        var cardWorkspaceId = await _context.Boards
            .Where(b => b.Id == _context.Lists.Where(l => l.Id == card.ListId).Select(l => l.BoardId).First())
            .Select(b => b.WorkspaceId)
            .FirstAsync(ct);

        if (label.WorkspaceId != cardWorkspaceId)
        {
            return ServiceResult.BadRequest("Label belongs to a different workspace.");
        }

        var exists = await _context.CardLabels
            .AnyAsync(cl => cl.CardId == cardId && cl.LabelId == labelId, ct);
        if (exists)
        {
            return ServiceResult.Ok();
        }

        _context.CardLabels.Add(new CardLabel { CardId = cardId, LabelId = labelId });
        await _context.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveLabelAsync(int userId, int cardId, int labelId, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult.Forbidden();
        }

        var link = await _context.CardLabels
            .FirstOrDefaultAsync(cl => cl.CardId == cardId && cl.LabelId == labelId, ct);
        if (link is null)
        {
            return ServiceResult.NotFound();
        }

        _context.CardLabels.Remove(link);
        await _context.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddMemberAsync(int userId, int cardId, int memberUserId, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult.NotFound("Card not found.");
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult.Forbidden();
        }

        // The assignee must be a member of the workspace owning the card.
        var workspaceId = await _context.Lists
            .Where(l => l.Id == card.ListId)
            .Join(_context.Boards, l => l.BoardId, b => b.Id, (l, b) => b.WorkspaceId)
            .FirstAsync(ct);

        if (!await _membership.IsMemberOfWorkspaceAsync(memberUserId, workspaceId, ct))
        {
            return ServiceResult.BadRequest("The user is not a member of this workspace.");
        }

        var exists = await _context.CardMembers
            .AnyAsync(cm => cm.CardId == cardId && cm.UserId == memberUserId, ct);
        if (exists)
        {
            return ServiceResult.Ok();
        }

        _context.CardMembers.Add(new CardMember { CardId = cardId, UserId = memberUserId });
        await _context.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveMemberAsync(int userId, int cardId, int memberUserId, CancellationToken ct = default)
    {
        var card = await _context.Cards.FindAsync(new object[] { cardId }, ct);
        if (card is null)
        {
            return ServiceResult.NotFound();
        }

        if (!await _membership.IsMemberOfListAsync(userId, card.ListId, ct))
        {
            return ServiceResult.Forbidden();
        }

        var link = await _context.CardMembers
            .FirstOrDefaultAsync(cm => cm.CardId == cardId && cm.UserId == memberUserId, ct);
        if (link is null)
        {
            return ServiceResult.NotFound();
        }

        _context.CardMembers.Remove(link);
        await _context.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}


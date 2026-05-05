using Microsoft.EntityFrameworkCore;
using TaskBoard.Data;

namespace TaskBoard.Services;

public class BoardLookup : IBoardLookup
{
    private readonly AppDbContext _context;

    public BoardLookup(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int?> BoardIdForListAsync(int listId, CancellationToken ct = default)
    {
        var boardId = await _context.Lists
            .Where(l => l.Id == listId)
            .Select(l => (int?)l.BoardId)
            .FirstOrDefaultAsync(ct);
        return boardId;
    }

    public async Task<int?> BoardIdForCardAsync(int cardId, CancellationToken ct = default)
    {
        var boardId = await _context.Cards
            .Where(c => c.Id == cardId)
            .Join(_context.Lists, c => c.ListId, l => l.Id, (c, l) => (int?)l.BoardId)
            .FirstOrDefaultAsync(ct);
        return boardId;
    }
}


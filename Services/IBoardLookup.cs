namespace TaskBoard.Services;

public interface IBoardLookup
{
    Task<int?> BoardIdForListAsync(int listId, CancellationToken ct = default);

    Task<int?> BoardIdForCardAsync(int cardId, CancellationToken ct = default);
}


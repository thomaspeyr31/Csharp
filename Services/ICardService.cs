using TaskBoard.Common;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface ICardService
{
    Task<ServiceResult<List<CardDto>>> GetByListAsync(int userId, int listId, CancellationToken ct = default);

    Task<ServiceResult<CardDetailDto>> GetWithDetailsAsync(int userId, int cardId, CancellationToken ct = default);

    Task<ServiceResult<CardDto>> CreateAsync(int userId, CreateCardDto dto, CancellationToken ct = default);

    Task<ServiceResult<CardDto>> UpdateAsync(int userId, int cardId, UpdateCardDto dto, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int userId, int cardId, CancellationToken ct = default);

    Task<ServiceResult> AddLabelAsync(int userId, int cardId, int labelId, CancellationToken ct = default);

    Task<ServiceResult> RemoveLabelAsync(int userId, int cardId, int labelId, CancellationToken ct = default);

    Task<ServiceResult> AddMemberAsync(int userId, int cardId, int memberUserId, CancellationToken ct = default);

    Task<ServiceResult> RemoveMemberAsync(int userId, int cardId, int memberUserId, CancellationToken ct = default);
}


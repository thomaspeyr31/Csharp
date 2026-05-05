using TaskBoard.Common;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface IBoardService
{
    Task<List<BoardDto>> GetForUserAsync(int userId, CancellationToken ct = default);

    Task<ServiceResult<BoardDto>> CreateAsync(int userId, CreateBoardDto dto, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int userId, int boardId, CancellationToken ct = default);
}


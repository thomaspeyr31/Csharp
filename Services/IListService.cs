using TaskBoard.Common;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface IListService
{
    Task<ServiceResult<List<BoardListDto>>> GetByBoardAsync(int userId, int boardId, CancellationToken ct = default);

    Task<ServiceResult<BoardListDto>> CreateAsync(int userId, CreateBoardListDto dto, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int userId, int listId, CancellationToken ct = default);
}


using TaskBoard.Common;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface ICommentService
{
    Task<ServiceResult<List<CommentDto>>> GetForCardAsync(int userId, int cardId, CancellationToken ct = default);

    Task<ServiceResult<CommentDto>> CreateAsync(int userId, int cardId, CreateCommentDto dto, CancellationToken ct = default);

    Task<ServiceResult<CommentDto>> UpdateAsync(int userId, int commentId, UpdateCommentDto dto, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int userId, int commentId, CancellationToken ct = default);
}


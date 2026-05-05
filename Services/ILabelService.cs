using TaskBoard.Common;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface ILabelService
{
    Task<ServiceResult<List<LabelDto>>> GetForWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default);

    Task<ServiceResult<LabelDto>> CreateAsync(int userId, CreateLabelDto dto, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int userId, int labelId, CancellationToken ct = default);
}


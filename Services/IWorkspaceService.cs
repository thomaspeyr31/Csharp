using TaskBoard.Common;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface IWorkspaceService
{
    Task<List<WorkspaceDto>> GetForUserAsync(int userId, CancellationToken ct = default);

    Task<ServiceResult<WorkspaceDto>> CreateAsync(int userId, CreateWorkspaceDto dto, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int userId, int workspaceId, CancellationToken ct = default);

    Task<ServiceResult<List<WorkspaceMemberDto>>> GetMembersAsync(int userId, int workspaceId, CancellationToken ct = default);

    Task<ServiceResult<WorkspaceMemberDto>> AddMemberAsync(int userId, int workspaceId, string email, CancellationToken ct = default);

    Task<ServiceResult> RemoveMemberAsync(int userId, int workspaceId, int memberUserId, CancellationToken ct = default);
}


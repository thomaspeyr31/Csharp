using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Models.Dto;

public record CreateWorkspaceDto(
    [Required][StringLength(100, MinimumLength = 1)] string Name,
    List<string>? MemberEmails = null);

public record WorkspaceDto(int Id, string Name, int OwnerId, string MyRole)
{
    public static WorkspaceDto From(Workspace w, string myRole) => new(w.Id, w.Name, w.OwnerId, myRole);
}

public record WorkspaceMemberDto(int UserId, string Username, string Email, string Role);

public record InviteMemberDto(
    [Required][EmailAddress] string Email);


namespace TaskBoard.Models;

public class WorkspaceMember
{
    public int WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Role { get; set; } = MemberRoles.Member;
}

public static class MemberRoles
{
    public const string Owner = "Owner";
    public const string Member = "Member";
}

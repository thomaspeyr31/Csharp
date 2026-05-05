using Microsoft.EntityFrameworkCore;
using TaskBoard.Common;
using TaskBoard.Data;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly AppDbContext _context;

    public WorkspaceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkspaceDto>> GetForUserAsync(int userId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Join(_context.Workspaces, m => m.WorkspaceId, w => w.Id,
                (m, w) => new WorkspaceDto(w.Id, w.Name, w.OwnerId, m.Role))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<WorkspaceDto>> CreateAsync(int userId, CreateWorkspaceDto dto, CancellationToken ct = default)
    {
        var emails = (dto.MemberEmails ?? new List<string>())
            .Select(e => e.Trim())
            .Where(e => e.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Resolve invited emails to existing users; reject the whole call if any is unknown.
        var invitedUsers = await _context.Users
            .Where(u => emails.Contains(u.Email))
            .ToListAsync(ct);

        if (invitedUsers.Count != emails.Count)
        {
            var found = invitedUsers.Select(u => u.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = emails.Where(e => !found.Contains(e)).ToList();
            return ServiceResult<WorkspaceDto>.BadRequest(
                $"Unknown email(s): {string.Join(", ", missing)}");
        }

        var workspace = new Workspace { Name = dto.Name, OwnerId = userId };
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync(ct);

        _context.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = MemberRoles.Owner
        });

        foreach (var u in invitedUsers.Where(u => u.Id != userId))
        {
            _context.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = u.Id,
                Role = MemberRoles.Member
            });
        }

        await _context.SaveChangesAsync(ct);

        return ServiceResult<WorkspaceDto>.Ok(WorkspaceDto.From(workspace, MemberRoles.Owner));
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int workspaceId, CancellationToken ct = default)
    {
        var membership = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (membership is null)
        {
            return ServiceResult.NotFound();
        }

        if (membership.Role != MemberRoles.Owner)
        {
            return ServiceResult.Forbidden("Only the workspace owner may delete it.");
        }

        var workspace = await _context.Workspaces.FindAsync(new object[] { workspaceId }, ct);
        if (workspace is null)
        {
            return ServiceResult.NotFound();
        }

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<List<WorkspaceMemberDto>>> GetMembersAsync(int userId, int workspaceId, CancellationToken ct = default)
    {
        var caller = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (caller is null)
        {
            return ServiceResult<List<WorkspaceMemberDto>>.NotFound();
        }

        var members = await _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Join(_context.Users, m => m.UserId, u => u.Id,
                (m, u) => new WorkspaceMemberDto(u.Id, u.Username, u.Email, m.Role))
            .ToListAsync(ct);

        return ServiceResult<List<WorkspaceMemberDto>>.Ok(members);
    }

    public async Task<ServiceResult<WorkspaceMemberDto>> AddMemberAsync(int userId, int workspaceId, string email, CancellationToken ct = default)
    {
        var caller = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (caller is null)
        {
            return ServiceResult<WorkspaceMemberDto>.NotFound();
        }

        if (caller.Role != MemberRoles.Owner)
        {
            return ServiceResult<WorkspaceMemberDto>.Forbidden("Only the workspace owner may invite members.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return ServiceResult<WorkspaceMemberDto>.BadRequest($"Unknown email: {email}");
        }

        var existing = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == user.Id, ct);
        if (existing is not null)
        {
            return ServiceResult<WorkspaceMemberDto>.Ok(
                new WorkspaceMemberDto(user.Id, user.Username, user.Email, existing.Role));
        }

        _context.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = user.Id,
            Role = MemberRoles.Member
        });
        await _context.SaveChangesAsync(ct);

        return ServiceResult<WorkspaceMemberDto>.Ok(
            new WorkspaceMemberDto(user.Id, user.Username, user.Email, MemberRoles.Member));
    }

    public async Task<ServiceResult> RemoveMemberAsync(int userId, int workspaceId, int memberUserId, CancellationToken ct = default)
    {
        var caller = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (caller is null)
        {
            return ServiceResult.NotFound();
        }

        if (caller.Role != MemberRoles.Owner)
        {
            return ServiceResult.Forbidden("Only the workspace owner may remove members.");
        }

        var target = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == memberUserId, ct);

        if (target is null)
        {
            return ServiceResult.NotFound();
        }

        if (target.Role == MemberRoles.Owner)
        {
            return ServiceResult.BadRequest("Cannot remove an Owner. Transfer ownership first.");
        }

        _context.WorkspaceMembers.Remove(target);
        await _context.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }
}


using Microsoft.EntityFrameworkCore;
using TaskBoard.Common;
using TaskBoard.Data;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class LabelService : ILabelService
{
    private readonly AppDbContext _context;
    private readonly IMembershipService _membership;

    public LabelService(AppDbContext context, IMembershipService membership)
    {
        _context = context;
        _membership = membership;
    }

    public async Task<ServiceResult<List<LabelDto>>> GetForWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default)
    {
        if (!await _membership.IsMemberOfWorkspaceAsync(userId, workspaceId, ct))
        {
            return ServiceResult<List<LabelDto>>.Forbidden();
        }

        var labels = await _context.Labels
            .Where(l => l.WorkspaceId == workspaceId)
            .Select(l => new LabelDto(l.Id, l.Name, l.Color, l.WorkspaceId))
            .ToListAsync(ct);

        return ServiceResult<List<LabelDto>>.Ok(labels);
    }

    public async Task<ServiceResult<LabelDto>> CreateAsync(int userId, CreateLabelDto dto, CancellationToken ct = default)
    {
        var workspaceId = dto.WorkspaceId!.Value;

        if (!await _membership.IsMemberOfWorkspaceAsync(userId, workspaceId, ct))
        {
            return ServiceResult<LabelDto>.Forbidden();
        }

        var label = new Label { Name = dto.Name, Color = dto.Color, WorkspaceId = workspaceId };
        _context.Labels.Add(label);
        await _context.SaveChangesAsync(ct);

        return ServiceResult<LabelDto>.Ok(LabelDto.From(label));
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int labelId, CancellationToken ct = default)
    {
        var label = await _context.Labels.FindAsync(new object[] { labelId }, ct);
        if (label is null)
        {
            return ServiceResult.NotFound();
        }

        if (!await _membership.IsMemberOfWorkspaceAsync(userId, label.WorkspaceId, ct))
        {
            return ServiceResult.Forbidden();
        }

        _context.Labels.Remove(label);
        await _context.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }
}


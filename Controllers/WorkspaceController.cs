using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Common;
using TaskBoard.Models.Dto;
using TaskBoard.Security;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceService _service;

    public WorkspaceController(IWorkspaceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkspaces(CancellationToken ct) =>
        Ok(await _service.GetForUserAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto, CancellationToken ct) =>
        (await _service.CreateAsync(User.GetUserId(), dto, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteWorkspace(int id, CancellationToken ct) =>
        (await _service.DeleteAsync(User.GetUserId(), id, ct)).ToActionResult();

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id, CancellationToken ct) =>
        (await _service.GetMembersAsync(User.GetUserId(), id, ct)).ToActionResult();

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] InviteMemberDto dto, CancellationToken ct) =>
        (await _service.AddMemberAsync(User.GetUserId(), id, dto.Email, ct)).ToActionResult();

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId, CancellationToken ct) =>
        (await _service.RemoveMemberAsync(User.GetUserId(), id, userId, ct)).ToActionResult();
}


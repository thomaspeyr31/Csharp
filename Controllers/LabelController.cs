using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Common;
using TaskBoard.Models.Dto;
using TaskBoard.Security;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
public class LabelController : ControllerBase
{
    private readonly ILabelService _service;

    public LabelController(ILabelService service)
    {
        _service = service;
    }

    [HttpGet("workspace/{workspaceId:int}")]
    public async Task<IActionResult> GetForWorkspace(int workspaceId, CancellationToken ct) =>
        (await _service.GetForWorkspaceAsync(User.GetUserId(), workspaceId, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLabelDto dto, CancellationToken ct) =>
        (await _service.CreateAsync(User.GetUserId(), dto, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        (await _service.DeleteAsync(User.GetUserId(), id, ct)).ToActionResult();
}


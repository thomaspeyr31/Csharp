using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Common;
using TaskBoard.Models.Dto;
using TaskBoard.Security;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _service;

    public CommentController(ICommentService service)
    {
        _service = service;
    }

    [HttpGet("api/cards/{cardId:int}/comments")]
    public async Task<IActionResult> GetForCard(int cardId, CancellationToken ct) =>
        (await _service.GetForCardAsync(User.GetUserId(), cardId, ct)).ToActionResult();

    [HttpPost("api/cards/{cardId:int}/comments")]
    public async Task<IActionResult> Create(int cardId, [FromBody] CreateCommentDto dto, CancellationToken ct) =>
        (await _service.CreateAsync(User.GetUserId(), cardId, dto, ct)).ToActionResult();

    [HttpPut("api/comments/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentDto dto, CancellationToken ct) =>
        (await _service.UpdateAsync(User.GetUserId(), id, dto, ct)).ToActionResult();

    [HttpDelete("api/comments/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        (await _service.DeleteAsync(User.GetUserId(), id, ct)).ToActionResult();
}


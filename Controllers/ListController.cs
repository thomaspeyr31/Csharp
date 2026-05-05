using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Common;
using TaskBoard.Models.Dto;
using TaskBoard.Security;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/lists")]
[Authorize]
public class ListController : ControllerBase
{
    private readonly IListService _service;

    public ListController(IListService service)
    {
        _service = service;
    }

    [HttpGet("board/{boardId:int}")]
    public async Task<IActionResult> GetLists(int boardId, CancellationToken ct) =>
        (await _service.GetByBoardAsync(User.GetUserId(), boardId, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> CreateList([FromBody] CreateBoardListDto dto, CancellationToken ct) =>
        (await _service.CreateAsync(User.GetUserId(), dto, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteList(int id, CancellationToken ct) =>
        (await _service.DeleteAsync(User.GetUserId(), id, ct)).ToActionResult();
}


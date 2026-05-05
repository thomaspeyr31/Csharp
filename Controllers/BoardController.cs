using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Common;
using TaskBoard.Models.Dto;
using TaskBoard.Security;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/boards")]
[Authorize]
public class BoardController : ControllerBase
{
    private readonly IBoardService _service;

    public BoardController(IBoardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetBoards(CancellationToken ct) =>
        Ok(await _service.GetForUserAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<IActionResult> CreateBoard([FromBody] CreateBoardDto dto, CancellationToken ct) =>
        (await _service.CreateAsync(User.GetUserId(), dto, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBoard(int id, CancellationToken ct) =>
        (await _service.DeleteAsync(User.GetUserId(), id, ct)).ToActionResult();
}


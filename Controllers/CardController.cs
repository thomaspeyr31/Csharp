using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Common;
using TaskBoard.Models.Dto;
using TaskBoard.Security;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardController : ControllerBase
{
    private readonly ICardService _service;

    public CardController(ICardService service)
    {
        _service = service;
    }

    [HttpGet("list/{listId:int}")]
    public async Task<IActionResult> GetCardsByList(int listId, CancellationToken ct) =>
        (await _service.GetByListAsync(User.GetUserId(), listId, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCard(int id, CancellationToken ct) =>
        (await _service.GetWithDetailsAsync(User.GetUserId(), id, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardDto dto, CancellationToken ct) =>
        (await _service.CreateAsync(User.GetUserId(), dto, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCard(int id, [FromBody] UpdateCardDto dto, CancellationToken ct) =>
        (await _service.UpdateAsync(User.GetUserId(), id, dto, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCard(int id, CancellationToken ct) =>
        (await _service.DeleteAsync(User.GetUserId(), id, ct)).ToActionResult();

    [HttpPost("{id:int}/labels")]
    public async Task<IActionResult> AddLabel(int id, [FromBody] AddLabelToCardDto dto, CancellationToken ct) =>
        (await _service.AddLabelAsync(User.GetUserId(), id, dto.LabelId!.Value, ct)).ToActionResult();

    [HttpDelete("{id:int}/labels/{labelId:int}")]
    public async Task<IActionResult> RemoveLabel(int id, int labelId, CancellationToken ct) =>
        (await _service.RemoveLabelAsync(User.GetUserId(), id, labelId, ct)).ToActionResult();

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberToCardDto dto, CancellationToken ct) =>
        (await _service.AddMemberAsync(User.GetUserId(), id, dto.UserId!.Value, ct)).ToActionResult();

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId, CancellationToken ct) =>
        (await _service.RemoveMemberAsync(User.GetUserId(), id, userId, ct)).ToActionResult();
}


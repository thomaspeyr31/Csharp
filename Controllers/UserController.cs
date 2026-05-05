using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Models;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _service;

    public UserController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var dto = await _service.RegisterAsync(request, ct);
        return dto is null ? BadRequest("User already exists") : Ok(dto);
    }
}


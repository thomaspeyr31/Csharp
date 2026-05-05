using Microsoft.AspNetCore.Mvc;
using TaskBoard.Models.Dto;
using TaskBoard.Services;

namespace TaskBoard.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var pair = await _auth.LoginAsync(dto.Email, dto.Password, ct);
        return pair is null ? Unauthorized("Invalid credentials") : Ok(pair);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto, CancellationToken ct)
    {
        var pair = await _auth.RefreshAsync(dto.RefreshToken, ct);
        return pair is null ? Unauthorized("Invalid or expired refresh token") : Ok(pair);
    }
}


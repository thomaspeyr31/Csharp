using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);

    Task<UserDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}


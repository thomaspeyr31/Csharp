using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public interface IAuthService
{
    Task<TokenPairDto?> LoginAsync(string email, string password, CancellationToken ct = default);

    Task<TokenPairDto?> RefreshAsync(string refreshToken, CancellationToken ct = default);
}


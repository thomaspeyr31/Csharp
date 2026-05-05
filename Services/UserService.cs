using Microsoft.EntityFrameworkCore;
using TaskBoard.Data;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<UserDto>> GetAllAsync(CancellationToken ct = default) =>
        _context.Users
            .Select(u => new UserDto(u.Id, u.Username, u.Email))
            .ToListAsync(ct);

    public async Task<UserDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (exists)
        {
            return null;
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        return UserDto.From(user);
    }
}


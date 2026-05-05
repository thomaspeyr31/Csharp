using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Data;
using TaskBoard.Models;
using TaskBoard.Models.Dto;

namespace TaskBoard.Services;

public class AuthService : IAuthService
{
    private const int AccessTokenLifetimeMinutes = 15;
    private const int RefreshTokenLifetimeDays = 7;
    private const int RefreshTokenByteLength = 32;

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<TokenPairDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return await IssueTokenPairAsync(user, ct);
    }

    public async Task<TokenPairDto?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }

        var hash = HashToken(refreshToken);
        var stored = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return await IssueTokenPairAsync(stored.User, ct);
    }

    private async Task<TokenPairDto> IssueTokenPairAsync(User user, CancellationToken ct)
    {
        var accessToken = IssueAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user, ct);
        return new TokenPairDto(accessToken, refreshToken);
    }

    private string IssueAccessToken(User user)
    {
        var keyValue = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("role", user.Role),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> IssueRefreshTokenAsync(User user, CancellationToken ct)
    {
        var raw = GenerateRandomToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = HashToken(raw),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
        });
        await _context.SaveChangesAsync(ct);

        return raw;
    }

    private static string GenerateRandomToken()
    {
        var bytes = new byte[RefreshTokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}


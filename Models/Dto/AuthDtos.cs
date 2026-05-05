using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Models.Dto;

public record LoginDto(
    [Required][EmailAddress] string Email,
    [Required][StringLength(200, MinimumLength = 1)] string Password);

public record RefreshDto(
    [Required] string RefreshToken);

public record TokenPairDto(string AccessToken, string RefreshToken);


using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Models.Dto;

public record CreateBoardDto(
    [Required][StringLength(100, MinimumLength = 1)] string Name,
    [Required] int? WorkspaceId);

public record UpdateBoardDto(
    [Required][StringLength(100, MinimumLength = 1)] string Name);

public record BoardDto(int Id, string Name, int WorkspaceId)
{
    public static BoardDto From(Board b) => new(b.Id, b.Name, b.WorkspaceId);
}


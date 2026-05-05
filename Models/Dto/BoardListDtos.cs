using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Models.Dto;

public record CreateBoardListDto(
    [Required][StringLength(100, MinimumLength = 1)] string Name,
    [Range(0, int.MaxValue)] int Position,
    [Required] int? BoardId);

public record UpdateBoardListDto(
    [Required][StringLength(100, MinimumLength = 1)] string Name,
    [Range(0, int.MaxValue)] int Position);

public record BoardListDto(int Id, string Name, int Position, int BoardId)
{
    public static BoardListDto From(BoardList l) => new(l.Id, l.Name, l.Position, l.BoardId);
}


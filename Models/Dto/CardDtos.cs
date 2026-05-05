using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Models.Dto;

public record CreateCardDto(
    [Required][StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    [Range(0, int.MaxValue)] int Position,
    DateTime? DueDate,
    [Required] int? ListId);

public record UpdateCardDto(
    [Required][StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    [Range(0, int.MaxValue)] int Position,
    DateTime? DueDate,
    [Required] int? ListId);

public record CardDto(int Id, string Title, string? Description, int Position, DateTime? DueDate, int ListId)
{
    public static CardDto From(Card c) => new(c.Id, c.Title, c.Description, c.Position, c.DueDate, c.ListId);
}

public record CardDetailDto(
    int Id, string Title, string? Description, int Position, DateTime? DueDate, int ListId,
    List<LabelDto> Labels, List<UserDto> Members, List<CommentDto> Comments);

public record AddLabelToCardDto([Required] int? LabelId);

public record AddMemberToCardDto([Required] int? UserId);


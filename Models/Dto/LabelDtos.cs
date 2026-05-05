using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TaskBoard.Models.Dto;

public record CreateLabelDto(
    [Required][StringLength(50, MinimumLength = 1)] string Name,
    [Required][RegularExpression("^#[0-9a-fA-F]{6}$")] string Color,
    [Required] int? WorkspaceId);

public record LabelDto(int Id, string Name, string Color, int WorkspaceId)
{
    public static LabelDto From(Label l) => new(l.Id, l.Name, l.Color, l.WorkspaceId);
}


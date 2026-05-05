using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Models.Dto;

public record CreateCommentDto(
    [Required][StringLength(2000, MinimumLength = 1)] string Content);

public record UpdateCommentDto(
    [Required][StringLength(2000, MinimumLength = 1)] string Content);

public record CommentDto(
    int Id, int CardId, int AuthorId, string AuthorUsername,
    string Content, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public static CommentDto From(Comment c) => new(
        c.Id, c.CardId, c.AuthorId, c.Author?.Username ?? "",
        c.Content, c.CreatedAt, c.UpdatedAt);
}


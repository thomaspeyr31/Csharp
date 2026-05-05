namespace TaskBoard.Models;

public class Comment
{
    public int Id { get; set; }

    public int CardId { get; set; }

    public Card Card { get; set; } = null!;

    public int AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}


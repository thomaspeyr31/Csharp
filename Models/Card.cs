namespace TaskBoard.Models;

public class Card
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public int Position { get; set; }

    public DateTime? DueDate { get; set; }

    public int ListId { get; set; }

    public BoardList? List { get; set; }

    public ICollection<CardLabel> Labels { get; set; } = new List<CardLabel>();

    public ICollection<CardMember> Members { get; set; } = new List<CardMember>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}


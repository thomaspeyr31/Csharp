namespace TaskBoard.Models;

public class CardMember
{
    public int CardId { get; set; }

    public Card Card { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}

